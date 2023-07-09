using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserProject",
                schema: "app");

            migrationBuilder.DropTable(
                name: "AppUsers",
                schema: "app");

            migrationBuilder.RenameColumn(
                name: "AppUserId",
                schema: "app",
                table: "TimeEntries",
                newName: "UserId");

            migrationBuilder.CreateTable(
                name: "ProjectUsers",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectUsers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "app",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUsers_ProjectId",
                schema: "app",
                table: "ProjectUsers",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectUsers",
                schema: "app");

            migrationBuilder.RenameColumn(
                name: "UserId",
                schema: "app",
                table: "TimeEntries",
                newName: "AppUserId");

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
                name: "IX_AppUserProject_ProjectsId",
                schema: "app",
                table: "AppUserProject",
                column: "ProjectsId");
        }
    }
}
