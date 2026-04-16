namespace PrimaNota.Domain.PrimaNota;

/// <summary>Lifecycle states of a prima-nota movement.</summary>
public enum StatoMovimento
{
    /// <summary>Draft: freely editable, does not contribute to balances.</summary>
    Draft = 1,

    /// <summary>Confirmed: immutable, contributes to account balances and VAT registers.</summary>
    Confirmed = 2,

    /// <summary>Reconciled: confirmed and linked to a bank statement row (module 10).</summary>
    Reconciled = 3,
}
