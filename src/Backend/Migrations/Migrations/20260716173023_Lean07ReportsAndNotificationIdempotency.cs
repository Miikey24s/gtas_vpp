using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Lean07ReportsAndNotificationIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_N01_User_Company_Type_Correlation",
                table: "N01_Notification",
                columns: new[] { "UserId", "MemberCompanyCode", "Type", "CorrelationId" },
                unique: true,
                filter: "[CorrelationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_N01_User_Company_Type_Correlation",
                table: "N01_Notification");
        }
    }
}
