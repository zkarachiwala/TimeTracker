using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <summary>
    /// Drops <c>app.ProjectsUserPolicy</c>. ADR-024 defines <c>ProjectUser</c> as a time-allocation
    /// gate only — all authenticated users can see all projects and clients. RLS enforced the
    /// opposite (membership-only visibility), and the app tier (<c>ProjectService.GetAllProjects</c>,
    /// <c>GetDeletedProjects</c>) has no user scoping to compensate, so admins were silently missing
    /// projects they were not assigned to on <c>/projects</c> and <c>/trash</c>. See #337.
    ///
    /// <c>app.Projects</c> is left with no RLS after this migration — that is the behaviour ADR-024
    /// specifies. <c>app.ProjectUsers</c> and <c>app.TimeEntries</c> keep their policies; those are
    /// genuine per-user isolation boundaries, not a visibility permission.
    /// </summary>
    /// <inheritdoc />
    public partial class DropProjectsUserRlsPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Policy before function — the function is SCHEMABINDING.
            migrationBuilder.Sql("DROP SECURITY POLICY IF EXISTS app.ProjectsUserPolicy;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS app.fn_filter_projects_by_user;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                CREATE SECURITY POLICY app.ProjectsUserPolicy
                ADD FILTER PREDICATE app.fn_filter_projects_by_user(Id) ON app.Projects
                WITH (STATE = ON);
                """);
        }
    }
}
