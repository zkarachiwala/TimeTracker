using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Web.Data;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// Verifies that EF Core migrations for both DbContexts apply cleanly from scratch.
/// Each test creates an isolated, empty database on the shared SQL Server container
/// and calls MigrateAsync() — if any migration is broken, the test fails before it
/// reaches production.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Container")]
public class MigrationSmokeTests(SqlServerFixture fixture)
{
    [Fact]
    public async Task TimeTrackerDataContext_migrations_apply_cleanly()
    {
        var opts = new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(fixture.CreateIsolatedConnectionString())
            .Options;
        await using var ctx = new TimeTrackerDataContext(opts);
        await ctx.Database.MigrateAsync();
    }

    [Fact]
    public async Task IdentityDataContext_migrations_apply_cleanly()
    {
        var opts = new DbContextOptionsBuilder<IdentityDataContext>()
            .UseSqlServer(fixture.CreateIsolatedConnectionString())
            .Options;
        await using var ctx = new IdentityDataContext(opts);
        await ctx.Database.MigrateAsync();
    }
}
