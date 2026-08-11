using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListImportBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceListImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FileFormat = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    AddedRows = table.Column<int>(type: "int", nullable: false),
                    UpdatedRows = table.Column<int>(type: "int", nullable: false),
                    UnchangedRows = table.Column<int>(type: "int", nullable: false),
                    WarningRows = table.Column<int>(type: "int", nullable: false),
                    ErrorRows = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsedCustomMapping = table.Column<bool>(type: "bit", nullable: false),
                    ColumnMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedRowsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListImportBatches", x => x.Id);
                    table.CheckConstraint("CK_PriceListImportBatches_CountsNonNegative", "[TotalRows] >= 0 AND [AddedRows] >= 0 AND [UpdatedRows] >= 0 AND [UnchangedRows] >= 0 AND [WarningRows] >= 0 AND [ErrorRows] >= 0");
                    table.ForeignKey(
                        name: "FK_PriceListImportBatches_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListImportBatches_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListImportBatches_HashStatus",
                table: "PriceListImportBatches",
                columns: new[] { "PriceListId", "FileHash", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListImportBatches_PriceListCreated",
                table: "PriceListImportBatches",
                columns: new[] { "PriceListId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListImportBatches_SupplierId",
                table: "PriceListImportBatches",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceListImportBatches");
        }
    }
}
