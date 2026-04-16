namespace PrimaNota.Domain.Esercizi;

/// <summary>Lifecycle states of an accounting exercise.</summary>
public enum StatoEsercizio
{
    /// <summary>The exercise is currently open and accepts new movements.</summary>
    Aperto = 0,

    /// <summary>Closing procedure in progress — new movements are blocked, read-only access remains.</summary>
    InChiusura = 1,

    /// <summary>The exercise has been closed and is read-only.</summary>
    Chiuso = 2,
}
