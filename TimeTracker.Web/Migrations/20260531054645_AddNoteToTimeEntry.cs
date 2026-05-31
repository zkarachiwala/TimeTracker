using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                schema: "app",
                table: "TimeEntries");
        }
    }
}
