using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RetireLegacyRollingPeriods202609And202610 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dọn đúng hai kỳ dư được cơ chế mở trước nhiều kỳ cũ tạo ra.
            // Kỳ có đơn, bản chốt, yêu cầu điều chỉnh hoặc do quản lý tạo sẽ không bị chạm tới.
            migrationBuilder.Sql(
                """
                UPDATE targetPeriod
                SET IsDeleted = 1,
                    UpdatedAtUtc = SYSUTCDATETIME(),
                    LastTransitionAtUtc = SYSUTCDATETIME(),
                    LastTransitionReason = N'legacy-rolling-horizon-retired'
                FROM Periods AS targetPeriod
                WHERE targetPeriod.IsDeleted = 0
                  AND targetPeriod.[Year] = 2026
                  AND targetPeriod.[Month] IN (9, 10)
                  AND targetPeriod.[State] = 0
                  AND targetPeriod.LastTransitionReason = N'rolling-horizon-top-up'
                  AND EXISTS
                  (
                      SELECT 1
                      FROM Periods AS currentPeriod
                      WHERE currentPeriod.IsDeleted = 0
                        AND currentPeriod.MemberCompanyCode = targetPeriod.MemberCompanyCode
                        AND currentPeriod.[Year] = 2026
                        AND currentPeriod.[Month] = 8
                        AND currentPeriod.[State] = 0
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Requests AS requestRow
                      WHERE requestRow.PeriodId = targetPeriod.Id
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Settlements AS settlementRow
                      WHERE settlementRow.PeriodId = targetPeriod.Id
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM PostSettlementOrderCorrections AS correctionRow
                      WHERE correctionRow.PeriodId = targetPeriod.Id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ khôi phục các dòng do chính migration này đánh dấu và vẫn an toàn.
            migrationBuilder.Sql(
                """
                UPDATE targetPeriod
                SET IsDeleted = 0,
                    UpdatedAtUtc = SYSUTCDATETIME(),
                    LastTransitionAtUtc = SYSUTCDATETIME(),
                    LastTransitionReason = N'rolling-horizon-top-up'
                FROM Periods AS targetPeriod
                WHERE targetPeriod.IsDeleted = 1
                  AND targetPeriod.[Year] = 2026
                  AND targetPeriod.[Month] IN (9, 10)
                  AND targetPeriod.LastTransitionReason = N'legacy-rolling-horizon-retired'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Periods AS activePeriod
                      WHERE activePeriod.IsDeleted = 0
                        AND activePeriod.MemberCompanyCode = targetPeriod.MemberCompanyCode
                        AND activePeriod.[Year] = targetPeriod.[Year]
                        AND activePeriod.[Month] = targetPeriod.[Month]
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Requests AS requestRow
                      WHERE requestRow.PeriodId = targetPeriod.Id
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM Settlements AS settlementRow
                      WHERE settlementRow.PeriodId = targetPeriod.Id
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM PostSettlementOrderCorrections AS correctionRow
                      WHERE correctionRow.PeriodId = targetPeriod.Id
                  );
                """);
        }
    }
}
