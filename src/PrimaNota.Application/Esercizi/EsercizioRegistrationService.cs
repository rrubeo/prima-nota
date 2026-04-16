using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Esercizi;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Application.Esercizi;

/// <summary>
/// Ensures the accounting exercise for a given year exists. Called at startup and
/// by the yearly Hangfire job scheduled on 1 January.
/// </summary>
public sealed class EsercizioRegistrationService
{
    private readonly IApplicationDbContext db;
    private readonly IDateTimeProvider clock;
    private readonly ILogger<EsercizioRegistrationService> logger;

    /// <summary>Initializes a new instance of the <see cref="EsercizioRegistrationService"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    /// <param name="clock">Clock abstraction.</param>
    /// <param name="logger">Logger.</param>
    public EsercizioRegistrationService(
        IApplicationDbContext db,
        IDateTimeProvider clock,
        ILogger<EsercizioRegistrationService> logger)
    {
        this.db = db;
        this.clock = clock;
        this.logger = logger;
    }

    /// <summary>
    /// Ensures an <see cref="EsercizioContabile"/> exists for the current Italian calendar year.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The year that was ensured.</returns>
    public async Task<int> EnsureCurrentYearAsync(CancellationToken cancellationToken = default)
    {
        var year = clock.TodayItaly.Year;
        return await EnsureYearAsync(year, cancellationToken);
    }

    /// <summary>Ensures an exercise exists for the given year.</summary>
    /// <param name="anno">Year to ensure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ensured year.</returns>
    public async Task<int> EnsureYearAsync(int anno, CancellationToken cancellationToken = default)
    {
        var set = ((DbContext)db).Set<EsercizioContabile>();
        var exists = await set.AnyAsync(e => e.Anno == anno, cancellationToken);
        if (exists)
        {
            return anno;
        }

        set.Add(new EsercizioContabile(anno));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Esercizio contabile {Anno} creato automaticamente.", anno);
        return anno;
    }
}
