using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class ExemptDbOwnerFromRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop policies before dropping the functions they reference
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectUsersUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.TimeEntriesUserPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_by_user_id;");

            // Recreate with db_owner exemption so SA can query app tables without setting session context
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS nvarchar(450));
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
                );
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
