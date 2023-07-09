using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserId",
                schema: "app");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppUsers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserProject",
                schema: "app",
                columns: table => new
                {
                    AppUsersId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserProject", x => new { x.AppUsersId, x.ProjectsId });
                    table.ForeignKey(
                        name: "FK_AppUserProject_AppUsers_AppUsersId",
                        column: x => x.AppUsersId,
                        principalSchema: "app",
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppUserProject_Projects_ProjectsId",
                        column: x => x.ProjectsId,
                        principalSchema: "app",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_AppUserId",
                schema: "app",
                table: "TimeEntries",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserProject_ProjectsId",
                schema: "app",
                table: "AppUserProject",
                column: "ProjectsId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_AppUsers_AppUserId",
                schema: "app",
                table: "TimeEntries",
                column: "AppUserId",
                principalSchema: "app",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_AppUsers_AppUserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropTable(
                name: "AppUserProject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "AppUsers",
                schema: "app");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_AppUserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserId",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserId", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserId_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "app",
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserId_ProjectId",
                schema: "app",
                table: "UserId",
                column: "ProjectId");
        }
    }
}
