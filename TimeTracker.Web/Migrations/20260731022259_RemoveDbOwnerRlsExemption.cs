using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <summary>
    /// Removes the blanket <c>IS_MEMBER('db_owner')</c> exemption, leaving <c>rls_bypass</c> as the
    /// only way past the RLS predicates.
    ///
    /// APPLY THIS ONLY AFTER the backup principal has been added to rls_bypass and one nightly
    /// .bacpac export has been verified as a plausible size. Applying it first would leave the
    /// backup with no exemption, and SqlPackage would export empty tables without erroring.
    ///
    /// After this migration, sa is filtered like any other principal. That is intentional: local
    /// development now enforces RLS exactly as production does, so RLS defects stop being
    /// production-only. To inspect data locally, join the role deliberately:
    ///
    ///     ALTER ROLE rls_bypass ADD MEMBER [sa];
    ///
    /// See docs/rls-security-model.md.
    /// </summary>
    /// <inheritdoc />
    public partial class RemoveDbOwnerRlsExemption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Policies before functions — the functions are SCHEMABINDING.
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
                   OR IS_MEMBER('rls_bypass') = 1;
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
                OR IS_MEMBER('rls_bypass') = 1;
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
            // Restores the dual exemption from AddRlsBypassRole.
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
    }
}
