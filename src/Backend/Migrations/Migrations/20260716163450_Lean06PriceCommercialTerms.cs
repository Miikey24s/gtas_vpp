using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Lean06PriceCommercialTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "L07_PriceList",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                table: "L07_PriceList",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExpiredByUserId",
                table: "L07_PriceList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "L07_PriceList",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PublishedByUserId",
                table: "L07_PriceList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RebateAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StatusReason",
                table: "L07_PriceList",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_L07_PriceBook_CommercialAmounts",
                table: "L07_PriceList",
                sql: "[RebateAmount] >= 0 AND [FeeAmount] >= 0 AND [ShippingAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_L07_PriceBook_DiscountRate",
                table: "L07_PriceList",
                sql: "[DiscountRate] >= 0 AND [DiscountRate] <= 100");

            migrationBuilder.Sql("""
                UPDATE [L07_PriceList]
                SET [DiscountRate] = COALESCE([DiscountRate], 0),
                    [RebateAmount] = COALESCE([RebateAmount], 0),
                    [FeeAmount] = COALESCE([FeeAmount], 0),
                    [ShippingAmount] = COALESCE([ShippingAmount], 0);
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscountRate",
                table: "L07_PriceList",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "RebateAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingAmount",
                table: "L07_PriceList",
                type: "decimal(19,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(19,4)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                THROW 51029, 'PRICE-002 commercial terms are forward-only; restore a paired backup or apply a corrective migration.', 1;
                """);
        }
    }
}
