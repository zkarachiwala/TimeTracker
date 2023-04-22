using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class Projects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Project",
                table: "TimeEntries");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "TimeEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_ProjectId",
                table: "TimeEntries",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_Projects_ProjectId",
                table: "TimeEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_Projects_ProjectId",
                table: "TimeEntries");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_ProjectId",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "TimeEntries");

            migrationBuilder.AddColumn<string>(
                name: "Project",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
