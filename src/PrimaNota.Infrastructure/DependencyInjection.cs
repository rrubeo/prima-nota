using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrimaNota.Application.Abstractions;
using PrimaNota.Infrastructure.Clock;
using PrimaNota.Infrastructure.Configuration;
using PrimaNota.Infrastructure.Identity;
using PrimaNota.Infrastructure.Persistence;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure;

/// <summary>Entry-point for wiring the Infrastructure layer into the DI container.</summary>
public static class DependencyInjection
{
    private static readonly string[] SqlServerHealthCheckTags = new[] { "ready", "db" };

    /// <summary>
    /// Registers infrastructure services (DB, clock, audit, identity placeholders).
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="configuration">The app configuration root.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, AnonymousCurrentUserService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var dbOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
            var interceptor = provider.GetRequiredService<AuditSaveChangesInterceptor>();
            options
                .UseSqlServer(dbOptions.ConnectionString, sql =>
                {
                    sql.CommandTimeout(dbOptions.CommandTimeoutSeconds);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "app");
                    sql.EnableRetryOnFailure(maxRetryCount: 3);
                })
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    /// <summary>Adds SQL Server connectivity to the health checks pipeline.</summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="configuration">The app configuration root.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(
        this IHealthChecksBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetSection(DatabaseOptions.SectionName)[nameof(DatabaseOptions.ConnectionString)]
            ?? string.Empty;

        return builder.AddSqlServer(
            connectionString,
            name: "sqlserver",
            failureStatus: HealthStatus.Unhealthy,
            tags: SqlServerHealthCheckTags);
    }
}
