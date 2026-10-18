using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIngestionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserActivityLogs_AppUsers_UserId",
                table: "UserActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ConquestPatientID",
                table: "Patients",
                column: "ConquestPatientID",
                unique: true,
                filter: "[ConquestPatientID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies",
                column: "AccessionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_ConquestPatientID",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserActivityLogs_AppUsers_UserId",
                table: "UserActivityLogs",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "Id");
        }
    }
}
