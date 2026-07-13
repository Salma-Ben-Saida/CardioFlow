using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLesionTreatmentAndPharmacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Manufacturer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MinimumStockLevel = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcedureReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CathStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Approach = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CatheterUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContrastVolumeMl = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DoseAreaProductGycm2 = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    DominanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimiFlowBaseline = table.Column<byte>(type: "tinyint", nullable: true),
                    TimiFlowFinal = table.Column<byte>(type: "tinyint", nullable: true),
                    Conclusion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PdfStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcedureReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcedureReports_CathStudies_CathStudyId",
                        column: x => x.CathStudyId,
                        principalTable: "CathStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservedForCathStudyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_CathStudies_ReservedForCathStudyId",
                        column: x => x.ReservedForCathStudyId,
                        principalTable: "CathStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventoryItems_InventoryProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "InventoryProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReferenceNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MovedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_AppUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "InventoryProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoronaryLesions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcedureReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Vessel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SegmentNumber = table.Column<byte>(type: "tinyint", nullable: false),
                    StenosisPercent = table.Column<byte>(type: "tinyint", nullable: false),
                    LesionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TimiFlow = table.Column<byte>(type: "tinyint", nullable: true),
                    IsTreated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoronaryLesions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoronaryLesions_ProcedureReports_ProcedureReportId",
                        column: x => x.ProcedureReportId,
                        principalTable: "ProcedureReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LesionTreatments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LesionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimiFlowAfter = table.Column<byte>(type: "tinyint", nullable: true),
                    PerformedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LesionTreatments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LesionTreatments_CoronaryLesions_LesionId",
                        column: x => x.LesionId,
                        principalTable: "CoronaryLesions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LesionTreatments_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BalloonDilations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BalloonDiameterMm = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    BalloonLengthMm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxPressureAtm = table.Column<byte>(type: "tinyint", nullable: false),
                    InflationDurationSec = table.Column<int>(type: "int", nullable: false),
                    IsPreDilation = table.Column<bool>(type: "bit", nullable: false),
                    IsPostDilation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalloonDilations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalloonDilations_LesionTreatments_Id",
                        column: x => x.Id,
                        principalTable: "LesionTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StentImplants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LandingZoneProximal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LandingZoneDistal = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StentDiameterMm = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    StentLengthMm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PostDilationPerformed = table.Column<bool>(type: "bit", nullable: false),
                    PostDilationPressure = table.Column<byte>(type: "tinyint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StentImplants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StentImplants_LesionTreatments_Id",
                        column: x => x.Id,
                        principalTable: "LesionTreatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoronaryLesions_ProcedureReportId",
                table: "CoronaryLesions",
                column: "ProcedureReportId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Barcode",
                table: "InventoryItems",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ProductId",
                table: "InventoryItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ReservedForCathStudyId",
                table: "InventoryItems",
                column: "ReservedForCathStudyId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryProducts_Reference",
                table: "InventoryProducts",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LesionTreatments_InventoryItemId",
                table: "LesionTreatments",
                column: "InventoryItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LesionTreatments_LesionId",
                table: "LesionTreatments",
                column: "LesionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcedureReports_CathStudyId",
                table: "ProcedureReports",
                column: "CathStudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedBy",
                table: "StockMovements",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalloonDilations");

            migrationBuilder.DropTable(
                name: "StentImplants");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "LesionTreatments");

            migrationBuilder.DropTable(
                name: "CoronaryLesions");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "ProcedureReports");

            migrationBuilder.DropTable(
                name: "InventoryProducts");
        }
    }
}
