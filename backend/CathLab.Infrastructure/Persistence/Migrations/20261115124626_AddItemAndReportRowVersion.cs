using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemAndReportRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProcedureReports",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InventoryItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProcedureReports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InventoryItems");
        }
    }
}
