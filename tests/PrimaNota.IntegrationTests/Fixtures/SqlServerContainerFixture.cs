using Testcontainers.MsSql;

namespace PrimaNota.IntegrationTests.Fixtures;

/// <summary>
/// xUnit collection fixture that spins up a SQL Server 2022 container once per test
/// assembly and tears it down at the end. Tests retrieve the connection string via
/// <see cref="ConnectionString"/> and are responsible for creating/disposing the DbContext.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Strong_Password_123!")
        .Build();

    /// <summary>Gets the connection string to the started SQL Server instance.</summary>
    public string ConnectionString => container.GetConnectionString();

    /// <inheritdoc />
    public Task InitializeAsync() => container.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

/// <summary>xUnit collection marker that binds tests to a single shared SQL Server container.</summary>
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>
{
    /// <summary>Collection name used by <see cref="CollectionAttribute"/>.</summary>
    public const string Name = "SqlServer";
}
