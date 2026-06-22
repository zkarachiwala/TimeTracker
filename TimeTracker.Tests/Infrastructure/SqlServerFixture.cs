using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using TimeTracker.Web.Data;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

/// <summary>
/// Starts a single SQL Server container per test session and applies both EF Core
/// migrations so that every test in the [Collection("SqlServer")] collection shares
/// one pre-migrated database instance.
///
/// Tests that need an isolated, empty database (e.g. migration smoke tests) should
/// call CreateIsolatedConnectionString() to spin up a fresh database on the same
/// container rather than using the shared AdminConnectionString directly.
/// </summary>
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithAutoRemove(true)
        .Build();

    /// <summary>
    /// SA-level connection string to the shared, pre-migrated database.
    /// Use this in tests that need to seed or inspect data (e.g. RLS tests).
    /// </summary>
    public string AdminConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        AdminConnectionString = _container.GetConnectionString();
        await ApplyMigrationsAsync(AdminConnectionString);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Returns a connection string pointing to a freshly created, empty database
    /// on the same container. Useful for migration smoke tests that need to verify
    /// migrations apply cleanly from scratch without touching the shared DB.
    /// </summary>
    public string CreateIsolatedConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(AdminConnectionString)
        {
            InitialCatalog = $"isolated_{Guid.NewGuid():N}"
        };
        return builder.ConnectionString;
    }

    private static async Task ApplyMigrationsAsync(string connectionString)
    {
        var trackerOpts = new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var trackerCtx = new TimeTrackerDataContext(trackerOpts);
        await trackerCtx.Database.MigrateAsync();

        var identityOpts = new DbContextOptionsBuilder<IdentityDataContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var identityCtx = new IdentityDataContext(identityOpts);
        await identityCtx.Database.MigrateAsync();
    }
}
