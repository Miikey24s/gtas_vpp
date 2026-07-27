using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Lean06CatalogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM dbo.L04_VPP
    WHERE VPPCode IS NULL OR NULLIF(LTRIM(RTRIM(VPPCode)), N'') IS NULL OR LEN(VPPCode) > 64
       OR VPPName IS NULL OR NULLIF(LTRIM(RTRIM(VPPName)), N'') IS NULL OR LEN(VPPName) > 250
)
    THROW 51001, 'LEAN-06 CAT-001 preflight failed: L04_VPP contains blank or over-length code/name.', 1;

IF EXISTS (
    SELECT LTRIM(RTRIM(VPPCode))
    FROM dbo.L04_VPP
    GROUP BY LTRIM(RTRIM(VPPCode))
    HAVING COUNT(*) > 1
)
    THROW 51002, 'LEAN-06 CAT-001 preflight failed: duplicate L04_VPP codes must be resolved before the unique index.', 1;
");

            migrationBuilder.AlterColumn<string>(
                name: "VPPName",
                table: "L04_VPP",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VPPCode",
                table: "L04_VPP",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_L04_VPP_Active_Category_Code",
                table: "L04_VPP",
                columns: new[] { "IsDeleted", "VPPCategoryId", "VPPCode" })
                .Annotation("SqlServer:Include", new[] { "VPPName", "UOMId" });

            migrationBuilder.CreateIndex(
                name: "UX_L04_VPP_VPPCode",
                table: "L04_VPP",
                column: "VPPCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_L04_VPP_Active_Category_Code",
                table: "L04_VPP");

            migrationBuilder.DropIndex(
                name: "UX_L04_VPP_VPPCode",
                table: "L04_VPP");

            migrationBuilder.AlterColumn<string>(
                name: "VPPName",
                table: "L04_VPP",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "VPPCode",
                table: "L04_VPP",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);
        }
    }
}
