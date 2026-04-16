using PrimaNota.Domain.Iva;

namespace PrimaNota.Application.Iva;

/// <summary>
/// A VAT liquidation period. Combines a fiscal year with an index within the year;
/// for <see cref="PeriodicitaIva.Mensile"/> the index is 1..12, for
/// <see cref="PeriodicitaIva.Trimestrale"/> it is 1..4.
/// </summary>
public sealed record IvaPeriodo(int Anno, PeriodicitaIva Periodicita, int Indice)
{
    /// <summary>Gets the inclusive start date of this period.</summary>
    public DateOnly DataInizio => Periodicita == PeriodicitaIva.Mensile
        ? new DateOnly(Anno, Indice, 1)
        : new DateOnly(Anno, ((Indice - 1) * 3) + 1, 1);

    /// <summary>Gets the inclusive end date of this period.</summary>
    public DateOnly DataFine
    {
        get
        {
            if (Periodicita == PeriodicitaIva.Mensile)
            {
                var lastDay = DateTime.DaysInMonth(Anno, Indice);
                return new DateOnly(Anno, Indice, lastDay);
            }

            var endMonth = Indice * 3;
            var endDay = DateTime.DaysInMonth(Anno, endMonth);
            return new DateOnly(Anno, endMonth, endDay);
        }
    }

    /// <summary>Gets a human-readable label (e.g. "Marzo 2026", "Q1 2026").</summary>
    public string Label => Periodicita == PeriodicitaIva.Mensile
        ? DataInizio.ToString("MMMM yyyy", new System.Globalization.CultureInfo("it-IT"))
        : $"Q{Indice} {Anno}";

    /// <summary>Enumerates all periods of the year for the given frequency.</summary>
    /// <param name="anno">Fiscal year.</param>
    /// <param name="periodicita">Monthly or quarterly.</param>
    /// <returns>All periods in chronological order.</returns>
    public static IEnumerable<IvaPeriodo> Elenco(int anno, PeriodicitaIva periodicita)
    {
        var count = periodicita == PeriodicitaIva.Mensile ? 12 : 4;
        for (var i = 1; i <= count; i++)
        {
            yield return new IvaPeriodo(anno, periodicita, i);
        }
    }
}
