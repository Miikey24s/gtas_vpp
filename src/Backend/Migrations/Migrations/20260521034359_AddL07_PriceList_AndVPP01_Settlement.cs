using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddL07_PriceList_AndVPP01_Settlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "L07_PriceList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceListCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PriceListName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_L07_PriceList", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_L07_OneDefault",
                table: "L07_PriceList",
                column: "IsDefault",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.InsertData(
                table: "L07_PriceList",
                columns: new[]
                {
                    "Id",
                    "PriceListCode",
                    "PriceListName",
                    "Description",
                    "IsDefault",
                    "CreateUserId",
                    "CreateDate",
                    "UpdateUserId",
                    "UpdateDate",
                    "IsDeleted"
                },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000700"),
                    "DEFAULT",
                    "Default Price List",
                    null,
                    true,
                    5615,
                    new DateTime(2026, 1, 1),
                    5615,
                    new DateTime(2026, 1, 1),
                    false
                });

            migrationBuilder.AddColumn<Guid>(
                name: "L07_PriceListId",
                table: "L06_VPPSupplierMapping",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE L06_VPPSupplierMapping
                SET L07_PriceListId = '00000000-0000-0000-0000-000000000700'
                WHERE L07_PriceListId IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "L07_PriceListId",
                table: "L06_VPPSupplierMapping",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UX_L06_OneDefaultPerVPP'
                      AND object_id = OBJECT_ID(N'[dbo].[L06_VPPSupplierMapping]')
                )
                    DROP INDEX [UX_L06_OneDefaultPerVPP] ON [dbo].[L06_VPPSupplierMapping]
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_L06_VPPSupplierMapping_L04_VPPId'
                      AND object_id = OBJECT_ID(N'[dbo].[L06_VPPSupplierMapping]')
                )
                    CREATE INDEX [IX_L06_VPPSupplierMapping_L04_VPPId]
                    ON [dbo].[L06_VPPSupplierMapping] ([L04_VPPId])
                """);

            migrationBuilder.CreateIndex(
                name: "UX_L06_OneDefaultPerVPPPerList",
                table: "L06_VPPSupplierMapping",
                columns: new[] { "L07_PriceListId", "L04_VPPId" },
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_L06_VPPSupplierMapping_L07_PriceList_L07_PriceListId",
                table: "L06_VPPSupplierMapping",
                column: "L07_PriceListId",
                principalTable: "L07_PriceList",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<DateTime>(
                name: "SettledAt",
                table: "VPP01_RequestHeader",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SettledByPriceListId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettledByUserId",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VPP01_RequestHeader_SettledByPriceListId",
                table: "VPP01_RequestHeader",
                column: "SettledByPriceListId");

            migrationBuilder.AddForeignKey(
                name: "FK_VPP01_RequestHeader_L07_PriceList_SettledByPriceListId",
                table: "VPP01_RequestHeader",
                column: "SettledByPriceListId",
                principalTable: "L07_PriceList",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_L06_VPPSupplierMapping_L07_PriceList_L07_PriceListId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropForeignKey(
                name: "FK_VPP01_RequestHeader_L07_PriceList_SettledByPriceListId",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropIndex(
                name: "IX_VPP01_RequestHeader_SettledByPriceListId",
                table: "VPP01_RequestHeader");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_L06_VPPSupplierMapping_L04_VPPId'
                      AND object_id = OBJECT_ID(N'[dbo].[L06_VPPSupplierMapping]')
                )
                    DROP INDEX [IX_L06_VPPSupplierMapping_L04_VPPId] ON [dbo].[L06_VPPSupplierMapping]
                """);

            migrationBuilder.DropIndex(
                name: "UX_L06_OneDefaultPerVPPPerList",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "SettledByPriceListId",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "SettledByUserId",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropColumn(
                name: "L07_PriceListId",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropTable(
                name: "L07_PriceList");

            migrationBuilder.CreateIndex(
                name: "UX_L06_OneDefaultPerVPP",
                table: "L06_VPPSupplierMapping",
                column: "L04_VPPId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }
    }
}
