namespace PrimaNota.Domain.ContiFinanziari;

/// <summary>Reconciliation state of a bank-statement row.</summary>
public enum StatoRiconciliazione
{
    /// <summary>Not yet matched to any movement.</summary>
    DaRiconciliare = 1,

    /// <summary>Matched to a movement (manually or automatically).</summary>
    Riconciliato = 2,

    /// <summary>Explicitly excluded from reconciliation (e.g. internal transfer, fee already booked).</summary>
    Escluso = 3,
}
