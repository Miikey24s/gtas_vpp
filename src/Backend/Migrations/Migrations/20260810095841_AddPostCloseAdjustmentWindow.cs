using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCloseAdjustmentWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.AddColumn<int>(
                name: "PostCloseAdjustmentDays",
                table: "OrderPeriodSettingsVersions",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.Sql(
                "UPDATE [OrderPeriodSettingsVersions] " +
                "SET [PostCloseAdjustmentDays] = [SupplementApprovalGraceDays] " +
                "WHERE [SupplementApprovalGraceDays] > [PostCloseAdjustmentDays]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions",
                sql: "[VersionNumber] > 0 AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 AND [PostCloseAdjustmentDays] BETWEEN 0 AND 31 AND [SupplementApprovalGraceDays] <= [PostCloseAdjustmentDays] AND [SettlementReopenWindowDays] BETWEEN 0 AND 30 AND [EffectiveFromYear] BETWEEN 1 AND 9999 AND [EffectiveFromMonth] BETWEEN 1 AND 12");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.DropColumn(
                name: "PostCloseAdjustmentDays",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions",
                sql: "[VersionNumber] > 0 AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 AND [SettlementReopenWindowDays] BETWEEN 0 AND 30 AND [EffectiveFromYear] BETWEEN 1 AND 9999 AND [EffectiveFromMonth] BETWEEN 1 AND 12");
        }
    }
}
