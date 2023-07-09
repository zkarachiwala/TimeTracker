using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.API.Migrations
{
    /// <inheritdoc />
    public partial class AppUserIdToTimeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeEntries_AppUsers_AppUserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_AppUserId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.AlterColumn<string>(
                name: "AppUserId",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AppUserId",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_AppUserId",
                schema: "app",
                table: "TimeEntries",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeEntries_AppUsers_AppUserId",
                schema: "app",
                table: "TimeEntries",
                column: "AppUserId",
                principalSchema: "app",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }
    }
}
