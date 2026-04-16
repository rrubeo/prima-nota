using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Anagrafiche;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.PrimaNota.Import;

/// <summary>Direction of the invoice being imported from the owner's perspective.</summary>
public enum DirezioneFattura
{
    /// <summary>Fattura emessa — we are the cedente, the other party is our customer.</summary>
    Attiva = 1,

    /// <summary>Fattura ricevuta — we are the cessionario, the other party is our supplier.</summary>
    Passiva = 2,
}

/// <summary>Result of a successful import.</summary>
/// <param name="MovimentoId">Identifier of the movement just created in Draft state.</param>
/// <param name="AnagraficaId">Identifier of the linked anagrafica (existing or freshly created).</param>
/// <param name="AnagraficaNuova">True if the anagrafica was created on the fly.</param>
/// <param name="RigheCreate">Number of movement lines generated.</param>
/// <param name="AliquoteMancanti">Diagnostic list of rate codes / natures that could not be matched to an existing AliquotaIva.</param>
public sealed record ImportFatturaResult(
    Guid MovimentoId,
    Guid AnagraficaId,
    bool AnagraficaNuova,
    int RigheCreate,
    IReadOnlyList<string> AliquoteMancanti);

/// <summary>
/// Parses an Italian electronic-invoice XML stream, creates/updates the counterpart
/// anagrafica and produces a prima-nota movement in Draft state.
/// </summary>
/// <param name="Xml">Readable XML stream.</param>
/// <param name="Direzione">Attiva (emessa) or Passiva (ricevuta).</param>
/// <param name="ContoFinanziarioId">Financial account that will carry the movement lines.</param>
/// <param name="EsercizioAnno">Target fiscal year (must match the invoice date).</param>
public sealed record ImportFatturaElettronica(
    Stream Xml,
    DirezioneFattura Direzione,
    Guid ContoFinanziarioId,
    int EsercizioAnno) : IRequest<ImportFatturaResult>;

