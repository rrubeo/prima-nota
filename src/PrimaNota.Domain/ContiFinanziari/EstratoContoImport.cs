using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>
/// Aggregate that represents a single bank-statement import session. Owns the parsed
/// <see cref="RigaEstrattoConto"/> rows and tracks the reconciliation progress.
/// </summary>
public sealed class EstratoContoImport : AuditableEntity<Guid>
{
    private readonly List<RigaEstrattoConto> righe = new();

    /// <summary>Initializes a new instance of the <see cref="EstratoContoImport"/> class.</summary>
    /// <param name="contoFinanziarioId">Financial account this statement belongs to.</param>
    /// <param name="nomeFile">Original uploaded file name.</param>
    /// <param name="periodoDa">Statement period start.</param>
    /// <param name="periodoA">Statement period end.</param>
    /// <param name="saldoContabile">Reported balance at statement date (if available).</param>
    public EstratoContoImport(
        Guid contoFinanziarioId,
        string nomeFile,
        DateOnly periodoDa,
        DateOnly periodoA,
        decimal? saldoContabile)
    {
        if (contoFinanziarioId == Guid.Empty)
        {
            throw new ArgumentException("Conto finanziario obbligatorio.", nameof(contoFinanziarioId));
        }

        Id = Guid.NewGuid();
        ContoFinanziarioId = contoFinanziarioId;
        NomeFile = string.IsNullOrWhiteSpace(nomeFile) ? "import.pdf" : nomeFile.Trim();
        PeriodoDa = periodoDa;
        PeriodoA = periodoA;
        SaldoContabile = saldoContabile;
    }

    private EstratoContoImport()
    {
    }

    /// <summary>Gets the financial account this statement belongs to.</summary>
    public Guid ContoFinanziarioId { get; private set; }

    /// <summary>Gets the original file name.</summary>
    public string NomeFile { get; private set; } = string.Empty;

    /// <summary>Gets the statement period start.</summary>
    public DateOnly PeriodoDa { get; private set; }

    /// <summary>Gets the statement period end.</summary>
    public DateOnly PeriodoA { get; private set; }

    /// <summary>Gets the reported balance at statement date.</summary>
    public decimal? SaldoContabile { get; private set; }

    /// <summary>Gets the parsed rows.</summary>
    public IReadOnlyList<RigaEstrattoConto> Righe => righe;

    /// <summary>Gets the number of rows not yet reconciled.</summary>
    public int RigheDaRiconciliare => righe.Count(r => r.Stato == StatoRiconciliazione.DaRiconciliare);

    /// <summary>Adds a parsed row to this import.</summary>
    /// <param name="riga">Parsed row.</param>
    public void AddRiga(RigaEstrattoConto riga)
    {
        ArgumentNullException.ThrowIfNull(riga);
        riga.ImportId = Id;
        righe.Add(riga);
    }
}
