using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrimaNota.Domain.Iva;
using PrimaNota.Domain.PianoConti;

namespace PrimaNota.Infrastructure.Persistence;

/// <summary>
/// Seeds the initial master data required for a fresh Prima Nota installation:
/// canonical categories, default causali and Italian VAT rates. Idempotent: checks
/// by unique code before inserting and never overwrites user-edited rows.
/// </summary>
public sealed class MasterDataSeeder
{
    private static readonly IReadOnlyList<(string Codice, string Nome, NaturaCategoria Natura)> DefaultCategorie = new[]
    {
        ("VEN", "Vendite / Prestazioni", NaturaCategoria.Entrata),
        ("INC-ALT", "Altri incassi", NaturaCategoria.Entrata),
        ("INT-ATT", "Interessi attivi", NaturaCategoria.Entrata),
        ("ACQ-BENI", "Acquisto beni", NaturaCategoria.Uscita),
        ("ACQ-SERV", "Acquisto servizi", NaturaCategoria.Uscita),
        ("STIP", "Stipendi e compensi", NaturaCategoria.Uscita),
        ("UTEN", "Utenze (luce, gas, acqua, telefono)", NaturaCategoria.Uscita),
        ("AFF", "Affitti e locazioni", NaturaCategoria.Uscita),
        ("TAXES", "Imposte e tasse (F24)", NaturaCategoria.Uscita),
        ("INT-PAS", "Interessi passivi e commissioni bancarie", NaturaCategoria.Uscita),
        ("TRAS", "Trasporti e spese di viaggio", NaturaCategoria.Uscita),
        ("CARB", "Carburanti e pedaggi", NaturaCategoria.Uscita),
        ("RIMB", "Rimborsi nota spese dipendenti", NaturaCategoria.Uscita),
        ("ALT-USC", "Altre uscite", NaturaCategoria.Uscita),
    };

    private static readonly IReadOnlyList<AliquotaSeed> DefaultAliquote = new[]
    {
        new AliquotaSeed("I22", "Aliquota ordinaria 22%", 22m, TipoIva.Ordinaria, null),
        new AliquotaSeed("I10", "Aliquota ridotta 10%", 10m, TipoIva.Ordinaria, null),
        new AliquotaSeed("I5",  "Aliquota ridotta 5%",  5m,  TipoIva.Ordinaria, null),
        new AliquotaSeed("I4",  "Aliquota ridotta 4%",  4m,  TipoIva.Ordinaria, null),
        new AliquotaSeed("I0",  "Aliquota 0% (split payment / specifici)", 0m, TipoIva.Ordinaria, null),
        new AliquotaSeed("ESE", "Esente art. 10 DPR 633/72", 0m, TipoIva.Esente, "N4"),
        new AliquotaSeed("NI-8", "Non imponibile art. 8 DPR 633/72 (export)", 0m, TipoIva.NonImponibile, "N3.1"),
        new AliquotaSeed("NI-9", "Non imponibile art. 9 DPR 633/72 (servizi internazionali)", 0m, TipoIva.NonImponibile, "N3.4"),
        new AliquotaSeed("FC",  "Fuori campo IVA art. 2-5 DPR 633/72", 0m, TipoIva.FuoriCampo, "N2.2"),
        new AliquotaSeed("REV-CHG", "Reverse charge (inversione contabile)", 0m, TipoIva.ReverseCharge, "N6"),
    };