/// <summary>Handler for <see cref="ImportFatturaElettronica"/>.</summary>
public sealed class ImportFatturaElettronicaHandler : IRequestHandler<ImportFatturaElettronica, ImportFatturaResult>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="ImportFatturaElettronicaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public ImportFatturaElettronicaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<ImportFatturaResult> Handle(ImportFatturaElettronica request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parsed = FatturaElettronicaParser.Parse(request.Xml);

        if (parsed.Data.Year != request.EsercizioAnno)
        {
            throw new InvalidOperationException(
                $"La fattura e datata {parsed.Data:yyyy-MM-dd} ma l'esercizio selezionato e {request.EsercizioAnno}.");
        }

        // Controparte = chi NON sono io
        var controparte = request.Direzione == DirezioneFattura.Attiva ? parsed.Cessionario : parsed.Cedente;

        var (anagrafica, created) = await UpsertAnagraficaAsync(controparte, request.Direzione, cancellationToken);

        // Causale coerente con la direzione: cerca per codice canonico, fallback al primo compatibile.
        var causale = await PickCausaleAsync(request.Direzione, cancellationToken);

        var descrizione = request.Direzione == DirezioneFattura.Attiva
            ? $"Fattura emessa {parsed.Numero} — {controparte.DisplayName}"
            : $"Fattura ricevuta {parsed.Numero} — {controparte.DisplayName}";

        var movimento = new MovimentoPrimaNota(parsed.Data, request.EsercizioAnno, descrizione, causale.Id);
        movimento.UpdateHeader(
            parsed.Data,
            descrizione,
            causale.Id,
            parsed.Numero,
            anagrafica.Id,
            note: null);

        var categoriaDefault = await PickCategoriaDefaultAsync(request.Direzione, causale, cancellationToken);
        var aliquote = await db.AliquoteIva.AsNoTracking().ToListAsync(cancellationToken);

        var aliquoteMancanti = new List<string>();
        var righe = new List<RigaMovimento>();

        foreach (var riep in parsed.Riepilogo)
        {
            var lordo = riep.Totale;
            if (lordo == 0m)
            {
                continue;
            }

            var signed = request.Direzione == DirezioneFattura.Attiva ? lordo : -lordo;

            var aliquota = MatchAliquota(aliquote, riep);
            if (aliquota is null)
            {
                aliquoteMancanti.Add(FormatMissingRate(riep));
            }

            var riga = new RigaMovimento(signed, request.ContoFinanziarioId, categoriaDefault.Id);
            riga.SetAnagrafica(anagrafica.Id);
            if (aliquota is not null)
            {
                riga.SetAliquotaIva(aliquota.Id);
            }

            if (!string.IsNullOrWhiteSpace(riep.RiferimentoNormativo))
            {
                riga.SetNote(riep.RiferimentoNormativo);
            }

            righe.Add(riga);
        }

        if (righe.Count == 0)
        {
            throw new InvalidOperationException("Nessuna riga da creare: il totale dei riepiloghi e zero.");
        }

        movimento.ReplaceRighe(righe);
        db.Movimenti.Add(movimento);

        await db.SaveChangesAsync(cancellationToken);

        return new ImportFatturaResult(
            movimento.Id,
            anagrafica.Id,
            created,
            righe.Count,
            aliquoteMancanti);
    }

    private async Task<(Anagrafica Anagrafica, bool Created)> UpsertAnagraficaAsync(
        FatturaSoggettoDto soggetto,
        DirezioneFattura direzione,
        CancellationToken cancellationToken)
    {
        var piva = Normalize(soggetto.PartitaIva);
        var cf = Normalize(soggetto.CodiceFiscale)?.ToUpperInvariant();

        Anagrafica? existing = null;
        if (!string.IsNullOrEmpty(piva))
        {
            existing = await db.Anagrafiche.FirstOrDefaultAsync(a => a.PartitaIva == piva, cancellationToken);
        }

        if (existing is null && !string.IsNullOrEmpty(cf))
        {
            existing = await db.Anagrafiche.FirstOrDefaultAsync(a => a.CodiceFiscale == cf, cancellationToken);
        }

        if (existing is not null)
        {
            var isCliente = existing.IsCliente || direzione == DirezioneFattura.Attiva;
            var isFornitore = existing.IsFornitore || direzione == DirezioneFattura.Passiva;
            existing.SetRuoli(isCliente, isFornitore, existing.IsDipendente);
            return (existing, false);
        }

        var personaFisica = !string.IsNullOrWhiteSpace(soggetto.Nome) || !string.IsNullOrWhiteSpace(soggetto.Cognome);
        var ragione = soggetto.DisplayName;
        if (string.IsNullOrWhiteSpace(ragione))
        {
            throw new InvalidOperationException("Controparte senza denominazione ne nome/cognome.");
        }

        var fresh = new Anagrafica(ragione, personaFisica);
        fresh.UpdateIdentificazione(ragione, soggetto.Nome, soggetto.Cognome, cf, piva, personaFisica);
        fresh.SetRuoli(direzione == DirezioneFattura.Attiva, direzione == DirezioneFattura.Passiva, false);
        fresh.UpdateContatti(new Contatti(soggetto.Email, null, null));
        fresh.UpdateIndirizzo(new Indirizzo(
            soggetto.Indirizzo,
            soggetto.Cap,
            soggetto.Comune,
            soggetto.Provincia?.ToUpperInvariant(),
            string.IsNullOrWhiteSpace(soggetto.Nazione) ? "IT" : soggetto.Nazione!.ToUpperInvariant()));

        db.Anagrafiche.Add(fresh);
        return (fresh, true);
    }

    private async Task<Causale> PickCausaleAsync(DirezioneFattura direzione, CancellationToken cancellationToken)
    {
        var preferredCode = direzione == DirezioneFattura.Attiva ? "INC-FATT" : "PAG-FATT";
        var preferredTipo = direzione == DirezioneFattura.Attiva ? TipoMovimento.Incasso : TipoMovimento.Pagamento;

        var causale = await db.Causali.FirstOrDefaultAsync(c => c.Codice == preferredCode && c.Attiva, cancellationToken);
        causale ??= await db.Causali.FirstOrDefaultAsync(c => c.Tipo == preferredTipo && c.Attiva, cancellationToken);

        return causale ?? throw new InvalidOperationException(
            $"Nessuna causale attiva di tipo {preferredTipo}. Crea almeno una causale compatibile prima di importare fatture.");
    }

    private async Task<Categoria> PickCategoriaDefaultAsync(
        DirezioneFattura direzione,
        Causale causale,
        CancellationToken cancellationToken)
    {
        if (causale.CategoriaDefaultId is { } catId)
        {
            var fromCausale = await db.Categorie.FirstOrDefaultAsync(c => c.Id == catId && c.Attiva, cancellationToken);
            if (fromCausale is not null)
            {
                return fromCausale;
            }
        }

        var preferredCode = direzione == DirezioneFattura.Attiva ? "VEN" : "ACQ-SERV";
        var preferredNatura = direzione == DirezioneFattura.Attiva ? NaturaCategoria.Entrata : NaturaCategoria.Uscita;

        var categoria = await db.Categorie.FirstOrDefaultAsync(c => c.Codice == preferredCode && c.Attiva, cancellationToken);
        categoria ??= await db.Categorie.FirstOrDefaultAsync(c => c.Natura == preferredNatura && c.Attiva, cancellationToken);

        return categoria ?? throw new InvalidOperationException(
            "Nessuna categoria attiva trovata per la fattura. Configura il piano dei conti prima di importare.");
    }

    private static Domain.Iva.AliquotaIva? MatchAliquota(
        IReadOnlyList<Domain.Iva.AliquotaIva> aliquote,
        FatturaRiepilogoDto riep)
    {
        if (riep.AliquotaPercentuale > 0m)
        {
            return aliquote.FirstOrDefault(a => a.Attiva && a.Percentuale == riep.AliquotaPercentuale);
        }

        if (!string.IsNullOrWhiteSpace(riep.Natura))
        {
            var natura = riep.Natura.Trim().ToUpperInvariant();
            var exact = aliquote.FirstOrDefault(a => a.Attiva && string.Equals(a.CodiceNatura, natura, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }

            // Fallback on the prefix (N2.1 -> N2)
            var family = natura.Split('.')[0];
            return aliquote.FirstOrDefault(a => a.Attiva && !string.IsNullOrEmpty(a.CodiceNatura)
                                                && a.CodiceNatura!.StartsWith(family, StringComparison.OrdinalIgnoreCase));
        }

        return aliquote.FirstOrDefault(a => a.Attiva && a.Percentuale == 0m);
    }

    private static string FormatMissingRate(FatturaRiepilogoDto riep)
    {
        if (riep.AliquotaPercentuale > 0m)
        {
            return $"{riep.AliquotaPercentuale:0.##}%";
        }

        return string.IsNullOrWhiteSpace(riep.Natura) ? "0%" : $"Natura {riep.Natura}";
    }

    private static string? Normalize(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
