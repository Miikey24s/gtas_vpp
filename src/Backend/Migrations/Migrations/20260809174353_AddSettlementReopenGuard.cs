using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementReopenGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.AddColumn<bool>(
                name: "HasExternalProcurementImpact",
                table: "Settlements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReopenCommandPayloadHash",
                table: "Settlements",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenIdempotencyKey",
                table: "Settlements",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenReason",
                table: "Settlements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAtUtc",
                table: "Settlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReopenedByUserId",
                table: "Settlements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementReopenWindowDays",
                table: "OrderPeriodSettingsVersions",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions",
                sql: "[VersionNumber] > 0 AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 AND [SettlementReopenWindowDays] BETWEEN 0 AND 30 AND [EffectiveFromYear] BETWEEN 1 AND 9999 AND [EffectiveFromMonth] BETWEEN 1 AND 12");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.DropColumn(
                name: "HasExternalProcurementImpact",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReopenCommandPayloadHash",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReopenIdempotencyKey",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReopenReason",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReopenedAtUtc",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReopenedByUserId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "SettlementReopenWindowDays",
                table: "OrderPeriodSettingsVersions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderPeriodSettings_Ranges",
                table: "OrderPeriodSettingsVersions",
                sql: "[VersionNumber] > 0 AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 AND [EffectiveFromYear] BETWEEN 1 AND 9999 AND [EffectiveFromMonth] BETWEEN 1 AND 12");
        }
    }
}
