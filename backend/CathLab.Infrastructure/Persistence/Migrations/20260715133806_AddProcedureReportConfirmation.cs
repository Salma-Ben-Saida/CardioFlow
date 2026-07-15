using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureReportConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Specialty",
                table: "AppUsers");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "ProcedureReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "ProcedureReports",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "ProcedureReports");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "ProcedureReports");

            migrationBuilder.AddColumn<string>(
                name: "Specialty",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
