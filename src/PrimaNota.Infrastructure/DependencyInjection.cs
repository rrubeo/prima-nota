using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Esercizi;
using PrimaNota.Infrastructure.Audit;
using PrimaNota.Infrastructure.Clock;
using PrimaNota.Infrastructure.Configuration;
using PrimaNota.Infrastructure.Esercizi;
using PrimaNota.Infrastructure.Identity;
using PrimaNota.Infrastructure.Persistence;
using PrimaNota.Shared.Clock;

namespace PrimaNota.Infrastructure;

/// <summary>Entry-point for wiring the Infrastructure layer into the DI container.</summary>
public static class DependencyInjection
{
    private static readonly string[] SqlServerHealthCheckTags = new[] { "ready", "db" };

    /// <summary>
    /// Registers infrastructure services: database, clock, identity, audit pipeline.
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

        services.AddOptions<IdentityBootstrapOptions>()
            .Bind(configuration.GetSection(IdentityBootstrapOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
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

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IdentitySeeder>();
        services.AddScoped<MasterDataSeeder>();
        services.AddScoped<IEsercizioContext, EsercizioContext>();
        services.AddScoped<EsercizioRegistrationService>();
        services.AddScoped<EsercizioYearlyJob>();
        services.AddScoped<IAuditLogger, AuditLogger>();

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

    /// <summary>
    /// Applies pending EF migrations (if any) and seeds roles and bootstrap admin.
    /// Intended to be called at application startup.
    /// </summary>
    /// <param name="services">The service provider of the running host.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when initialization is done.</returns>
    public static async Task InitializeInfrastructureAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var seeder = provider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync(cancellationToken);

        var esercizi = provider.GetRequiredService<EsercizioRegistrationService>();
        await esercizi.EnsureCurrentYearAsync(cancellationToken);

        var masterData = provider.GetRequiredService<MasterDataSeeder>();
        await masterData.SeedAsync(cancellationToken);
    }
}
