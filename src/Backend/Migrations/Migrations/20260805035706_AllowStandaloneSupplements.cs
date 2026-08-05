using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AllowStandaloneSupplements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[Requests]
                    WHERE [IsDeleted] = 0
                      AND [IsCurrentRevision] = 1
                      AND [IsAdditionalOrder] = 1
                      AND [Status] = 6
                    GROUP BY [CreatedByUserId], [PeriodId]
                    HAVING COUNT(*) > 1
                )
                    THROW 51020, 'Standalone supplement migration found multiple pending requests for one user and period.', 1;

                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[Requests]
                    WHERE [IsDeleted] = 0
                      AND [IsCurrentRevision] = 1
                      AND [IsAdditionalOrder] = 1
                      AND [SupplementAttemptNumber] IS NOT NULL
                    GROUP BY [CreatedByUserId], [PeriodId], [SupplementAttemptNumber]
                    HAVING COUNT(*) > 1
                )
                    THROW 51021, 'Standalone supplement migration found duplicate attempt numbers for one user and period.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "UX_Requests_OnePendingSupplement",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "UX_Requests_SupplementAttempt",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "UX_Requests_OnePendingSupplement",
                table: "Requests",
                columns: new[] { "CreatedByUserId", "PeriodId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [Status] = 6");

            migrationBuilder.CreateIndex(
                name: "UX_Requests_SupplementAttempt",
                table: "Requests",
                columns: new[] { "CreatedByUserId", "PeriodId", "SupplementAttemptNumber" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [SupplementAttemptNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Requests_OnePendingSupplement",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "UX_Requests_SupplementAttempt",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "UX_Requests_OnePendingSupplement",
                table: "Requests",
                columns: new[] { "CreatedByUserId", "PeriodId", "BaseRequestSeriesId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [Status] = 6");

            migrationBuilder.CreateIndex(
                name: "UX_Requests_SupplementAttempt",
                table: "Requests",
                columns: new[] { "CreatedByUserId", "PeriodId", "BaseRequestSeriesId", "SupplementAttemptNumber" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [SupplementAttemptNumber] IS NOT NULL");
        }
    }
}
