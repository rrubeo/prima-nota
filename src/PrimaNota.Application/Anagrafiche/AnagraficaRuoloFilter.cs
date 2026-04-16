namespace PrimaNota.Application.Anagrafiche;

/// <summary>Role filter used when listing anagrafiche.</summary>
public enum AnagraficaRuoloFilter
{
    /// <summary>All roles.</summary>
    Tutti = 0,

    /// <summary>Only customers.</summary>
    Clienti = 1,

    /// <summary>Only suppliers.</summary>
    Fornitori = 2,

    /// <summary>Only employees.</summary>
    Dipendenti = 3,
}
