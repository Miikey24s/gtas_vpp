using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Lean06SettlementSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VPP04_Settlement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    M = table.Column<int>(type: "int", nullable: false),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    IsCurrentRevision = table.Column<bool>(type: "bit", nullable: false),
                    IsCorrection = table.Column<bool>(type: "bit", nullable: false),
                    SupersedesSettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupersededBySettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimarySupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimarySupplierName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceListName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PriceListVersion = table.Column<int>(type: "int", nullable: false),
                    PriceAsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalculationVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CommandPayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    RebateAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    RoundingAdjustment = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfirmedByUserId = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP04_Settlement", x => x.Id);
                    table.CheckConstraint("CK_VPP04_Settlement_Amounts", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0 AND [VatAmount] >= 0 AND [GrandTotal] >= 0");
                    table.CheckConstraint("CK_VPP04_Settlement_Period", "[Y] BETWEEN 1 AND 9999 AND [M] BETWEEN 1 AND 12 AND [RevisionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_VPP04_Settlement_VPP00_Period_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "VPP00_Period",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VPP04_Settlement_VPP04_Settlement_SupersedesSettlementId",
                        column: x => x.SupersedesSettlementId,
                        principalTable: "VPP04_Settlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VPP05_SettlementItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VppCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VppName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UomCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UomName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceBookItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierSku = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    MinimumOrderQuantity = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    LeadTimeDays = table.Column<int>(type: "int", nullable: false),
                    IsSupplierException = table.Column<bool>(type: "bit", nullable: false),
                    SupplierExceptionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP05_SettlementItem", x => x.Id);
                    table.CheckConstraint("CK_VPP05_SettlementItem_Amounts", "[Quantity] > 0 AND [NetUnitPrice] >= 0 AND [VatRate] >= 0 AND [VatRate] <= 100 AND [NetAmount] >= 0 AND [VatAmount] >= 0 AND [GrossAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_VPP05_SettlementItem_VPP04_Settlement_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "VPP04_Settlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VPP06_SettlementCharge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    AllocationBasis = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP06_SettlementCharge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VPP06_SettlementCharge_VPP04_Settlement_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "VPP04_Settlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VPP07_SettlementAllocation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettlementItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequesterUserId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    CommercialAdjustmentAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    RoundingAdjustment = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP07_SettlementAllocation", x => x.Id);
                    table.CheckConstraint("CK_VPP07_SettlementAllocation_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_VPP07_SettlementAllocation_VPP04_Settlement_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "VPP04_Settlement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VPP07_SettlementAllocation_VPP05_SettlementItem_SettlementItemId",
                        column: x => x.SettlementItemId,
                        principalTable: "VPP05_SettlementItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VPP04_Settlement_PeriodId",
                table: "VPP04_Settlement",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_VPP04_Settlement_SupersedesSettlementId",
                table: "VPP04_Settlement",
                column: "SupersedesSettlementId");

            migrationBuilder.CreateIndex(
                name: "UX_VPP04_Settlement_Current",
                table: "VPP04_Settlement",
                columns: new[] { "MemberCompanyCode", "Y", "M" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_VPP04_Settlement_Idempotency",
                table: "VPP04_Settlement",
                columns: new[] { "MemberCompanyCode", "IdempotencyKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_VPP04_Settlement_Revision",
                table: "VPP04_Settlement",
                columns: new[] { "MemberCompanyCode", "Y", "M", "RevisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VPP05_SettlementItem_SettlementId_VppId",
                table: "VPP05_SettlementItem",
                columns: new[] { "SettlementId", "VppId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VPP06_SettlementCharge_SettlementId_ChargeType",
                table: "VPP06_SettlementCharge",
                columns: new[] { "SettlementId", "ChargeType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VPP07_SettlementAllocation_SettlementId_DepartmentCode",
                table: "VPP07_SettlementAllocation",
                columns: new[] { "SettlementId", "DepartmentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_VPP07_SettlementAllocation_SettlementId_RequestDetailId",
                table: "VPP07_SettlementAllocation",
                columns: new[] { "SettlementId", "RequestDetailId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VPP07_SettlementAllocation_SettlementItemId",
                table: "VPP07_SettlementAllocation",
                column: "SettlementItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Settlement snapshots are audit evidence.  Rollback is intentionally
            // forward-only; dropping them would destroy the close history.
        }
    }
}
