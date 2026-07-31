using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <summary>
    /// Introduces the <c>rls_bypass</c> database role and makes the RLS predicates exempt it.
    ///
    /// The previous exemption, <c>IS_MEMBER('db_owner')</c>, is implicit and blanket: it silently
    /// covers sa, the deploy principal, and anyone who gains admin. Nobody grants it, so nobody can
    /// revoke or audit it. A purpose-built role with no inherent power makes the exemption an
    /// explicit act instead.
    ///
    /// This migration exempts BOTH roles on purpose. The nightly .bacpac export depends on being
    /// exempt — SqlPackage issues its own SELECTs, with nowhere to set SESSION_CONTEXT — so
    /// removing db_owner in the same step would risk a window where the backup silently exports
    /// empty tables. A follow-up migration drops the db_owner clause, once the backup principal has
    /// been added to rls_bypass and one nightly run has been verified.
    ///
    /// See docs/rls-security-model.md.
    /// </summary>
    /// <inheritdoc />
    public partial class AddRlsBypassRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A role holding no permissions of its own. Its only purpose is to satisfy the
            // predicates below, which makes RLS exemption something a principal is granted rather
            // than something it happens to be.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'rls_bypass' AND type = 'R')
                    CREATE ROLE rls_bypass;
                """);

            // Policies must be dropped before the functions they reference: the functions are
            // SCHEMABINDING, so each policy holds a hard dependency on them.
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectUsersUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.TimeEntriesUserPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_by_user_id;");

            // IS_MEMBER rather than IS_ROLEMEMBER: both work for a database role, and IS_MEMBER is
            // already proven to work inside these SCHEMABINDING predicates by the migration this
            // one replaces.
            migrationBuilder.Sql("""
                CREATE FUNCTION app.fn_filter_by_user_id(@UserId nvarchar(450))
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS fn_result
                WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                   OR IS_MEMBER('rls_bypass') = 1
                   OR IS_MEMBER('db_owner') = 1;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION app.fn_filter_projects_by_user(@ProjectId int)
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS fn_result
                WHERE EXISTS (
                    SELECT 1 FROM app.ProjectUsers
                    WHERE ProjectId = @ProjectId
                    AND UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                )
                OR IS_MEMBER('rls_bypass') = 1
                OR IS_MEMBER('db_owner') = 1;
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.TimeEntriesUserPolicy
                ADD FILTER PREDICATE app.fn_filter_by_user_id(UserId) ON app.TimeEntries
                WITH (STATE = ON);
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.ProjectUsersUserPolicy
                ADD FILTER PREDICATE app.fn_filter_by_user_id(UserId) ON app.ProjectUsers
                WITH (STATE = ON);
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.ProjectsUserPolicy
                ADD FILTER PREDICATE app.fn_filter_projects_by_user(Id) ON app.Projects
                WITH (STATE = ON);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the db_owner-only predicates from ExemptDbOwnerFromRls.
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectUsersUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.TimeEntriesUserPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_by_user_id;");

            migrationBuilder.Sql("""
                CREATE FUNCTION app.fn_filter_by_user_id(@UserId nvarchar(450))
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS fn_result
                WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                   OR IS_MEMBER('db_owner') = 1;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION app.fn_filter_projects_by_user(@ProjectId int)
                RETURNS TABLE
                WITH SCHEMABINDING
                AS
                RETURN SELECT 1 AS fn_result
                WHERE EXISTS (
                    SELECT 1 FROM app.ProjectUsers
                    WHERE ProjectId = @ProjectId
                    AND UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450))
                )
                OR IS_MEMBER('db_owner') = 1;
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.TimeEntriesUserPolicy
                ADD FILTER PREDICATE app.fn_filter_by_user_id(UserId) ON app.TimeEntries
                WITH (STATE = ON);
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.ProjectUsersUserPolicy
                ADD FILTER PREDICATE app.fn_filter_by_user_id(UserId) ON app.ProjectUsers
                WITH (STATE = ON);
                """);

            migrationBuilder.Sql("""
                CREATE SECURITY POLICY app.ProjectsUserPolicy
                ADD FILTER PREDICATE app.fn_filter_projects_by_user(Id) ON app.Projects
                WITH (STATE = ON);
                """);

            // The role is deliberately left in place. Dropping it would fail while any principal is
            // still a member, and an unused empty role is harmless.
        }
    }
}
