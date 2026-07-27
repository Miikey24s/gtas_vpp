using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddVPPRequestHeaderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VPP01_RequestHeader_Period_Status",
                table: "VPP01_RequestHeader",
                columns: new[] { "Y", "M", "IsDeleted", "Status", "IsAdditionalOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VPP01_RequestHeader_User_Period_Status",
                table: "VPP01_RequestHeader",
                columns: new[] { "CreateUserId", "Y", "M", "IsDeleted", "IsAdditionalOrder", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VPP01_RequestHeader_Period_Status",
                table: "VPP01_RequestHeader");

            migrationBuilder.DropIndex(
                name: "IX_VPP01_RequestHeader_User_Period_Status",
                table: "VPP01_RequestHeader");
        }
    }
}
