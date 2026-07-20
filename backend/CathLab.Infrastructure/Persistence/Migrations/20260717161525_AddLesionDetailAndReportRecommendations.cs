using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLesionDetailAndReportRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedByUserId",
                table: "ProcedureReports",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "ProcedureReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Calcified",
                table: "CoronaryLesions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CoronaryLesions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "CoronaryLesions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefDiam",
                table: "CoronaryLesions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidualStenosis",
                table: "CoronaryLesions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Thrombus",
                table: "CoronaryLesions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "ProcedureReports");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "ProcedureReports");

            migrationBuilder.DropColumn(
                name: "Calcified",
                table: "CoronaryLesions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CoronaryLesions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "CoronaryLesions");

            migrationBuilder.DropColumn(
                name: "RefDiam",
                table: "CoronaryLesions");

            migrationBuilder.DropColumn(
                name: "ResidualStenosis",
                table: "CoronaryLesions");

            migrationBuilder.DropColumn(
                name: "Thrombus",
                table: "CoronaryLesions");
        }
    }
}
