namespace PrimaNota.Domain.Iva;

/// <summary>Frequency of the periodic VAT liquidation for ordinary-regime companies.</summary>
public enum PeriodicitaIva
{
    /// <summary>Monthly liquidation (mandatory above certain turnover thresholds).</summary>
    Mensile = 1,

    /// <summary>Quarterly liquidation (available below thresholds).</summary>
    Trimestrale = 2,
}
