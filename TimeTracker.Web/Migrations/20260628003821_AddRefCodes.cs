using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRefCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "seq_ClientRef",
                schema: "app");

            migrationBuilder.CreateSequence<int>(
                name: "seq_ProjectRef",
                schema: "app");

            migrationBuilder.AddColumn<int>(
                name: "ProjectSeqId",
                schema: "app",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR app.seq_ProjectRef");

            migrationBuilder.AddColumn<int>(
                name: "ClientSeqId",
                schema: "app",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValueSql: "NEXT VALUE FOR app.seq_ClientRef");

            migrationBuilder.AddColumn<string>(
                name: "RefCode",
                schema: "app",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                computedColumnSql: "'PROJ-' + RIGHT('000' + CAST(ProjectSeqId AS VARCHAR(10)), 3)",
                stored: true);

            migrationBuilder.AddColumn<string>(
                name: "RefCode",
                schema: "app",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                computedColumnSql: "'CLI-' + RIGHT('000' + CAST(ClientSeqId AS VARCHAR(10)), 3)",
                stored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefCode",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RefCode",
                schema: "app",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ProjectSeqId",
                schema: "app",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ClientSeqId",
                schema: "app",
                table: "Clients");

            migrationBuilder.DropSequence(
                name: "seq_ClientRef",
                schema: "app");

            migrationBuilder.DropSequence(
                name: "seq_ProjectRef",
                schema: "app");
        }
    }
}
