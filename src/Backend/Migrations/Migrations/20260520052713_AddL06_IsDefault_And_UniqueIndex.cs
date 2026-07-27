using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddL06_IsDefault_And_UniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "L06_VPPSupplierMapping",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "UX_L06_OneDefaultPerVPP",
                table: "L06_VPPSupplierMapping",
                column: "L04_VPPId",
                unique: true,
                filter: "[IsDefault] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_L06_OneDefaultPerVPP",
                table: "L06_VPPSupplierMapping");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "L06_VPPSupplierMapping");
        }
    }
}
