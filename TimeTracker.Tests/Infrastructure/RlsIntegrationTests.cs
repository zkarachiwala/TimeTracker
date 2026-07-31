using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;
using Xunit;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// Verifies that SQL Server Row-Level Security filter predicates enforce data isolation
/// even when the application service layer is bypassed entirely (raw DbContext queries).
///
/// These tests use a Testcontainers SQL Server instance (started once per session by
/// SqlServerFixture) so they run in CI and locally without any manual DB setup.
///
/// Tests connect as a dedicated low-privilege SQL login that holds only
/// db_datareader + db_datawriter — the same role as the production Managed Identity.
///
/// Note SQL Server does NOT exempt sa, sysadmin or db_owner from RLS filter predicates; they are
/// filtered like anyone else. The only way past these policies is membership of the rls_bypass
/// role, which the predicates name explicitly. That is why seeding and cleanup below run as a
/// dedicated rls_bypass member rather than as sa — see docs/rls-security-model.md.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Container")]
public class RlsIntegrationTests(SqlServerFixture fixture)
{
    private const string TestLoginName = "timetracker_rls_test";
    private const string TestLoginPassword = "Rls_T3st!Pw#2026";

    // Holds db_datareader + db_datawriter *and* rls_bypass — the shape of the backup principal.
    private const string BypassLoginName = "timetracker_rls_bypass_test";
    private const string BypassLoginPassword = "Rls_Byp4ss!Pw#2026";

    private const string UserA = "user-rls-A";
    private const string UserB = "user-rls-B";

    private string ConnectionAs(string login, string password) =>
        new SqlConnectionStringBuilder(fixture.AdminConnectionString)
        {
            UserID = login,
            Password = password,
            IntegratedSecurity = false,
        }.ConnectionString;

    private string AppConnectionAsTestUser => ConnectionAs(TestLoginName, TestLoginPassword);

    private string AppConnectionAsBypassUser => ConnectionAs(BypassLoginName, BypassLoginPassword);

    private DbContextOptions<TimeTrackerDataContext> OptionsForTestUser() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(AppConnectionAsTestUser)
            .Options;

    private DbContextOptions<TimeTrackerDataContext> OptionsForBypassUser() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(AppConnectionAsBypassUser)
            .Options;

