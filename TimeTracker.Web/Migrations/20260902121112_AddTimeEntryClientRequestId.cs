using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <summary>
    /// Stage B offline-resilience: <c>ClientRequestId</c> is a client-generated idempotency tag for
    /// entries created via the offline sync queue, so a retried create (e.g. after a dropped
    /// response) returns the existing row instead of inserting a duplicate. Every entry created the
    /// normal online way — including everything that already exists — has it null.
    ///
    /// The index is filtered (<c>WHERE [ClientRequestId] IS NOT NULL</c>) rather than a plain unique
    /// index: SQL Server's plain unique index only tolerates a single NULL, and almost every row will
    /// have one. A plain unique index would have broken the very next normal entry created online.
    /// </summary>
    /// <inheritdoc />
    public partial class AddTimeEntryClientRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientRequestId",
                schema: "app",
                table: "TimeEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_ClientRequestId",
                schema: "app",
                table: "TimeEntries",
                column: "ClientRequestId",
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_ClientRequestId",
                schema: "app",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                schema: "app",
                table: "TimeEntries");
        }
    }
}
