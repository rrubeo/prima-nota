using Microsoft.EntityFrameworkCore;
using PrimaNota.Infrastructure.Persistence;
using PrimaNota.IntegrationTests.Fixtures;

namespace PrimaNota.IntegrationTests.Persistence;

/// <summary>
/// Smoke integration test that validates the Initial EF migration can be applied
/// against a real SQL Server 2022 instance. Skipped automatically if Docker is
/// not available locally — always runs in CI where Docker is provisioned.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class AppDbContextMigrationTests
{
    private readonly SqlServerContainerFixture fixture;

    public AppDbContextMigrationTests(SqlServerContainerFixture fixture)
    {
        this.fixture = fixture;
    }

    [SkippableFact]
    public async Task Migrate_Should_Create_Migrations_History_Table_In_App_Schema()
    {
        Skip.IfNot(
            fixture.IsAvailable,
            $"Docker is not available on this host ({fixture.UnavailableReason}). This test runs automatically in CI.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(fixture.ConnectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "app"))
            .Options;

        await using var ctx = new AppDbContext(options);

        await ctx.Database.MigrateAsync();

        var appliedMigrations = (await ctx.Database.GetAppliedMigrationsAsync()).ToList();
        appliedMigrations.Should().ContainSingle(m => m.EndsWith("_Initial", StringComparison.Ordinal));
    }
}
