using PrimaNota.Domain.Abstractions;
using PrimaNota.Domain.Iva;

namespace PrimaNota.Domain.Esercizi;

/// <summary>
/// Represents an accounting fiscal year. Italian companies operate on the solar year:
/// the period always runs from 1 January to 31 December. An exercise is uniquely
/// identified by its <see cref="Anno"/>, which doubles as its primary key. Every
/// exercise carries its own VAT regime configuration, so a company can change regime
/// year over year without polluting the global configuration.
/// </summary>
public sealed class EsercizioContabile : AuditableEntity<int>
{
    /// <summary>Initializes a new instance of the <see cref="EsercizioContabile"/> class for the given solar year.</summary>
    /// <param name="anno">Four-digit year (e.g. 2026).</param>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="anno"/> is not a plausible year.</exception>
    public EsercizioContabile(int anno)
    {
        if (anno < 2000 || anno > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(anno), "Anno fuori range supportato (2000-2100).");
        }

        Id = anno;
        Anno = anno;
        DataInizio = new DateOnly(anno, 1, 1);
        DataFine = new DateOnly(anno, 12, 31);
        Stato = StatoEsercizio.Aperto;
        RegimeIva = RegimeIva.Ordinario;
        PeriodicitaIva = PeriodicitaIva.Trimestrale;
    }

    /// <summary>Initializes a new instance of the <see cref="EsercizioContabile"/> class. EF Core constructor.</summary>
    private EsercizioContabile()
    {
    }

    /// <summary>Gets the solar year of this exercise.</summary>
    public int Anno { get; private set; }

    /// <summary>Gets the exercise start date (always 1 January).</summary>
    public DateOnly DataInizio { get; private set; }

    /// <summary>Gets the exercise end date (always 31 December).</summary>
    public DateOnly DataFine { get; private set; }

    /// <summary>Gets the current state of the exercise.</summary>
    public StatoEsercizio Stato { get; private set; }

    /// <summary>Gets the UTC timestamp when the exercise was closed, if any.</summary>
    public DateTimeOffset? DataChiusura { get; private set; }

    /// <summary>Gets the VAT regime applicable during this exercise.</summary>
    public RegimeIva RegimeIva { get; private set; }

    /// <summary>Gets the VAT liquidation frequency (only meaningful when <see cref="RegimeIva"/> is Ordinario).</summary>
    public PeriodicitaIva PeriodicitaIva { get; private set; }

    /// <summary>
    /// Gets the profitability coefficient applied to gross revenue under the flat-rate regime
    /// to derive the taxable income. Typical values by activity: 40, 62, 67, 78, 86 %.
    /// Null when <see cref="RegimeIva"/> is Ordinario.
    /// </summary>
    public decimal? CoefficienteRedditivitaForfettario { get; private set; }

    /// <summary>Transitions the exercise into <see cref="StatoEsercizio.InChiusura"/>.</summary>
    public void MarkInChiusura()
    {
        if (Stato != StatoEsercizio.Aperto)
        {
            throw new InvalidOperationException($"Impossibile passare a InChiusura dallo stato {Stato}.");
        }

        Stato = StatoEsercizio.InChiusura;
    }

    /// <summary>Closes the exercise. Once closed, no new movements can be attached.</summary>
    /// <param name="closedAt">UTC instant of closure.</param>
    public void Chiudi(DateTimeOffset closedAt)
    {
        if (Stato == StatoEsercizio.Chiuso)
        {
            return;
        }

        Stato = StatoEsercizio.Chiuso;
        DataChiusura = closedAt;
    }

    /// <summary>Reverts the exercise back to <see cref="StatoEsercizio.Aperto"/> (reversibility window).</summary>
    public void Riapri()
    {
        Stato = StatoEsercizio.Aperto;
        DataChiusura = null;
    }

    /// <summary>
    /// Configures the VAT regime and liquidation frequency for this exercise.
    /// Meaningful only before any confirmed movement has been attached; for open
    /// exercises with movements, changing the regime mid-year should be avoided and
    /// is a business decision flagged to the user by the application layer.
    /// </summary>
    /// <param name="regime">Desired regime.</param>
    /// <param name="periodicita">Liquidation frequency (ignored for Forfettario).</param>
    /// <param name="coefficienteRedditivita">Profitability coefficient (0..100) required for Forfettario.</param>
    public void ConfiguraIva(RegimeIva regime, PeriodicitaIva periodicita, decimal? coefficienteRedditivita)
    {
        if (Stato == StatoEsercizio.Chiuso)
        {
            throw new InvalidOperationException("Esercizio chiuso: impossibile modificare la configurazione IVA.");
        }

        if (regime == RegimeIva.Forfettario)
        {
            if (coefficienteRedditivita is not { } c || c is <= 0m or > 100m)
            {
                throw new ArgumentException(
                    "Per il regime forfettario e richiesto un coefficiente di redditivita (0-100%).",
                    nameof(coefficienteRedditivita));
            }

            RegimeIva = RegimeIva.Forfettario;
            PeriodicitaIva = periodicita;
            CoefficienteRedditivitaForfettario = decimal.Round(c, 2, MidpointRounding.ToEven);
        }
        else
        {
            RegimeIva = RegimeIva.Ordinario;
            PeriodicitaIva = periodicita;
            CoefficienteRedditivitaForfettario = null;
        }
    }
}
