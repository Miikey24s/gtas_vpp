using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Lean06PriceEffectivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [L06_VPPSupplierMapping] WHERE [Price] < 0)
                    THROW 51011, 'PRICE-001 preflight failed: legacy price contains a negative value.', 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "ContractCode",
                table: "L07_PriceList",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "L07_PriceList",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFromUtc",
                table: "L07_PriceList",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveToUtc",
                table: "L07_PriceList",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyBackfillStatus",
                table: "L07_PriceList",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "L07_PriceList",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "L07_PriceList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "L07_PriceList",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatPolicy",
                table: "L07_PriceList",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "L07_PriceList",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "L06_VPPSupplierMapping",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "L06_VPPSupplierMapping",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumOrderQuantity",
                table: "L06_VPPSupplierMapping",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetPrice",
                table: "L06_VPPSupplierMapping",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "L06_VPPSupplierMapping",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "SupplierSku",
                table: "L06_VPPSupplierMapping",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "L06_VPPSupplierMapping",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.Sql("""
                ;WITH SupplierOwnership AS
                (
                    SELECT
                        pl.[Id],
                        COUNT(DISTINCT CASE WHEN map.[IsDeleted] = 0 THEN CONVERT(nvarchar(36), map.[L05_VPPSupplierId]) END) AS SupplierCount,
                        MAX(CASE WHEN map.[IsDeleted] = 0 THEN CONVERT(nvarchar(36), map.[L05_VPPSupplierId]) END) AS SingleSupplierIdText
                    FROM [L07_PriceList] pl
                    LEFT JOIN [L06_VPPSupplierMapping] map ON map.[L07_PriceListId] = pl.[Id]
                    GROUP BY pl.[Id]
                ), RankedBooks AS
                (
                    SELECT
                        pl.[Id],
                        ownership.[SupplierCount],
                        CASE WHEN ownership.[SupplierCount] = 1
                            THEN TRY_CONVERT(uniqueidentifier, ownership.[SingleSupplierIdText])
                        END AS SupplierId,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY
                                CASE WHEN ownership.[SupplierCount] = 1 THEN ownership.[SingleSupplierIdText] END,
                                COALESCE(NULLIF(LTRIM(RTRIM(pl.[PriceListCode])), ''), CONVERT(nvarchar(36), pl.[Id]))
                            ORDER BY pl.[CreateDate], pl.[Id]
                        ) AS BackfillVersion
                    FROM [L07_PriceList] pl
                    INNER JOIN SupplierOwnership ownership ON ownership.[Id] = pl.[Id]
                )
                UPDATE pl
                SET
                    pl.[SupplierId] = ranked.[SupplierId],
                    pl.[Version] = ranked.[BackfillVersion],
                    pl.[EffectiveFromUtc] = pl.[CreateDate],
                    pl.[Status] = CASE WHEN pl.[IsDeleted] = 1 THEN 2 ELSE 1 END,
                    pl.[CurrencyCode] = 'VND',
                    pl.[VatPolicy] = 'legacy-zero',
                    pl.[LegacyBackfillStatus] = CASE
                        WHEN ranked.[SupplierCount] = 0 THEN 'NO_ACTIVE_ITEMS'
                        WHEN ranked.[SupplierCount] = 1 THEN 'BACKFILLED_VAT_ZERO'
                        ELSE 'MULTIPLE_SUPPLIERS_VAT_ZERO'
                    END
                FROM [L07_PriceList] pl
                INNER JOIN RankedBooks ranked ON ranked.[Id] = pl.[Id];

                UPDATE [L06_VPPSupplierMapping]
                SET
                    [NetPrice] = CONVERT(decimal(19,4), [Price]),
                    [VatRate] = 0,
                    [MinimumOrderQuantity] = 0,
                    [LeadTimeDays] = 0;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "L07_PriceList",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EffectiveFromUtc",
                table: "L07_PriceList",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "L07_PriceList",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VatPolicy",
                table: "L07_PriceList",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "L07_PriceList",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LeadTimeDays",
                table: "L06_VPPSupplierMapping",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinimumOrderQuantity",
                table: "L06_VPPSupplierMapping",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetPrice",
                table: "L06_VPPSupplierMapping",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "VatRate",
                table: "L06_VPPSupplierMapping",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_L07_PriceBook_Effective",
                table: "L07_PriceList",
                columns: new[] { "SupplierId", "Status", "EffectiveFromUtc", "EffectiveToUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_L07_PriceBook_Supplier_Version",
                table: "L07_PriceList",
                columns: new[] { "SupplierId", "PriceListCode", "Version" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [SupplierId] IS NOT NULL AND [PriceListCode] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L07_PriceBook_EffectiveWindow",
                table: "L07_PriceList",
                sql: "[EffectiveToUtc] IS NULL OR [EffectiveToUtc] > [EffectiveFromUtc]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L07_PriceBook_VersionPositive",
                table: "L07_PriceList",
                sql: "[Version] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_L06_PriceBookItem_Resolve",
                table: "L06_VPPSupplierMapping",
                columns: new[] { "L07_PriceListId", "L04_VPPId", "IsDeleted" })
                .Annotation("SqlServer:Include", new[] { "NetPrice", "VatRate", "MinimumOrderQuantity", "LeadTimeDays" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_L06_LeadTime_NonNegative",
                table: "L06_VPPSupplierMapping",
                sql: "[LeadTimeDays] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L06_Moq_NonNegative",
                table: "L06_VPPSupplierMapping",
                sql: "[MinimumOrderQuantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L06_Price_NonNegative",
                table: "L06_VPPSupplierMapping",
                sql: "[NetPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L06_VatRate_Range",
                table: "L06_VPPSupplierMapping",
                sql: "[VatRate] >= 0 AND [VatRate] <= 100");

            migrationBuilder.AddForeignKey(
                name: "FK_L07_PriceList_L05_VPPSupplier_SupplierId",
                table: "L07_PriceList",
                column: "SupplierId",
                principalTable: "L05_VPPSupplier",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                THROW 51019, 'PRICE-001 is forward-only because reverting would discard version, VAT and decimal price data. Restore a paired backup or apply a corrective migration.', 1;
                """);
        }
    }
}
