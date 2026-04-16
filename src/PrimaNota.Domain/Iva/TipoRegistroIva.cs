namespace PrimaNota.Domain.Iva;

/// <summary>Kind of Italian VAT register.</summary>
public enum TipoRegistroIva
{
    /// <summary>Sales register (fatture emesse).</summary>
    Vendite = 1,

    /// <summary>Purchases register (fatture ricevute).</summary>
    Acquisti = 2,

    /// <summary>Retail/till register (corrispettivi).</summary>
    Corrispettivi = 3,
}
