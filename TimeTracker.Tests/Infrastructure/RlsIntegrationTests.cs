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
/// These tests connect as a dedicated low-privilege SQL login that holds only
/// db_datareader + db_datawriter — the same role as the production Managed Identity.
/// 'sa' / db_owner users are exempt from RLS by SQL Server design; these tests would
/// trivially pass for them, which is why they use a separate unprivileged connection.
///
/// HOW TO RUN LOCALLY:
///   1. Ensure the app is configured with a running local SQL Server and migrations applied.
///   2. Set these environment variables:
///        SQL_SERVER_RLS_TESTS=true
///        SQL_SERVER_ADMIN_CONNECTION=Server=localhost,1435;Database=TimeTrackerDb;User Id=sa;Password=<pwd>;TrustServerCertificate=true
///        SQL_SERVER_APP_CONNECTION=Server=localhost,1435;Database=TimeTrackerDb;TrustServerCertificate=true
///      (The admin connection is used only for setup/teardown of the test login.)
///   3. dotnet test --filter "FullyQualifiedName~RlsIntegrationTests"
///
/// These tests are always skipped in CI — they are local validation that RLS policies
/// are correctly applied in a real SQL Server environment.
/// </summary>
[Collection("RlsIntegration")]
public class RlsIntegrationTests
{
    private const string TestLoginName = "timetracker_rls_test";
    private const string TestLoginPassword = "Rls_T3st!Pw#2026";
    private const string UserA = "user-rls-A";
    private const string UserB = "user-rls-B";

    private static bool RlsTestsEnabled =>
        Environment.GetEnvironmentVariable("SQL_SERVER_RLS_TESTS") == "true";

    private static string? AdminConnection =>
        Environment.GetEnvironmentVariable("SQL_SERVER_ADMIN_CONNECTION");

    private static string? AppConnectionTemplate =>
        Environment.GetEnvironmentVariable("SQL_SERVER_APP_CONNECTION");

    private static string AppConnectionAsTestUser =>
        new SqlConnectionStringBuilder(AppConnectionTemplate)
        {
            UserID = TestLoginName,
            Password = TestLoginPassword,
            IntegratedSecurity = false,
        }.ConnectionString;

    private static DbContextOptions<TimeTrackerDataContext> OptionsForTestUser() =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(AppConnectionAsTestUser)
            .Options;

    [Fact]
    public async Task TimeEntries_UserA_CannotSeeUserB_Entries()
    {
        if (!RlsTestsEnabled) return; // opt-in only — set SQL_SERVER_RLS_TESTS=true to run locally

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
        if (!RlsTestsEnabled) return; // opt-in only — set SQL_SERVER_RLS_TESTS=true to run locally

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
        if (!RlsTestsEnabled) return; // opt-in only — set SQL_SERVER_RLS_TESTS=true to run locally

        await using var setup = await SetupAsync();

        await using var ctx = new TimeTrackerDataContext(OptionsForTestUser());
        await SetSessionContextAsync(ctx, UserA);

        var visible = await ctx.ProjectUsers.ToListAsync();

        Assert.All(visible, pu => Assert.Equal(UserA, pu.UserId));
        Assert.DoesNotContain(visible, pu => pu.UserId == UserB);
    }

    // Seeds test data as admin (sa bypasses RLS) and creates the unprivileged test login.
    private static async Task<RlsTestSetup> SetupAsync()
    {
        await using var adminConn = new SqlConnection(AdminConnection);
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

        // Seed data using admin connection (bypasses RLS — intentional).
        var adminOptions = new DbContextOptionsBuilder<TimeTrackerDataContext>()
            .UseSqlServer(AdminConnection)
            .Options;

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
            ctx.Projects.RemoveRange(ProjectA, ProjectB);
            await ctx.SaveChangesAsync();
        }
    }
}
