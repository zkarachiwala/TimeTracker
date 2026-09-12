using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// Verifies the ClientRequestId filtered unique index (Stage B offline-sync idempotency — see
/// docs/plans/stage-b-start-timer-offline-plan.md) against a real SQL Server engine. This is
/// exactly the gotcha the filter exists for: a plain unique index in SQL Server only tolerates a
/// single NULL row (unlike Postgres), and EF Core InMemory doesn't enforce unique indexes at all,
/// so neither the fast suite nor a naive index could ever have caught a regression here.
///
/// Each test runs against a freshly migrated, isolated database (SqlServerFixture). Queries go
/// through a dedicated rls_bypass member, not the sa admin login — despite what its name suggests,
/// sa/db_owner is NOT exempt from the app.TimeEntries FILTER PREDICATE (see RlsIntegrationTests
/// and migration RemoveDbOwnerRlsExemption); only explicit rls_bypass role membership is.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Container")]
public class ClientRequestIdUniqueIndexTests(SqlServerFixture fixture)
{
    private const string BypassLoginName = "timetracker_idx_test_bypass";
    private const string BypassLoginPassword = "Idx_Byp4ss!Pw#2026";

    private async Task<TimeTrackerDataContext> NewIsolatedContextAsync()
    {
        var isolatedConnectionString = fixture.CreateIsolatedConnectionString();

        var adminOpts = new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(isolatedConnectionString).Options;
        await using (var adminCtx = new TimeTrackerDataContext(adminOpts))
            await adminCtx.Database.MigrateAsync();

        await using (var adminConn = new SqlConnection(isolatedConnectionString))
        {
            await adminConn.OpenAsync();
            await using var cmd = new SqlCommand($"""
                IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'{BypassLoginName}')
                    CREATE LOGIN [{BypassLoginName}] WITH PASSWORD = N'{BypassLoginPassword}';
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{BypassLoginName}')
                BEGIN
                    CREATE USER [{BypassLoginName}] FOR LOGIN [{BypassLoginName}];
                    ALTER ROLE db_datareader ADD MEMBER [{BypassLoginName}];
                    ALTER ROLE db_datawriter ADD MEMBER [{BypassLoginName}];
                    ALTER ROLE rls_bypass ADD MEMBER [{BypassLoginName}];
                END
                """, adminConn);
            await cmd.ExecuteNonQueryAsync();
        }

        var bypassConnectionString = new SqlConnectionStringBuilder(isolatedConnectionString)
        {
            UserID = BypassLoginName,
            Password = BypassLoginPassword,
            IntegratedSecurity = false,
        }.ConnectionString;

        var opts = new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(bypassConnectionString).Options;
        return new TimeTrackerDataContext(opts);
    }

    private static Project NewProject() => new() { Name = $"Idx-Test-{Guid.NewGuid():N}" };

    [Fact]
    public async Task Insert_TwoEntries_WithSameClientRequestId_ThrowsUniqueConstraintViolation()
    {
        await using var ctx = await NewIsolatedContextAsync();
        var project = NewProject();
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();

        var tag = Guid.NewGuid();
        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow, ClientRequestId = tag });
        await ctx.SaveChangesAsync();

        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow, ClientRequestId = tag });
        await Assert.ThrowsAsync<DbUpdateException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Insert_TwoEntries_WithNullClientRequestId_BothSucceed()
    {
        // The whole reason for HasFilter("[ClientRequestId] IS NOT NULL") on the index: every
        // entry created the normal online way has a null tag, so without the filter, only one
        // such entry could ever exist per project — a plain unique index in SQL Server only
        // tolerates a single NULL, unlike Postgres.
        await using var ctx = await NewIsolatedContextAsync();
        var project = NewProject();
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();

        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow });
        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await ctx.TimeEntries.CountAsync(e => e.ProjectId == project.Id));
    }

    [Fact]
    public async Task Insert_TwoEntries_WithDifferentClientRequestIds_BothSucceed()
    {
        await using var ctx = await NewIsolatedContextAsync();
        var project = NewProject();
        ctx.Projects.Add(project);
        await ctx.SaveChangesAsync();

        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow, ClientRequestId = Guid.NewGuid() });
        ctx.TimeEntries.Add(new TimeEntry { ProjectId = project.Id, UserId = "user-1", Start = DateTime.UtcNow, ClientRequestId = Guid.NewGuid() });
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await ctx.TimeEntries.CountAsync(e => e.ProjectId == project.Id));
    }
}
