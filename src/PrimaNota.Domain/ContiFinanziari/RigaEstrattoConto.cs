namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>
/// Single row parsed from a bank statement PDF. Owned by <see cref="EstratoContoImport"/>.
/// Carries the raw fields extracted from the PDF plus a reconciliation state that tracks
/// whether the row has been matched to a <see cref="PrimaNota.MovimentoPrimaNota"/> (via a
/// <see cref="PrimaNota.PagamentoMovimento"/>).
/// </summary>
public sealed class RigaEstrattoConto
{
    /// <summary>Initializes a new instance of the <see cref="RigaEstrattoConto"/> class.</summary>
    /// <param name="dataContabile">Accounting date.</param>
    /// <param name="dataValuta">Value date.</param>
    /// <param name="causaleOperazione">Bank cause code.</param>
    /// <param name="operazione">Operation name.</param>
    /// <param name="descrizione">Free-text description.</param>
    /// <param name="importo">Signed amount (positive=credit, negative=debit).</param>
    public RigaEstrattoConto(
        DateOnly dataContabile,
        DateOnly dataValuta,
        string? causaleOperazione,
        string? operazione,
        string? descrizione,
        decimal importo)
    {
        Id = Guid.NewGuid();
        DataContabile = dataContabile;
        DataValuta = dataValuta;
        CausaleOperazione = causaleOperazione?.Trim();
        Operazione = operazione?.Trim();
        Descrizione = descrizione?.Trim();
        Importo = decimal.Round(importo, 2, MidpointRounding.ToEven);
        Stato = StatoRiconciliazione.DaRiconciliare;
    }

    private RigaEstrattoConto()
    {
    }

    /// <summary>Gets the row identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent import identifier.</summary>
    public Guid ImportId { get; internal set; }

    /// <summary>Gets the accounting date.</summary>
    public DateOnly DataContabile { get; private set; }

    /// <summary>Gets the value date.</summary>
    public DateOnly DataValuta { get; private set; }

    /// <summary>Gets the bank operation cause code (e.g. "48", "26", "PO").</summary>
    public string? CausaleOperazione { get; private set; }

    /// <summary>Gets the operation name (e.g. "BONIFICO SEPA").</summary>
    public string? Operazione { get; private set; }

    /// <summary>Gets the free-text description.</summary>
    public string? Descrizione { get; private set; }

    /// <summary>Gets the signed amount (positive = credit/entrata, negative = debit/uscita).</summary>
    public decimal Importo { get; private set; }

    /// <summary>Gets the reconciliation state.</summary>
    public StatoRiconciliazione Stato { get; private set; }

    /// <summary>Gets the linked movement id (set when reconciled).</summary>
    public Guid? MovimentoId { get; private set; }

    /// <summary>Gets the linked payment id (set when reconciled to a specific payment).</summary>
    public Guid? PagamentoId { get; private set; }

    /// <summary>Marks the row as reconciled against a movement and optionally a payment.</summary>
    /// <param name="movimentoId">Movement id.</param>
    /// <param name="pagamentoId">Payment id (nullable).</param>
    public void Riconcilia(Guid movimentoId, Guid? pagamentoId)
    {
        if (movimentoId == Guid.Empty)
        {
            throw new ArgumentException("MovimentoId obbligatorio.", nameof(movimentoId));
        }

        MovimentoId = movimentoId;
        PagamentoId = pagamentoId;
        Stato = StatoRiconciliazione.Riconciliato;
    }

    /// <summary>Marks the row as excluded from reconciliation.</summary>
    public void Escludi()
    {
        MovimentoId = null;
        PagamentoId = null;
        Stato = StatoRiconciliazione.Escluso;
    }

    /// <summary>Resets the row back to pending.</summary>
    public void ResetRiconciliazione()
    {
        MovimentoId = null;
        PagamentoId = null;
        Stato = StatoRiconciliazione.DaRiconciliare;
    }
}
