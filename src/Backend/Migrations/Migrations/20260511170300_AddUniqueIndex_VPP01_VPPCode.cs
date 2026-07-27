using gtas_vpp_be.Model;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    [DbContext(typeof(VPPMigrationDbContext))]
    [Migration("20260511170300_AddUniqueIndex_VPP01_VPPCode")]
    public partial class AddUniqueIndex_VPP01_VPPCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE VPP01_RequestHeader
SET VPPCode = CONCAT('VPP-LEGACY-', LEFT(CONVERT(varchar(36), Id), 11))
WHERE VPPCode IN (
    SELECT VPPCode
    FROM VPP01_RequestHeader
    WHERE VPPCode IS NOT NULL
    GROUP BY VPPCode
    HAVING COUNT(*) > 1
);");

            migrationBuilder.AlterColumn<string>(
                name: "VPPCode",
                table: "VPP01_RequestHeader",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_VPPCode",
                table: "VPP01_RequestHeader",
                column: "VPPCode",
                unique: true,
                filter: "[VPPCode] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_VPP01_VPPCode",
                table: "VPP01_RequestHeader");

            migrationBuilder.AlterColumn<string>(
                name: "VPPCode",
                table: "VPP01_RequestHeader",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