    [Fact]
    public async Task TimeEntries_UserA_CannotSeeUserB_Entries()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForTestUser());
        await SetSessionContextAsync(ctx, UserA);

        var visible = await ctx.TimeEntries.ToListAsync();

        Assert.All(visible, e => Assert.Equal(UserA, e.UserId));
        Assert.DoesNotContain(visible, e => e.UserId == UserB);
    }

    [Fact]
    public async Task Projects_UserA_CannotSeeUserB_Projects()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForTestUser());
        await SetSessionContextAsync(ctx, UserA);

        var visible = await ctx.Projects
            .IgnoreQueryFilters()  // bypass soft-delete filter; RLS should still apply
            .ToListAsync();

        var visibleIds = visible.Select(p => p.Id).ToHashSet();
        Assert.Contains(setup.ProjectA.Id, visibleIds);
        Assert.DoesNotContain(setup.ProjectB.Id, visibleIds);
    }

    [Fact]
    public async Task ProjectUsers_UserA_CannotSeeUserB_Memberships()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForTestUser());
        await SetSessionContextAsync(ctx, UserA);

        var visible = await ctx.ProjectUsers.ToListAsync();

        Assert.All(visible, pu => Assert.Equal(UserA, pu.UserId));
        Assert.DoesNotContain(visible, pu => pu.UserId == UserB);
    }

    // ── rls_bypass ──────────────────────────────────────────────────────────────────────
    //
    // These guard the nightly .bacpac export. SqlPackage issues its own SELECTs with nowhere to set
    // SESSION_CONTEXT, so it can only read data by being an rls_bypass member. If that membership
    // is ever lost, the export silently produces empty tables rather than failing — these tests are
    // what turn that into a visible failure.

    [Fact]
    public async Task TimeEntries_BypassMember_SeesAllUsersEntries()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForBypassUser());
        // Deliberately no SESSION_CONTEXT: the backup principal never sets one either.

        var visible = await ctx.TimeEntries.ToListAsync();

        Assert.Contains(visible, e => e.UserId == UserA);
        Assert.Contains(visible, e => e.UserId == UserB);
    }

    [Fact]
    public async Task Projects_BypassMember_SeesAllProjects()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForBypassUser());

        var visibleIds = await ctx.Projects
            .IgnoreQueryFilters()  // bypass soft-delete filter; RLS is what is under test
            .Select(p => p.Id)
            .ToListAsync();

        Assert.Contains(setup.ProjectA.Id, visibleIds);
        Assert.Contains(setup.ProjectB.Id, visibleIds);
    }

    [Fact]
    public async Task ProjectUsers_BypassMember_SeesAllMemberships()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForBypassUser());

        var visible = await ctx.ProjectUsers.ToListAsync();

        Assert.Contains(visible, pu => pu.UserId == UserA);
        Assert.Contains(visible, pu => pu.UserId == UserB);
    }

    /// <summary>
    /// The negative control. Without this, the tests above could pass simply because the policies
    /// stopped filtering altogether.
    /// </summary>
    [Fact]
    public async Task TimeEntries_NonBypassMember_WithoutSessionContext_SeesNothing()
    {
        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForTestUser());
        // No SESSION_CONTEXT and no rls_bypass: nothing should be visible at all.

        var visible = await ctx.TimeEntries.ToListAsync();

        Assert.Empty(visible);
    }

    // Creates both test logins (as sa, which holds the DDL rights) and seeds data through the
    // rls_bypass member. Seeding cannot run as sa: sa is filtered by these policies, so the
    // SELECTs in cleanup would match nothing and silently leak rows into the shared database.
    private async Task<RlsTestSetup> SetupAsync()
    {
        await using var adminConn = new SqlConnection(fixture.AdminConnectionString);
        await adminConn.OpenAsync();

        // Create the low-privilege test login if it doesn't already exist.
        await ExecuteAdminSqlAsync(adminConn, $"""
            IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'{TestLoginName}')
                CREATE LOGIN [{TestLoginName}] WITH PASSWORD = N'{TestLoginPassword}';
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{TestLoginName}')
            BEGIN
                CREATE USER [{TestLoginName}] FOR LOGIN [{TestLoginName}];
                ALTER ROLE db_datareader ADD MEMBER [{TestLoginName}];
                ALTER ROLE db_datawriter ADD MEMBER [{TestLoginName}];
            END
            """);

        // Same rights, plus rls_bypass — mirrors how the backup principal is configured.
        await ExecuteAdminSqlAsync(adminConn, $"""
            IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'{BypassLoginName}')
                CREATE LOGIN [{BypassLoginName}] WITH PASSWORD = N'{BypassLoginPassword}';
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{BypassLoginName}')
            BEGIN
                CREATE USER [{BypassLoginName}] FOR LOGIN [{BypassLoginName}];
                ALTER ROLE db_datareader ADD MEMBER [{BypassLoginName}];
                ALTER ROLE db_datawriter ADD MEMBER [{BypassLoginName}];
                ALTER ROLE rls_bypass ADD MEMBER [{BypassLoginName}];
            END
            """);

        // Seed through the rls_bypass member so every row is visible for setup and teardown.
        var adminOptions = OptionsForBypassUser();

        await using var ctx = new TimeTrackerDataContext(adminOptions);

        var projectA = new Project { Name = $"RLS-Test-A-{Guid.NewGuid():N}", ProjectUsers = [new ProjectUser { UserId = UserA }] };
        var projectB = new Project { Name = $"RLS-Test-B-{Guid.NewGuid():N}", ProjectUsers = [new ProjectUser { UserId = UserB }] };
        ctx.Projects.AddRange(projectA, projectB);
        await ctx.SaveChangesAsync();

        ctx.TimeEntries.AddRange(
            new TimeEntry { UserId = UserA, ProjectId = projectA.Id, Start = DateTime.UtcNow },
            new TimeEntry { UserId = UserB, ProjectId = projectB.Id, Start = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        return new RlsTestSetup(adminOptions, projectA, projectB);
    }

    private static async Task SetSessionContextAsync(TimeTrackerDataContext ctx, string userId)
    {
        await ctx.Database.OpenConnectionAsync();
        await using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context N'UserId', @userId";
        cmd.Parameters.Add(new SqlParameter("@userId", userId));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteAdminSqlAsync(SqlConnection conn, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class RlsTestSetup(
        DbContextOptions<TimeTrackerDataContext> adminOptions,
        Project projectA,
        Project projectB) : IAsyncDisposable
    {
        public Project ProjectA { get; } = projectA;
        public Project ProjectB { get; } = projectB;

        public async ValueTask DisposeAsync()
        {
            // Clean up test data using admin connection.
            await using var ctx = new TimeTrackerDataContext(adminOptions);
            ctx.TimeEntries.RemoveRange(
                await ctx.TimeEntries.Where(e => e.UserId == UserA || e.UserId == UserB).ToListAsync());
            ctx.ProjectUsers.RemoveRange(
                await ctx.ProjectUsers.Where(pu => pu.UserId == UserA || pu.UserId == UserB).ToListAsync());

            // Re-query projects fresh rather than using the stale entity references from SetupAsync.
            // The stale objects carry their ProjectUsers navigation collection; attaching them would cause
            // EF to generate cascade-delete commands for ProjectUsers that are already gone (deleted above),
            // resulting in DbUpdateConcurrencyException (0 rows affected).
            // IgnoreAutoIncludes() prevents EF loading the navigation; IgnoreQueryFilters() bypasses soft-delete.
            var projects = await ctx.Projects
                .IgnoreAutoIncludes()
                .IgnoreQueryFilters()
                .Where(p => p.Id == ProjectA.Id || p.Id == ProjectB.Id)
                .ToListAsync();
            ctx.Projects.RemoveRange(projects);
            await ctx.SaveChangesAsync();
        }
    }
}