    private static readonly IReadOnlyList<CausaleSeed> DefaultCausali = new[]
    {
        new CausaleSeed("INC-FATT", "Incasso fattura cliente", TipoMovimento.Incasso, "VEN"),
        new CausaleSeed("INC-CASH", "Incasso corrispettivo", TipoMovimento.Incasso, "VEN"),
        new CausaleSeed("PAG-FATT", "Pagamento fattura fornitore", TipoMovimento.Pagamento, "ACQ-SERV"),
        new CausaleSeed("PAG-BENI", "Pagamento acquisto beni", TipoMovimento.Pagamento, "ACQ-BENI"),
        new CausaleSeed("PAG-UTE", "Pagamento utenze", TipoMovimento.Pagamento, "UTEN"),
        new CausaleSeed("PAG-AFF", "Pagamento affitto", TipoMovimento.Pagamento, "AFF"),
        new CausaleSeed("STIP", "Stipendio netto", TipoMovimento.StipendioNetto, "STIP"),
        new CausaleSeed("F24", "Pagamento F24", TipoMovimento.F24, "TAXES"),
        new CausaleSeed("GC", "Giroconto interno", TipoMovimento.GirocontoInterno, null),
        new CausaleSeed("RIMB-SPS", "Rimborso nota spese dipendente", TipoMovimento.RimborsoNotaSpese, "RIMB"),
        new CausaleSeed("INT-BC", "Commissioni e interessi bancari", TipoMovimento.Pagamento, "INT-PAS"),
    };

    private readonly AppDbContext db;
    private readonly ILogger<MasterDataSeeder> logger;

    /// <summary>Initializes a new instance of the <see cref="MasterDataSeeder"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    /// <param name="logger">Logger.</param>
    public MasterDataSeeder(AppDbContext db, ILogger<MasterDataSeeder> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    /// <summary>Runs the full seeding sequence.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCategorieAsync(cancellationToken);
        await SeedAliquoteIvaAsync(cancellationToken);
        await SeedCausaliAsync(cancellationToken);
    }

    private async Task SeedCategorieAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Categorie
            .Select(c => c.Codice)
            .ToListAsync(cancellationToken);

        var toInsert = DefaultCategorie
            .Where(c => !existing.Contains(c.Codice, StringComparer.OrdinalIgnoreCase))
            .Select(c => new Categoria(c.Codice, c.Nome, c.Natura))
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        db.Categorie.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} default categorie.", toInsert.Count);
    }

    private async Task SeedAliquoteIvaAsync(CancellationToken cancellationToken)
    {
        var existing = await db.AliquoteIva
            .Select(a => a.Codice)
            .ToListAsync(cancellationToken);

        var toInsert = DefaultAliquote
            .Where(a => !existing.Contains(a.Codice, StringComparer.OrdinalIgnoreCase))
            .Select(MaterializeAliquota)
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        db.AliquoteIva.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} default aliquote IVA.", toInsert.Count);
    }

    private async Task SeedCausaliAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Causali
            .Select(c => c.Codice)
            .ToListAsync(cancellationToken);

        var categorieByCode = await db.Categorie
            .ToDictionaryAsync(c => c.Codice, c => c.Id, cancellationToken);

        var toInsert = DefaultCausali
            .Where(c => !existing.Contains(c.Codice, StringComparer.OrdinalIgnoreCase))
            .Select(c => MaterializeCausale(c, categorieByCode))
            .ToList();

        if (toInsert.Count == 0)
        {
            return;
        }

        db.Causali.AddRange(toInsert);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} default causali.", toInsert.Count);
    }

    private static AliquotaIva MaterializeAliquota(AliquotaSeed a)
    {
        var entity = new AliquotaIva(a.Codice, a.Descrizione, a.Percentuale, a.Tipo);
        if (a.CodiceNatura is not null)
        {
            entity.Update(a.Codice, a.Descrizione, a.Percentuale, 0m, a.Tipo, a.CodiceNatura);
        }

        return entity;
    }

    private static Causale MaterializeCausale(CausaleSeed c, Dictionary<string, Guid> categorieByCode)
    {
        var entity = new Causale(c.Codice, c.Nome, c.Tipo);
        if (c.CategoriaCodice is not null && categorieByCode.TryGetValue(c.CategoriaCodice, out var catId))
        {
            entity.Update(c.Codice, c.Nome, c.Tipo, catId, null);
        }

        return entity;
    }

    private sealed record AliquotaSeed(
        string Codice,
        string Descrizione,
        decimal Percentuale,
        TipoIva Tipo,
        string? CodiceNatura);

    private sealed record CausaleSeed(
        string Codice,
        string Nome,
        TipoMovimento Tipo,
        string? CategoriaCodice);
}
