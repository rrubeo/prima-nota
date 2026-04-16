namespace PrimaNota.Domain.PianoConti;

/// <summary>Business operation kind of a prima-nota movement.</summary>
public enum TipoMovimento
{
    /// <summary>Incoming payment (invoice collected, cash sale, ...).</summary>
    Incasso = 1,

    /// <summary>Outgoing payment (supplier, utility, tax, ...).</summary>
    Pagamento = 2,

    /// <summary>Transfer between two internal accounts (e.g. ATM withdrawal).</summary>
    GirocontoInterno = 3,

    /// <summary>Net salary paid to an employee.</summary>
    StipendioNetto = 4,

    /// <summary>F24 tax payment (IRPEF, INPS, VAT, ...).</summary>
    F24 = 5,

    /// <summary>Employee expense-note reimbursement.</summary>
    RimborsoNotaSpese = 6,

    /// <summary>Any other movement not fitting the above.</summary>
    Altro = 99,
}
