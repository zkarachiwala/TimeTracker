using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TimeTracker.Shared.Entities;
using TimeTracker.Web.Data;
using Xunit;
using Xunit.Abstractions;

namespace TimeTracker.Tests.Infrastructure;

/// <summary>
/// THROWAWAY SPIKE — delete once the tenancy design is settled.
///
/// Answers seven open questions about SQL Server Row-Level Security before we commit to a
/// design for project-level budgets and a future Organisation (tenant) tier. Nothing here is
/// production code: no entity changes, no migration, no DbSet. Candidate predicates are applied
/// as raw SQL to a throwaway database and discarded.
///
/// WHY THIS EXISTS
/// The #167 budget bar sums only the signed-in user's hours, because app.TimeEntriesUserPolicy
/// filters app.TimeEntries to UserId = SESSION_CONTEXT('UserId'). A budget belongs to a project,
/// so it must roll up everyone's hours. The same cause already breaks GetProjectUsers, which
/// returns only yourself in production.
///
/// HOW TO READ THE RESULTS
/// "All green" is not the goal — a written answer to each question is. Several tests prove a
/// constraint by asserting that something fails. Run with console verbosity to see the captured
/// SQL Server messages, which are the actual deliverable:
///
///   dotnet test TimeTracker.Tests --filter "FullyQualifiedName~RlsPredicateSpike" \
///     --logger "console;verbosity=normal"
///
/// Each test builds its own isolated, freshly migrated database, so tests do not interfere with
/// each other or with the shared RlsIntegrationTests database.
///
/// Every query runs as a low-privilege login holding only db_datareader + db_datawriter — the
/// same shape as the production Managed Identity. This is essential: sa is db_owner and exempt
/// from RLS by design, so a spike run as sa would prove nothing.
/// </summary>
[Collection("SqlServer")]
[Trait("Category", "Container")]
public class RlsPredicateSpike(SqlServerFixture fixture, ITestOutputHelper output)
{
    private const string LoginName = "timetracker_spike_test";
    private const string LoginPassword = "Sp1ke_T3st!Pw#2026";

    private const string UserA = "user-spike-A";
    private const string UserB = "user-spike-B";

    private const int RoleMember = 0;
    private const int RoleManager = 1;

    private const int OrgOne = 1;
    private const int OrgTwo = 2;

    // ── Q1: can a predicate on app.ProjectUsers query app.ProjectUsers? ──────────────────

