using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseStudyInstanceUidAsUniqueKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies");

            migrationBuilder.AlterColumn<string>(
                name: "StudyInstanceUid",
                table: "CathStudies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies",
                column: "AccessionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CathStudies_StudyInstanceUid",
                table: "CathStudies",
                column: "StudyInstanceUid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies");

            migrationBuilder.DropIndex(
                name: "IX_CathStudies_StudyInstanceUid",
                table: "CathStudies");

            migrationBuilder.AlterColumn<string>(
                name: "StudyInstanceUid",
                table: "CathStudies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_CathStudies_AccessionNumber",
                table: "CathStudies",
                column: "AccessionNumber",
                unique: true);
        }
    }
}
