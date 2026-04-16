using Hangfire;
using Microsoft.Extensions.Logging;
using PrimaNota.Application.Esercizi;

namespace PrimaNota.Infrastructure.Esercizi;

/// <summary>
/// Hangfire recurring job that ensures an exercise exists for the current year.
/// Registered to fire a few minutes past midnight on 1 January every year.
/// Also usable as an on-demand entry point for administrators.
/// </summary>
public sealed class EsercizioYearlyJob
{
    /// <summary>Canonical job identifier used in Hangfire dashboard.</summary>
    public const string JobId = "esercizio-yearly";

    /// <summary>Recurring cron: at 00:05 on 1 January (Europe/Rome).</summary>
    public const string Cron = "5 0 1 1 *";

    private readonly EsercizioRegistrationService registrationService;
    private readonly ILogger<EsercizioYearlyJob> logger;

    /// <summary>Initializes a new instance of the <see cref="EsercizioYearlyJob"/> class.</summary>
    /// <param name="registrationService">Exercise registration service.</param>
    /// <param name="logger">Logger.</param>
    public EsercizioYearlyJob(
        EsercizioRegistrationService registrationService,
        ILogger<EsercizioYearlyJob> logger)
    {
        this.registrationService = registrationService;
        this.logger = logger;
    }

    /// <summary>Schedules the recurring job on the Hangfire server.</summary>
    /// <param name="recurringJobManager">Hangfire recurring job manager.</param>
    public static void Schedule(IRecurringJobManager recurringJobManager)
    {
        ArgumentNullException.ThrowIfNull(recurringJobManager);
        recurringJobManager.AddOrUpdate<EsercizioYearlyJob>(
            JobId,
            job => job.ExecuteAsync(),
            Cron,
            new RecurringJobOptions
            {
                TimeZone = ResolveItalyTimeZone(),
            });
    }

    /// <summary>Executes the ensure-current-year logic.</summary>
    /// <returns>Task.</returns>
    public async Task ExecuteAsync()
    {
        logger.LogInformation("EsercizioYearlyJob started.");
        var year = await registrationService.EnsureCurrentYearAsync();
        logger.LogInformation("EsercizioYearlyJob completed. Ensured year {Year}.", year);
    }

    private static TimeZoneInfo ResolveItalyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }
}
