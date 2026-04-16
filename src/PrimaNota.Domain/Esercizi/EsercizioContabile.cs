using PrimaNota.Domain.Abstractions;

namespace PrimaNota.Domain.Esercizi;

/// <summary>
/// Represents an accounting fiscal year. Italian companies operate on the solar year:
/// the period always runs from 1 January to 31 December. An exercise is uniquely
/// identified by its <see cref="Anno"/>, which doubles as its primary key.
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
}
