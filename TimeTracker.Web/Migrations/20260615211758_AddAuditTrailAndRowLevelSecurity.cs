using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrailAndRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "app",
                table: "ProjectUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "app",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "app",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "app",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "app",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "app",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "app",
                table: "Clients",
                type: "nvarchar(max)",
                nullable: true);

            // Row-Level Security
            // Requires the app DB user to hold:
            //   GRANT CREATE FUNCTION TO [<app-user>];
            //   GRANT ALTER ANY SECURITY POLICY TO [<app-user>];
            // (db_owner / sysadmin are exempt from RLS — local dev with 'sa' bypasses these policies by design.)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectUsersUserPolicy;");
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.TimeEntriesUserPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_by_user_id;");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "app",
                table: "ProjectUsers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "app",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "app",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "app",
                table: "Clients");
        }
    }
}
