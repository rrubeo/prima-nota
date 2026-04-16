using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PrimaNota.Infrastructure.Configuration;

namespace PrimaNota.Infrastructure.BackgroundJobs;

/// <summary>Registers Hangfire on SQL Server with the application DI container.</summary>
public static class HangfireServiceCollectionExtensions
{
    /// <summary>Adds Hangfire services backed by the <c>hangfire</c> SQL Server schema.</summary>
    /// <param name="services">The DI container.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration
            .GetSection(DatabaseOptions.SectionName)[nameof(DatabaseOptions.ConnectionString)]
            ?? throw new InvalidOperationException("Database:ConnectionString is required for Hangfire.");

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true,
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true,
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount;
            options.ServerName = $"primanota-{Environment.MachineName}";
        });

        return services;
    }
}
