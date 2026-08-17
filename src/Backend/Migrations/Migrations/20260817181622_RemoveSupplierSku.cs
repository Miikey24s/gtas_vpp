using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierSku",
                table: "SupplierProductMappings");

            migrationBuilder.DropColumn(
                name: "SupplierSku",
                table: "SettlementItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierSku",
                table: "SupplierProductMappings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierSku",
                table: "SettlementItems",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
