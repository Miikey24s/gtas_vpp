using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodAdjustmentDeadlineSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods");

            migrationBuilder.AddColumn<DateTime>(
                name: "PostCloseAdjustmentDeadlineUtc",
                table: "Periods",
                type: "datetime2",
                nullable: true);

            // Chụp thời hạn điều chỉnh từ cấu hình đang gắn với từng kỳ cũ.
            migrationBuilder.Sql(
                """
                UPDATE periodRow
                SET PostCloseAdjustmentDeadlineUtc = DATEADD(
                    day,
                    COALESCE(settingsRow.PostCloseAdjustmentDays, 10),
                    periodRow.SubmissionDeadlineUtc)
                FROM Periods AS periodRow
                LEFT JOIN OrderPeriodSettingsVersions AS settingsRow
                    ON settingsRow.Id = periodRow.SettingsVersionId
                WHERE periodRow.PostCloseAdjustmentDeadlineUtc IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods",
                sql: "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND ([PostCloseAdjustmentDeadlineUtc] IS NULL OR [PostCloseAdjustmentDeadlineUtc] >= [SupplementApprovalDeadlineUtc]) AND [State] IN (0, 1, 2, 3, 4, 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods");

            migrationBuilder.DropColumn(
                name: "PostCloseAdjustmentDeadlineUtc",
                table: "Periods");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods",
                sql: "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND [State] IN (0, 1, 2, 3, 4, 5)");
        }
    }
}
