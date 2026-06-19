using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAwardRateToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AwardRate",
                schema: "app",
                table: "Clients",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardRate",
                schema: "app",
                table: "Clients");
        }
    }
}
