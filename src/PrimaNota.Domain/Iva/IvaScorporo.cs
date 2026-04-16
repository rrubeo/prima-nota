namespace PrimaNota.Domain.Iva;

/// <summary>
/// Pure helpers to decompose a gross amount into taxable base + VAT given a rate.
/// All results are rounded to 2 decimals (banker's rounding).
/// </summary>
public static class IvaScorporo
{
    /// <summary>
    /// Splits a gross amount into taxable base and VAT.
    /// </summary>
    /// <param name="lordo">Gross amount (signed).</param>
    /// <param name="percentuale">VAT percentage (0..100). A zero percentage leaves the amount untouched.</param>
    /// <returns>Tuple (imponibile, imposta) both rounded to 2 decimals.</returns>
    public static (decimal Imponibile, decimal Imposta) Scorpora(decimal lordo, decimal percentuale)
    {
        if (percentuale <= 0m)
        {
            return (decimal.Round(lordo, 2, MidpointRounding.ToEven), 0m);
        }

        var imponibile = lordo / (1m + (percentuale / 100m));
        var imponibileRounded = decimal.Round(imponibile, 2, MidpointRounding.ToEven);
        var imposta = decimal.Round(lordo - imponibileRounded, 2, MidpointRounding.ToEven);
        return (imponibileRounded, imposta);
    }

    /// <summary>
    /// Returns the VAT amount only (sign-preserving).
    /// </summary>
    /// <param name="lordo">Gross amount.</param>
    /// <param name="percentuale">VAT percentage.</param>
    /// <returns>VAT amount rounded to 2 decimals.</returns>
    public static decimal Imposta(decimal lordo, decimal percentuale) =>
        Scorpora(lordo, percentuale).Imposta;

    /// <summary>
    /// Returns the taxable base only (sign-preserving).
    /// </summary>
    /// <param name="lordo">Gross amount.</param>
    /// <param name="percentuale">VAT percentage.</param>
    /// <returns>Taxable base rounded to 2 decimals.</returns>
    public static decimal Imponibile(decimal lordo, decimal percentuale) =>
        Scorpora(lordo, percentuale).Imponibile;
}
