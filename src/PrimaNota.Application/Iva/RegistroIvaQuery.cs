using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Azienda;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.Iva;

/// <summary>A row of the VAT register for display/export.</summary>
public sealed record RegistroIvaRigaDto(
    Guid MovimentoId,
    Guid RigaId,
    DateOnly Data,
    string? Numero,
    string CausaleCodice,
    string? AnagraficaRagioneSociale,
    string? PartitaIva,
    string? CodiceFiscale,
    string Descrizione,
    string AliquotaCodice,
    decimal AliquotaPercentuale,
    TipoIva TipoIva,
    decimal Lordo,
    decimal Imponibile,
    decimal Imposta);

/// <summary>Returns all eligible lines of a given VAT register within a period.</summary>
/// <param name="Registro">Sales / Purchases / Retail.</param>
/// <param name="Periodo">Period.</param>
public sealed record GetRegistroIva(TipoRegistroIva Registro, IvaPeriodo Periodo)
    : IRequest<IReadOnlyList<RegistroIvaRigaDto>>;

/// <summary>Handler for <see cref="GetRegistroIva"/>.</summary>
public sealed class GetRegistroIvaHandler : IRequestHandler<GetRegistroIva, IReadOnlyList<RegistroIvaRigaDto>>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="GetRegistroIvaHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public GetRegistroIvaHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistroIvaRigaDto>> Handle(GetRegistroIva request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var causaleTipiAmmessi = request.Registro switch
        {
            TipoRegistroIva.Corrispettivi => new[] { TipoMovimento.Incasso },
            TipoRegistroIva.Vendite => new[] { TipoMovimento.Incasso },
            TipoRegistroIva.Acquisti => new[] { TipoMovimento.Pagamento, TipoMovimento.RimborsoNotaSpese },
            _ => Array.Empty<TipoMovimento>(),
        };

        // Vendite = solo causali con Fonte = Fattura; Corrispettivi = solo Fonte = Corrispettivo.
        FonteCausale? fonteRichiesta = request.Registro switch
        {
            TipoRegistroIva.Vendite => FonteCausale.Fattura,
            TipoRegistroIva.Corrispettivi => FonteCausale.Corrispettivo,
            _ => null,
        };

        var natureAmmesse = request.Registro switch
        {
            TipoRegistroIva.Acquisti => new[] { NaturaCategoria.Uscita },
            _ => new[] { NaturaCategoria.Entrata },
        };

        var esigibilita = await LoadEsigibilitaAsync(cancellationToken);

        return esigibilita == EsigibilitaIva.Cassa
            ? await HandleCassaAsync(request.Periodo, causaleTipiAmmessi, fonteRichiesta, natureAmmesse, cancellationToken)
            : await HandleImmediataAsync(request.Periodo, causaleTipiAmmessi, fonteRichiesta, natureAmmesse, cancellationToken);
    }

    private async Task<EsigibilitaIva> LoadEsigibilitaAsync(CancellationToken cancellationToken)
    {
        var config = await db.ConfigurazioneAzienda.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ConfigurazioneAzienda.SingletonId, cancellationToken);
        return config?.EsigibilitaIvaPredefinita ?? EsigibilitaIva.Immediata;
    }

    private async Task<IReadOnlyList<RegistroIvaRigaDto>> HandleImmediataAsync(
        IvaPeriodo periodo,
        IReadOnlyList<TipoMovimento> causaleTipiAmmessi,
        FonteCausale? fonteRichiesta,
        IReadOnlyList<NaturaCategoria> natureAmmesse,
        CancellationToken cancellationToken)
    {
        var dataFrom = periodo.DataInizio;
        var dataTo = periodo.DataFine;

        var rawRows = await (
            from m in db.Movimenti.AsNoTracking()
            where m.EsercizioAnno == periodo.Anno
                  && m.DataCompetenza >= dataFrom && m.DataCompetenza <= dataTo
                  && (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
            join c in db.Causali.AsNoTracking() on m.CausaleId equals c.Id
            where causaleTipiAmmessi.Contains(c.Tipo)
                  && (fonteRichiesta == null || c.Fonte == fonteRichiesta)
            from r in m.Righe
            where r.AliquotaIvaId != null
            join cat in db.Categorie.AsNoTracking() on r.CategoriaId equals cat.Id
            where natureAmmesse.Contains(cat.Natura)
            join al in db.AliquoteIva.AsNoTracking() on r.AliquotaIvaId equals al.Id
            let anagId = r.AnagraficaId ?? m.AnagraficaId
            join an in db.Anagrafiche.AsNoTracking() on anagId equals an.Id into anagJoin
            from anag in anagJoin.DefaultIfEmpty()
            orderby m.DataCompetenza, m.Numero
            select new
            {
                MovimentoId = m.Id,
                RigaId = r.Id,
                Data = m.DataCompetenza,
                m.Numero,
                CausaleCodice = c.Codice,
                AnagraficaRagioneSociale = anag != null ? anag.RagioneSociale : null,
                PartitaIva = anag != null ? anag.PartitaIva : null,
                CodiceFiscale = anag != null ? anag.CodiceFiscale : null,
                m.Descrizione,
                AliquotaCodice = al.Codice,
                AliquotaPercentuale = al.Percentuale,
                al.Tipo,
                Lordo = r.Importo,
            }).ToListAsync(cancellationToken);

        return rawRows.Select(row =>
        {
            var (imponibile, imposta) = IvaScorporo.Scorpora(Math.Abs(row.Lordo), row.AliquotaPercentuale);
            var sign = Math.Sign(row.Lordo);
            return new RegistroIvaRigaDto(
                row.MovimentoId,
                row.RigaId,
                row.Data,
                row.Numero,
                row.CausaleCodice,
                row.AnagraficaRagioneSociale,
                row.PartitaIva,
                row.CodiceFiscale,
                row.Descrizione,
                row.AliquotaCodice,
                row.AliquotaPercentuale,
                row.Tipo,
                row.Lordo,
                imponibile * sign,
                imposta * sign);
        }).ToList();
    }

    private async Task<IReadOnlyList<RegistroIvaRigaDto>> HandleCassaAsync(
        IvaPeriodo periodo,
        IReadOnlyList<TipoMovimento> causaleTipiAmmessi,
        FonteCausale? fonteRichiesta,
        IReadOnlyList<NaturaCategoria> natureAmmesse,
        CancellationToken cancellationToken)
    {
        var dataFrom = periodo.DataInizio;
        var dataTo = periodo.DataFine;

        var candidates = await (
            from m in db.Movimenti.AsNoTracking()
            where m.EsercizioAnno == periodo.Anno
                  && (m.Stato == StatoMovimento.Confirmed || m.Stato == StatoMovimento.Reconciled)
                  && (
                      m.Pagamenti.Any(p => p.Data >= dataFrom && p.Data <= dataTo)
                      || (!m.Pagamenti.Any() && m.Data >= dataFrom && m.Data <= dataTo))
            join c in db.Causali.AsNoTracking() on m.CausaleId equals c.Id
            where causaleTipiAmmessi.Contains(c.Tipo)
                  && (fonteRichiesta == null || c.Fonte == fonteRichiesta)
            select new
            {
                MovimentoId = m.Id,
                m.Data,
                m.Numero,
                m.Descrizione,
                m.AnagraficaId,
                CausaleCodice = c.Codice,
                Righe = m.Righe
                    .Where(r => r.AliquotaIvaId != null)
                    .Select(r => new { r.Id, r.Importo, r.CategoriaId, r.AliquotaIvaId, r.AnagraficaId })
                    .ToList(),
                HasPagamenti = m.Pagamenti.Any(),
                PagamentiNelPeriodo = m.Pagamenti
                    .Where(p => p.Data >= dataFrom && p.Data <= dataTo)
                    .OrderBy(p => p.Data)
                    .Select(p => new { p.Data, p.Importo })
                    .ToList(),
            }).ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<RegistroIvaRigaDto>();
        }

        var categorie = await db.Categorie.AsNoTracking()
            .ToDictionaryAsync(cat => cat.Id, cat => cat.Natura, cancellationToken);
        var aliquote = await db.AliquoteIva.AsNoTracking()
            .ToDictionaryAsync(a => a.Id, a => new { a.Codice, a.Percentuale, a.Tipo }, cancellationToken);
        var anagIds = candidates
            .SelectMany(c => new[] { c.AnagraficaId }.Concat(c.Righe.Select(r => r.AnagraficaId)))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var anagrafiche = await db.Anagrafiche.AsNoTracking()
            .Where(a => anagIds.Contains(a.Id))
            .ToDictionaryAsync(
                a => a.Id,
                a => new { a.RagioneSociale, a.PartitaIva, a.CodiceFiscale },
                cancellationToken);

        var result = new List<RegistroIvaRigaDto>(candidates.Count * 2);
        foreach (var m in candidates)
        {
            // Effective payments in period: real ones, or a synthesised "cash-basis" event on m.Data.
            var pagamentiEffettivi = m.HasPagamenti
                ? m.PagamentiNelPeriodo.Select(p => (p.Data, p.Importo)).ToList()
                : new List<(DateOnly Data, decimal Importo)> { (m.Data, m.Righe.Sum(r => Math.Abs(r.Importo))) };

            if (pagamentiEffettivi.Count == 0)
            {
                continue;
            }

            var totaleLordo = m.Righe.Sum(r => Math.Abs(r.Importo));
            if (totaleLordo <= 0m)
            {
                continue;
            }

            foreach (var pag in pagamentiEffettivi)
            {
                if (pag.Importo <= 0m)
                {
                    continue;
                }

                var ratio = Math.Min(pag.Importo / totaleLordo, 1m);

                foreach (var riga in m.Righe)
                {
                    if (riga.AliquotaIvaId is not Guid alId || !aliquote.TryGetValue(alId, out var al))
                    {
                        continue;
                    }

                    if (!categorie.TryGetValue(riga.CategoriaId, out var natura) || !natureAmmesse.Contains(natura))
                    {
                        continue;
                    }

                    var lordoRiga = decimal.Round(riga.Importo * ratio, 2, MidpointRounding.ToEven);
                    if (lordoRiga == 0m)
                    {
                        continue;
                    }

                    var (imponibile, imposta) = IvaScorporo.Scorpora(Math.Abs(lordoRiga), al.Percentuale);
                    var sign = Math.Sign(lordoRiga);

                    var anagId = riga.AnagraficaId ?? m.AnagraficaId;
                    var anag = anagId.HasValue && anagrafiche.TryGetValue(anagId.Value, out var a) ? a : null;

                    result.Add(new RegistroIvaRigaDto(
                        m.MovimentoId,
                        riga.Id,
                        pag.Data,
                        m.Numero,
                        m.CausaleCodice,
                        anag?.RagioneSociale,
                        anag?.PartitaIva,
                        anag?.CodiceFiscale,
                        m.Descrizione,
                        al.Codice,
                        al.Percentuale,
                        al.Tipo,
                        lordoRiga,
                        imponibile * sign,
                        imposta * sign));
                }
            }
        }

        return result
            .OrderBy(r => r.Data)
            .ThenBy(r => r.Numero)
            .ToList();
    }
}
