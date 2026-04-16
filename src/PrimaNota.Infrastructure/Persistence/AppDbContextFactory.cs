using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PrimaNota.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> tooling to instantiate <see cref="AppDbContext"/>
/// without booting the Web host. Reads the connection string from the environment variable
/// <c>PRIMANOTA_CONNECTION</c>, falling back to a localdb default so that migrations can be
/// generated (but not applied) even on developer machines without SQL Server configured.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultDesignTimeConnection =
        "Server=(localdb)\\mssqllocaldb;Database=PrimaNota_Design;Trusted_Connection=True;TrustServerCertificate=True;";

    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PRIMANOTA_CONNECTION")
            ?? DefaultDesignTimeConnection;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "app"))
            .Options;

        return new AppDbContext(options);
    }
}
