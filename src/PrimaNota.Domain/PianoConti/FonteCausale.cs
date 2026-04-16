namespace PrimaNota.Domain.PianoConti;

/// <summary>
/// Distinguishes whether an <see cref="TipoMovimento.Incasso"/> causale represents
/// a formal invoice (fattura, art. 23 DPR 633/72) or a daily-aggregate retail sale
/// (corrispettivo, art. 24 DPR 633/72). Used by the VAT-register query to split the
/// rows between the Vendite and Corrispettivi registers. Null for non-Incasso causali.
/// </summary>
public enum FonteCausale
{
    /// <summary>Formal invoice issued to a customer (B2B, B2G).</summary>
    Fattura = 1,

    /// <summary>Daily retail sale without a formal invoice (B2C, registratore telematico).</summary>
    Corrispettivo = 2,
}