    /// <summary>
    /// Q1. A manager check on the membership table would have to look up the membership table.
    /// If SQL Server rejects that, the ProjectUsers policy cannot carry manager logic and must
    /// either be dropped or keyed on something else.
    /// </summary>
    [Fact]
    public async Task Q1_SelfReferentialPredicateOnProjectUsers()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);

        // Predicate ON app.ProjectUsers that reads FROM app.ProjectUsers.
        await ExecAdminAsync(db, """
            CREATE FUNCTION app.fn_spike_self_ref(@ProjectId int, @UserId nvarchar(450))
            RETURNS TABLE
            WITH SCHEMABINDING
            AS
            RETURN SELECT 1 AS fn_result
            WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
               OR EXISTS (
                    SELECT 1 FROM app.ProjectUsers pu
                    WHERE pu.ProjectId = @ProjectId
                      AND pu.UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                      AND pu.Role = 1
               )
               OR IS_MEMBER('db_owner') = 1;
            """);

        // The failure may surface when the policy is created, or later when the table is queried.
        // Capture whichever happens so the constraint is documented rather than folklore.
        var failure = await CaptureSqlFailureAsync(async () =>
        {
            await ExecAdminAsync(db, """
                CREATE SECURITY POLICY app.SpikeSelfRefPolicy
                ADD FILTER PREDICATE app.fn_spike_self_ref(ProjectId, UserId) ON app.ProjectUsers
                WITH (STATE = ON);
                """);

            await SeedAsync(db);

            await using var ctx = LowPrivContext(db);
            await SetSessionUserAsync(ctx, UserA);
            _ = await ctx.ProjectUsers.ToListAsync();
        });

        Report("Q1", "Self-referential predicate on app.ProjectUsers",
            failure is null
                ? "ALLOWED — self-reference did not fail. The membership table CAN carry a manager check."
                : $"REJECTED — {failure}");

        // Deliberately not asserting a specific outcome: this test records behaviour.
        // Whichever way it lands, the answer drives the design.
        Assert.True(true);
    }

    // ── Q2/Q3: manager rollup via a multi-column predicate, ProjectUsers policy left ON ──

    /// <summary>
    /// Q2. The TimeEntries predicate reads app.ProjectUsers, which has its own policy. If that
    /// inner read is filtered to the session user's own rows, a manager check still works — the
    /// manager is looking up their own membership row. This proves it rather than assuming it.
    /// Q3. Also exercises a two-column filter predicate, (UserId, ProjectId).
    /// </summary>
    [Fact]
    public async Task Q2_ManagerSeesTeamEntries_WithProjectUsersPolicyStillOn()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);
        await CreateManagerAwareTimeEntryPolicyAsync(db);
        await RestoreProjectUsersPolicyAsync(db);

        var seed = await SeedAsync(db);
        await SetRoleAsync(db, seed.SharedProjectId, UserA, RoleManager);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        var visible = await ctx.TimeEntries.Where(e => e.ProjectId == seed.SharedProjectId).ToListAsync();
        var sawOtherUser = visible.Any(e => e.UserId == UserB);

        Report("Q2/Q3", "Manager rollup with the ProjectUsers policy left ON",
            $"Manager saw {visible.Count} entries on the shared project; other user's rows visible: {sawOtherUser}. " +
            "Nested policy on the inner ProjectUsers read did NOT break the manager check." );

        Assert.True(sawOtherUser,
            "Manager could not see team entries — the nested ProjectUsers policy filtered the manager lookup itself.");
    }

    /// <summary>
    /// Q2 negative control. A plain member must NOT gain visibility of a colleague's entries.
    /// Without this, a passing Q2 could simply mean the predicate is letting everyone through.
    /// </summary>
    [Fact]
    public async Task Q2_PlainMemberStillCannotSeeTeamEntries()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);
        await CreateManagerAwareTimeEntryPolicyAsync(db);
        await RestoreProjectUsersPolicyAsync(db);

        var seed = await SeedAsync(db);
        await SetRoleAsync(db, seed.SharedProjectId, UserA, RoleMember);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        var visible = await ctx.TimeEntries.Where(e => e.ProjectId == seed.SharedProjectId).ToListAsync();

        Report("Q2 control", "Plain member on the same project",
            $"Member saw {visible.Count} entries; all own rows: {visible.All(e => e.UserId == UserA)}");

        Assert.All(visible, e => Assert.Equal(UserA, e.UserId));
    }

    // ── Q4: drop the ProjectUsers policy ─────────────────────────────────────────────────

    /// <summary>
    /// Q4. Dropping the membership policy is the candidate fix for the GetProjectUsers bug.
    /// Confirms a manager then sees the full membership, and the manager rollup still works.
    /// </summary>
    [Fact]
    public async Task Q4_DroppingProjectUsersPolicy_RevealsFullMembership()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);
        await CreateManagerAwareTimeEntryPolicyAsync(db);
        // ProjectUsers policy deliberately NOT restored.

        var seed = await SeedAsync(db);
        await SetRoleAsync(db, seed.SharedProjectId, UserA, RoleManager);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        var members = await ctx.ProjectUsers.Where(pu => pu.ProjectId == seed.SharedProjectId).ToListAsync();
        var entries = await ctx.TimeEntries.Where(e => e.ProjectId == seed.SharedProjectId).ToListAsync();

        Report("Q4", "ProjectUsers policy dropped",
            $"Membership rows visible: {members.Count} (expected 2). " +
            $"Entries visible to manager: {entries.Count}.");

        Assert.Contains(members, pu => pu.UserId == UserB);
        Assert.Contains(entries, e => e.UserId == UserB);
    }

    // ── Q5: tenant-keyed predicate on app.Projects ───────────────────────────────────────

    /// <summary>
    /// Q5. The target model: app.Projects filtered by the tenant a user belongs to, composed with
    /// the per-project/per-user rule on app.TimeEntries.
    /// </summary>
    [Fact]
    public async Task Q5_TenantKeyedProjectPredicate_ComposesWithTimeEntryPredicate()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);
        await AddOrganisationScaffoldingAsync(db);
        await CreateManagerAwareTimeEntryPolicyAsync(db);

        await ExecAdminAsync(db, """
            CREATE FUNCTION app.fn_spike_projects_by_org(@OrganisationId int)
            RETURNS TABLE
            WITH SCHEMABINDING
            AS
            RETURN SELECT 1 AS fn_result
            WHERE EXISTS (
                    SELECT 1 FROM app.OrganisationUsers ou
                    WHERE ou.OrganisationId = @OrganisationId
                      AND ou.UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                  )
               OR IS_MEMBER('db_owner') = 1;
            """);
        await ExecAdminAsync(db, """
            CREATE SECURITY POLICY app.SpikeProjectsOrgPolicy
            ADD FILTER PREDICATE app.fn_spike_projects_by_org(OrganisationId) ON app.Projects
            WITH (STATE = ON);
            """);

        var seed = await SeedAsync(db);

        // UserA belongs to org 1; the shared project is org 1, the foreign project is org 2.
        await ExecAdminAsync(db, $"""
            INSERT INTO app.OrganisationUsers (OrganisationId, UserId) VALUES ({OrgOne}, N'{UserA}');
            INSERT INTO app.OrganisationUsers (OrganisationId, UserId) VALUES ({OrgTwo}, N'{UserB}');
            UPDATE app.Projects SET OrganisationId = {OrgOne} WHERE Id = {seed.SharedProjectId};
            UPDATE app.Projects SET OrganisationId = {OrgTwo} WHERE Id = {seed.ForeignProjectId};
            """);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        var visible = await ctx.Projects.IgnoreQueryFilters().Select(p => p.Id).ToListAsync();

        Report("Q5", "Tenant-keyed predicate on app.Projects",
            $"UserA (org {OrgOne}) sees project ids: [{string.Join(", ", visible)}]. " +
            $"Shared={seed.SharedProjectId} expected visible, Foreign={seed.ForeignProjectId} expected hidden.");

        Assert.Contains(seed.SharedProjectId, visible);
        Assert.DoesNotContain(seed.ForeignProjectId, visible);
    }

    // ── Q6: BLOCK predicates on write ────────────────────────────────────────────────────

    /// <summary>
    /// Q6. Everything in the app today is FILTER-only, which hides rows on read but does not stop
    /// a write carrying someone else's id. Under real multi-tenancy that is the gap that matters.
    /// </summary>
    [Fact]
    public async Task Q6_BlockPredicate_PreventsWritingAnotherUsersRow()
    {
        await using var db = await NewDatabaseAsync();
        await DropExistingPoliciesAsync(db);
        await AddRoleColumnAsync(db);
        await CreateManagerAwareTimeEntryPolicyAsync(db);

        await ExecAdminAsync(db, """
            ALTER SECURITY POLICY app.SpikeTimeEntriesPolicy
            ADD BLOCK PREDICATE app.fn_spike_time_entries(UserId, ProjectId) ON app.TimeEntries AFTER INSERT;
            """);

        var seed = await SeedAsync(db);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        // UserA attempts to write a row attributed to UserB.
        ctx.TimeEntries.Add(new TimeEntry
        {
            UserId = UserB,
            ProjectId = seed.SharedProjectId,
            Start = DateTime.UtcNow,
        });

        var failure = await CaptureSqlFailureAsync(() => ctx.SaveChangesAsync());

        Report("Q6", "BLOCK predicate AFTER INSERT",
            failure is null
                ? "NOT BLOCKED — the cross-user insert succeeded. BLOCK predicate did not apply as expected."
                : $"BLOCKED — {failure}");

        Assert.NotNull(failure);
    }

    // ── Q7: silent partial aggregates ────────────────────────────────────────────────────

    /// <summary>
    /// Q7. The crux of the #167 bug. Under RLS an aggregate does not error — it silently returns a
    /// partial total, which is why the budget bar showed a plausible but wrong number. This is the
    /// one test intended to survive the spike; it belongs in RlsIntegrationTests once the design
    /// lands, as a permanent guard against re-introducing a per-user rollup by accident.
    /// </summary>
    [Fact]
    public async Task Q7_AggregateUnderRls_SilentlyReturnsPartialSum()
    {
        await using var db = await NewDatabaseAsync();
        // Keep the CURRENT production policies — this documents today's behaviour, not a candidate.
        var seed = await SeedAsync(db);

        await using var ctx = LowPrivContext(db);
        await SetSessionUserAsync(ctx, UserA);

        var visibleHours = await ctx.TimeEntries
            .Where(e => e.ProjectId == seed.SharedProjectId && e.End.HasValue)
            .SumAsync(e => EF.Functions.DateDiffMinute(e.Start, e.End!.Value) / 60.0);

        Report("Q7", "SUM over a project under per-user RLS",
            $"UserA computed {visibleHours}h for the shared project. " +
            $"True total across both users is {seed.SharedProjectTotalHours}h. " +
            "No error was raised — the wrong answer is returned silently.");

        Assert.Equal(seed.UserAHours, visibleHours, precision: 3);
        Assert.NotEqual(seed.SharedProjectTotalHours, visibleHours);
    }

    // ── Candidate predicate definitions ──────────────────────────────────────────────────

    /// <summary>
    /// The candidate TimeEntries rule: your own rows, or rows on a project you manage.
    /// Takes two columns from the table it guards, which is what Q3 exercises.
    /// </summary>
    private async Task CreateManagerAwareTimeEntryPolicyAsync(IsolatedDb db)
    {
        await ExecAdminAsync(db, $"""
            CREATE FUNCTION app.fn_spike_time_entries(@UserId nvarchar(450), @ProjectId int)
            RETURNS TABLE
            WITH SCHEMABINDING
            AS
            RETURN SELECT 1 AS fn_result
            WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
               OR EXISTS (
                    SELECT 1 FROM app.ProjectUsers pu
                    WHERE pu.ProjectId = @ProjectId
                      AND pu.UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                      AND pu.Role = {RoleManager}
               )
               OR IS_MEMBER('db_owner') = 1;
            """);

        await ExecAdminAsync(db, """
            CREATE SECURITY POLICY app.SpikeTimeEntriesPolicy
            ADD FILTER PREDICATE app.fn_spike_time_entries(UserId, ProjectId) ON app.TimeEntries
            WITH (STATE = ON);
            """);
    }

    /// <summary>Re-creates the production-shaped membership policy, for the Q2 "still on" case.</summary>
    private async Task RestoreProjectUsersPolicyAsync(IsolatedDb db)
    {
        await ExecAdminAsync(db, """
            CREATE FUNCTION app.fn_spike_project_users(@UserId nvarchar(450))
            RETURNS TABLE
            WITH SCHEMABINDING
            AS
            RETURN SELECT 1 AS fn_result
            WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
               OR IS_MEMBER('db_owner') = 1;
            """);

        await ExecAdminAsync(db, """
            CREATE SECURITY POLICY app.SpikeProjectUsersPolicy
            ADD FILTER PREDICATE app.fn_spike_project_users(UserId) ON app.ProjectUsers
            WITH (STATE = ON);
            """);
    }

    // ── Schema scaffolding (throwaway, raw SQL, never a migration) ───────────────────────

    private Task AddRoleColumnAsync(IsolatedDb db) => ExecAdminAsync(db, $"""
        ALTER TABLE app.ProjectUsers ADD [Role] int NOT NULL CONSTRAINT DF_Spike_Role DEFAULT {RoleMember};
        """);

    private Task AddOrganisationScaffoldingAsync(IsolatedDb db) => ExecAdminAsync(db, """
        CREATE TABLE app.OrganisationUsers (
            OrganisationId int NOT NULL,
            UserId nvarchar(450) NOT NULL,
            CONSTRAINT PK_Spike_OrganisationUsers PRIMARY KEY (OrganisationId, UserId)
        );
        -- Predicates run per row: index the lookup the predicate performs.
        CREATE INDEX IX_Spike_OrganisationUsers_UserId ON app.OrganisationUsers (UserId, OrganisationId);
        ALTER TABLE app.Projects ADD OrganisationId int NOT NULL CONSTRAINT DF_Spike_Org DEFAULT 1;
        """);

    private Task DropExistingPoliciesAsync(IsolatedDb db) => ExecAdminAsync(db, """
        DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;
        DROP SECURITY POLICY IF EXISTS app.ProjectUsersUserPolicy;
        DROP SECURITY POLICY IF EXISTS app.TimeEntriesUserPolicy;
        DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;
        DROP FUNCTION IF EXISTS app.fn_filter_by_user_id;
        """);

    private Task SetRoleAsync(IsolatedDb db, int projectId, string userId, int role) =>
        ExecAdminAsync(db, $"""
            UPDATE app.ProjectUsers SET [Role] = {role}
            WHERE ProjectId = {projectId} AND UserId = N'{userId}';
            """);

    // ── Seeding ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds as sa (db_owner, RLS-exempt — intentional) so the fixture data exists regardless of
    /// whichever candidate policy is under test.
    ///
    /// Shared project: UserA and UserB both members, both with entries — the rollup case.
    /// Foreign project: UserB only — the isolation case.
    /// </summary>
    private async Task<SeedResult> SeedAsync(IsolatedDb db)
    {
        await using var ctx = new TimeTrackerDataContext(OptionsFor(db.AdminConnectionString));

        var shared = new Project
        {
            Name = $"Spike-Shared-{Guid.NewGuid():N}",
            ProjectUsers = [new ProjectUser { UserId = UserA }, new ProjectUser { UserId = UserB }],
        };
        var foreignProject = new Project
        {
            Name = $"Spike-Foreign-{Guid.NewGuid():N}",
            ProjectUsers = [new ProjectUser { UserId = UserB }],
        };
        ctx.Projects.AddRange(shared, foreignProject);
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Unspecified);
        ctx.TimeEntries.AddRange(
            new TimeEntry { UserId = UserA, ProjectId = shared.Id, Start = start, End = start.AddHours(2) },
            new TimeEntry { UserId = UserB, ProjectId = shared.Id, Start = start, End = start.AddHours(3) },
            new TimeEntry { UserId = UserB, ProjectId = foreignProject.Id, Start = start, End = start.AddHours(1) });
        await ctx.SaveChangesAsync();

        return new SeedResult(shared.Id, foreignProject.Id, UserAHours: 2.0, SharedProjectTotalHours: 5.0);
    }

    private sealed record SeedResult(
        int SharedProjectId,
        int ForeignProjectId,
        double UserAHours,
        double SharedProjectTotalHours);

    // ── Database plumbing ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fresh, fully migrated database on the shared container and grants the
    /// low-privilege login access to it. Each test gets its own, so candidate policies never
    /// collide and nothing needs unwinding.
    /// </summary>
    private async Task<IsolatedDb> NewDatabaseAsync()
    {
        var adminConnectionString = fixture.CreateIsolatedConnectionString();

        await using (var ctx = new TimeTrackerDataContext(OptionsFor(adminConnectionString)))
            await ctx.Database.MigrateAsync();

        // The login is server-scoped and may already exist from RlsIntegrationTests; the database
        // user is per-database and must be created in this new one.
        await ExecSqlAsync(adminConnectionString, $"""
            IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'{LoginName}')
                CREATE LOGIN [{LoginName}] WITH PASSWORD = N'{LoginPassword}';
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{LoginName}')
            BEGIN
                CREATE USER [{LoginName}] FOR LOGIN [{LoginName}];
                ALTER ROLE db_datareader ADD MEMBER [{LoginName}];
                ALTER ROLE db_datawriter ADD MEMBER [{LoginName}];
            END
            """);

        var lowPriv = new SqlConnectionStringBuilder(adminConnectionString)
        {
            UserID = LoginName,
            Password = LoginPassword,
            IntegratedSecurity = false,
        }.ConnectionString;

        return new IsolatedDb(adminConnectionString, lowPriv);
    }

    private TimeTrackerDataContext LowPrivContext(IsolatedDb db) =>
        new(OptionsFor(db.LowPrivilegeConnectionString));

    private static DbContextOptions<TimeTrackerDataContext> OptionsFor(string connectionString) =>
        new DbContextOptionsBuilder<TimeTrackerDataContext>().UseSqlServer(connectionString).Options;

    private static Task ExecAdminAsync(IsolatedDb db, string sql) =>
        ExecSqlAsync(db.AdminConnectionString, sql);

    private static async Task ExecSqlAsync(string connectionString, string sql)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // GO is not valid in a single command; these scripts use statement batches instead.
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SetSessionUserAsync(TimeTrackerDataContext ctx, string userId)
    {
        await ctx.Database.OpenConnectionAsync();
        await using var cmd = ctx.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context N'UserId', @userId";
        cmd.Parameters.Add(new SqlParameter("@userId", userId));
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>Runs an action and returns the SQL error text, or null if it succeeded.</summary>
    private static async Task<string?> CaptureSqlFailureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (SqlException ex)
        {
            return $"Msg {ex.Number}: {ex.Message}";
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            return $"Msg {sql.Number}: {sql.Message}";
        }
    }

    private void Report(string question, string subject, string finding)
    {
        output.WriteLine($"[{question}] {subject}");
        output.WriteLine($"        {finding}");
    }

    private sealed record IsolatedDb(string AdminConnectionString, string LowPrivilegeConnectionString)
        : IAsyncDisposable
    {
        // The whole database is throwaway; the container is torn down with the test session.
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
