using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class MergeProjectDetailsIntoProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add columns to Projects first
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "app",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "app",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                schema: "app",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            // 2. Copy existing data from ProjectDetails into Projects
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.Description = pd.Description,
                    p.StartDate   = pd.StartDate,
                    p.EndDate     = pd.EndDate
                FROM app.Projects p
                INNER JOIN app.ProjectDetails pd ON pd.ProjectId = p.Id
            ");

            // 3. Now safe to drop the ProjectDetails table
            migrationBuilder.DropTable(
                name: "ProjectDetails",
                schema: "app");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "app",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "ProjectDetails",
                schema: "app",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDetails_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "app",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDetails_ProjectId",
                schema: "app",
                table: "ProjectDetails",
                column: "ProjectId",
                unique: true);
        }
    }
}
