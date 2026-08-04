using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureReportConfirmedByUserNav : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProcedureReports_ConfirmedByUserId",
                table: "ProcedureReports",
                column: "ConfirmedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcedureReports_AppUsers_ConfirmedByUserId",
                table: "ProcedureReports",
                column: "ConfirmedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcedureReports_AppUsers_ConfirmedByUserId",
                table: "ProcedureReports");

            migrationBuilder.DropIndex(
                name: "IX_ProcedureReports_ConfirmedByUserId",
                table: "ProcedureReports");
        }
    }
}
