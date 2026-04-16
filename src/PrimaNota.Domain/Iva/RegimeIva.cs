namespace PrimaNota.Domain.Iva;

/// <summary>VAT regime chosen by the company for a given fiscal year.</summary>
public enum RegimeIva
{
    /// <summary>Ordinary VAT regime: VAT registers + periodic liquidation.</summary>
    Ordinario = 1,

    /// <summary>Flat-rate regime (forfettario): no VAT on sales, no deductible VAT on purchases.</summary>
    Forfettario = 2,
}
