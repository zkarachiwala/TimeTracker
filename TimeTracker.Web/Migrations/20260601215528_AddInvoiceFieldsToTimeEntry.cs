using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceFieldsToTimeEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceReference",
                schema: "app",
                table: "TimeEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoicedAt",
                schema: "app",
                table: "TimeEntries",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceReference",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "InvoicedAt",
                schema: "app",
                table: "TimeEntries");
        }
    }
}
