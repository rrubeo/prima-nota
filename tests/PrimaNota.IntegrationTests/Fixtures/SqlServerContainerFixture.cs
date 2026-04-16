using Testcontainers.MsSql;

namespace PrimaNota.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection fixture that spins up a SQL Server 2022 container once per test
/// assembly and tears it down at the end. If Docker is not available on the host
/// (common on developer workstations that opted out of Docker), the fixture sets
/// <see cref="IsAvailable"/> to <c>false</c> and tests are expected to skip via
/// <see cref="Skip.IfNot(bool, string)"/>.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private MsSqlContainer? container;

    /// <summary>Gets a value indicating whether the container started successfully.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Gets the reason why the container is not available, if applicable.</summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>Gets the connection string to the started SQL Server instance.</summary>
    /// <exception cref="InvalidOperationException">If the container did not start.</exception>
    public string ConnectionString
    {
        get
        {
            if (!IsAvailable || container is null)
            {
                throw new InvalidOperationException(
                    $"SQL Server container is not available: {UnavailableReason ?? "unknown reason"}.");
            }

            return container.GetConnectionString();
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        try
        {
            container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Strong_Password_123!")
                .Build();

            await container.StartAsync();
            IsAvailable = true;
        }
#pragma warning disable CA1031 // Any failure here (Docker missing, image pull failure, port conflict) must be reported as "unavailable", not crash the test run.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            IsAvailable = false;
            UnavailableReason = ex.Message.Split('\n')[0].Trim();
        }
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}

/// <summary>xUnit collection marker that binds tests to a single shared SQL Server container.</summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    /// <summary>Collection name used by <see cref="CollectionAttribute"/>.</summary>
    public const string Name = "SqlServer";
}
