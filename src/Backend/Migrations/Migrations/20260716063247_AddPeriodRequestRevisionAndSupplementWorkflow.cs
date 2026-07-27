using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodRequestRevisionAndSupplementWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail before any schema mutation when a legacy row cannot be
            // represented by the durable period aggregate.  SQL Server cannot
            // calculate the following month's day 05 for December 9999.
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.VPP01_RequestHeader
                    WHERE [Y] NOT BETWEEN 1 AND 9999
                       OR [M] NOT BETWEEN 1 AND 12
                       OR ([Y] = 9999 AND [M] = 12)
                )
                BEGIN
                    THROW 51001, 'LEAN-05 preflight failed: VPP01_RequestHeader contains an invalid year/month for period backfill.', 1;
                END;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.VPP01_RequestHeader
                    WHERE LEN(COALESCE(NULLIF(LTRIM(RTRIM(MemberCompanyCode)), N''), N'77500')) > 50
                )
                BEGIN
                    THROW 51002, 'LEAN-05 preflight failed: MemberCompanyCode exceeds 50 characters.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "UX_VPP01_OneRegularPerUserPeriod",
                table: "VPP01_RequestHeader");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "VPP03_Log",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActorUserId",
                table: "VPP03_Log",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "VPP03_Log",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MemberCompanyCode",
                table: "VPP03_Log",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "VPP03_Log",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "VPP03_Log",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseRequestId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseRequestSeriesId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "VPP01_RequestHeader",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "VPP01_RequestHeader",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledById",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommandPayloadHash",
                table: "VPP01_RequestHeader",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "VPP01_RequestHeader",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCurrentRevision",
                table: "VPP01_RequestHeader",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PeriodId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestSeriesId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "RevisionNumber",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByRequestId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesRequestId",
                table: "VPP01_RequestHeader",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplementAttemptNumber",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplementReason",
                table: "VPP01_RequestHeader",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplementSequence",
                table: "VPP01_RequestHeader",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VPP00_Period",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    M = table.Column<int>(type: "int", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmissionDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplementApprovalDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    LastTransitionUserId = table.Column<int>(type: "int", nullable: true),
                    LastTransitionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTransitionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreateUserId = table.Column<int>(type: "int", nullable: false),
                    CreateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateUserId = table.Column<int>(type: "int", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VPP00_Period", x => x.Id);
                    table.CheckConstraint("CK_VPP00_Period_ValidRange", "[Y] BETWEEN 1 AND 9999 AND [M] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND [State] IN (0, 1, 2, 3)");
                });

            // Backfill legacy rows inside the migration transaction.  Legacy
            // cancellation was implemented as a soft delete; restoring those
            // rows is required for the new immutable history view.
            migrationBuilder.Sql("""
                DECLARE @UtcNow datetime2(7) = SYSUTCDATETIME();

                UPDATE h
                SET h.IsDeleted = 0,
                    h.CancelledById = COALESCE(h.CancelledById, NULLIF(h.UpdateUserId, 0)),
                    h.CancelledAt = COALESCE(h.CancelledAt, h.UpdateDate)
                FROM dbo.VPP01_RequestHeader AS h
                WHERE h.Status = 4
                  AND h.IsDeleted = 1;

                UPDATE d
                SET d.IsDeleted = 0
                FROM dbo.VPP02_RequestDetail AS d
                INNER JOIN dbo.VPP01_RequestHeader AS h
                    ON h.Id = d.VPP01_RequestHeaderId
                WHERE h.Status = 4
                  AND h.IsDeleted = 0
                  AND d.IsDeleted = 1;

                UPDATE dbo.VPP01_RequestHeader
                SET MemberCompanyCode = N'77500'
                WHERE MemberCompanyCode IS NULL
                   OR LTRIM(RTRIM(MemberCompanyCode)) = N'';

                ;WITH PeriodSource AS
                (
                    SELECT
                        h.MemberCompanyCode,
                        h.Y,
                        h.M,
                        MIN(h.CreateDate) AS FirstCreateDate,
                        MAX(h.UpdateDate) AS LastUpdateDate,
                        MAX(CASE WHEN h.SettledAt IS NOT NULL THEN 1 ELSE 0 END) AS HasSettlement
                    FROM dbo.VPP01_RequestHeader AS h
                    GROUP BY h.MemberCompanyCode, h.Y, h.M
                )
                INSERT dbo.VPP00_Period
                (
                    Id,
                    MemberCompanyCode,
                    TimeZoneId,
                    Y,
                    M,
                    StartAtUtc,
                    SubmissionDeadlineUtc,
                    SupplementApprovalDeadlineUtc,
                    State,
                    LastTransitionUserId,
                    LastTransitionAtUtc,
                    LastTransitionReason,
                    Description,
                    CreateUserId,
                    CreateDate,
                    UpdateUserId,
                    UpdateDate,
                    IsDeleted
                )
                SELECT
                    NEWID(),
                    source.MemberCompanyCode,
                    N'Asia/Ho_Chi_Minh',
                    source.Y,
                    source.M,
                    boundary.StartAtUtc,
                    boundary.SubmissionDeadlineUtc,
                    DATEADD(DAY, 2, boundary.SubmissionDeadlineUtc),
                    CASE
                        WHEN source.HasSettlement = 1 THEN 3
                        WHEN @UtcNow >= DATEADD(DAY, 2, boundary.SubmissionDeadlineUtc) THEN 2
                        WHEN @UtcNow >= boundary.SubmissionDeadlineUtc THEN 1
                        ELSE 0
                    END,
                    NULL,
                    NULL,
                    N'Backfilled by LEAN-05 migration',
                    N'Durable period backfilled from legacy request headers',
                    0,
                    COALESCE(source.FirstCreateDate, @UtcNow),
                    0,
                    COALESCE(source.LastUpdateDate, @UtcNow),
                    0
                FROM PeriodSource AS source
                CROSS APPLY
                (
                    SELECT
                        DATEADD(
                            HOUR,
                            -7,
                            CAST(DATEFROMPARTS(source.Y, source.M, 5) AS datetime2(7))) AS StartAtUtc,
                        DATEADD(
                            HOUR,
                            -7,
                            DATEADD(
                                MONTH,
                                1,
                                CAST(DATEFROMPARTS(source.Y, source.M, 5) AS datetime2(7)))) AS SubmissionDeadlineUtc
                ) AS boundary;

                UPDATE h
                SET h.PeriodId = p.Id
                FROM dbo.VPP01_RequestHeader AS h
                INNER JOIN dbo.VPP00_Period AS p
                    ON p.MemberCompanyCode = h.MemberCompanyCode
                   AND p.Y = h.Y
                   AND p.M = h.M
                   AND p.IsDeleted = 0;

                -- Every pre-migration row begins as a one-revision series.
                -- Regular rows for the same owner/period are then folded into
                -- one chronological lineage so cancel-and-recreate history is
                -- preserved without exposing an obsolete cancelled request as
                -- a second actionable current request.
                UPDATE h
                SET h.RequestSeriesId = h.Id,
                    h.RevisionNumber = 1,
                    h.IsCurrentRevision = CASE WHEN h.IsDeleted = 0 THEN 1 ELSE 0 END,
                    h.SupersedesRequestId = NULL,
                    h.SupersededByRequestId = NULL
                FROM dbo.VPP01_RequestHeader AS h;

                ;WITH RankedRegular AS
                (
                    SELECT
                        h.Id,
                        FIRST_VALUE(h.Id) OVER
                        (
                            PARTITION BY h.CreateUserId, h.PeriodId
                            ORDER BY COALESCE(h.SubmittedDate, h.CreateDate) ASC,
                                     h.CreateDate ASC,
                                     h.UpdateDate ASC,
                                     h.Id ASC
                            ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING
                        ) AS SeriesId,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY h.CreateUserId, h.PeriodId
                            ORDER BY COALESCE(h.SubmittedDate, h.CreateDate) ASC,
                                     h.CreateDate ASC,
                                     h.UpdateDate ASC,
                                     h.Id ASC
                        ) AS RevisionNo,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY h.CreateUserId, h.PeriodId
                            ORDER BY COALESCE(h.SubmittedDate, h.CreateDate) DESC,
                                     h.CreateDate DESC,
                                     h.UpdateDate DESC,
                                     h.Id DESC
                        ) AS CurrentRank,
                        LAG(h.Id) OVER
                        (
                            PARTITION BY h.CreateUserId, h.PeriodId
                            ORDER BY COALESCE(h.SubmittedDate, h.CreateDate) ASC,
                                     h.CreateDate ASC,
                                     h.UpdateDate ASC,
                                     h.Id ASC
                        ) AS PreviousId,
                        LEAD(h.Id) OVER
                        (
                            PARTITION BY h.CreateUserId, h.PeriodId
                            ORDER BY COALESCE(h.SubmittedDate, h.CreateDate) ASC,
                                     h.CreateDate ASC,
                                     h.UpdateDate ASC,
                                     h.Id ASC
                        ) AS NextId
                    FROM dbo.VPP01_RequestHeader AS h
                    WHERE h.IsDeleted = 0
                      AND h.IsAdditionalOrder = 0
                )
                UPDATE h
                SET h.RequestSeriesId = ranked.SeriesId,
                    h.RevisionNumber = CONVERT(int, ranked.RevisionNo),
                    h.IsCurrentRevision = CASE WHEN ranked.CurrentRank = 1 THEN 1 ELSE 0 END,
                    h.SupersedesRequestId = ranked.PreviousId,
                    h.SupersededByRequestId = ranked.NextId
                FROM dbo.VPP01_RequestHeader AS h
                INNER JOIN RankedRegular AS ranked
                    ON ranked.Id = h.Id;

                -- Link a supplement to the regular revision that existed when
                -- it was created.  If no regular row exists, the nullable base
                -- fields deliberately remain null as an explicit legacy case.
                UPDATE supplement
                SET supplement.BaseRequestId = baseRequest.Id,
                    supplement.BaseRequestSeriesId = baseRequest.RequestSeriesId
                FROM dbo.VPP01_RequestHeader AS supplement
                OUTER APPLY
                (
                    SELECT TOP (1)
                        regular.Id,
                        regular.RequestSeriesId
                    FROM dbo.VPP01_RequestHeader AS regular
                    WHERE regular.IsDeleted = 0
                      AND regular.IsAdditionalOrder = 0
                      AND regular.CreateUserId = supplement.CreateUserId
                      AND regular.PeriodId = supplement.PeriodId
                    ORDER BY
                        CASE
                            WHEN COALESCE(regular.SubmittedDate, regular.CreateDate)
                                 <= COALESCE(supplement.SubmittedDate, supplement.CreateDate)
                            THEN 0 ELSE 1
                        END,
                        CASE
                            WHEN COALESCE(regular.SubmittedDate, regular.CreateDate)
                                 <= COALESCE(supplement.SubmittedDate, supplement.CreateDate)
                            THEN COALESCE(regular.SubmittedDate, regular.CreateDate)
                        END DESC,
                        CASE
                            WHEN COALESCE(regular.SubmittedDate, regular.CreateDate)
                                 > COALESCE(supplement.SubmittedDate, supplement.CreateDate)
                            THEN COALESCE(regular.SubmittedDate, regular.CreateDate)
                        END ASC,
                        regular.Id ASC
                ) AS baseRequest
                WHERE supplement.IsDeleted = 0
                  AND supplement.IsAdditionalOrder = 1;

                UPDATE supplement
                SET supplement.SupplementReason = LEFT(
                        NULLIF(LTRIM(RTRIM(supplement.Description)), N''),
                        500)
                FROM dbo.VPP01_RequestHeader AS supplement
                WHERE supplement.IsAdditionalOrder = 1
                  AND (supplement.SupplementReason IS NULL
                       OR LTRIM(RTRIM(supplement.SupplementReason)) = N'');

                ;WITH NumberedSupplements AS
                (
                    SELECT
                        supplement.Id,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY supplement.CreateUserId,
                                         supplement.PeriodId,
                                         supplement.BaseRequestSeriesId
                            ORDER BY COALESCE(supplement.SubmittedDate, supplement.CreateDate) ASC,
                                     supplement.CreateDate ASC,
                                     supplement.UpdateDate ASC,
                                     supplement.Id ASC
                        ) AS AttemptNo
                    FROM dbo.VPP01_RequestHeader AS supplement
                    WHERE supplement.IsDeleted = 0
                      AND supplement.IsAdditionalOrder = 1
                )
                UPDATE supplement
                SET supplement.SupplementAttemptNumber = CONVERT(int, numbered.AttemptNo),
                    supplement.SupplementSequence = CONVERT(int, numbered.AttemptNo)
                FROM dbo.VPP01_RequestHeader AS supplement
                INNER JOIN NumberedSupplements AS numbered
                    ON numbered.Id = supplement.Id;

                -- Only fields supported by legacy facts are inferred.  Unknown
                -- correlation IDs and cancellation reasons remain null.
                UPDATE logEntry
                SET logEntry.Action = LEFT(
                        COALESCE(NULLIF(LTRIM(RTRIM(logEntry.LogTitle)), N''), N'LEGACY_EVENT'),
                        64),
                    logEntry.ActorUserId = CASE UPPER(LTRIM(RTRIM(COALESCE(logEntry.LogTitle, N''))))
                        WHEN N'CREATE' THEN NULLIF(header.CreateUserId, 0)
                        WHEN N'UPDATE' THEN NULLIF(header.UpdateUserId, 0)
                        WHEN N'CANCEL' THEN COALESCE(header.CancelledById, NULLIF(header.UpdateUserId, 0))
                        WHEN N'APPROVE' THEN header.ApprovedById
                        WHEN N'REJECT' THEN header.RejectedById
                        WHEN N'SETTLE' THEN header.SettledByUserId
                        ELSE NULL
                    END,
                    logEntry.MemberCompanyCode = header.MemberCompanyCode,
                    logEntry.RevisionNumber = header.RevisionNumber,
                    logEntry.Reason = CASE
                        WHEN UPPER(LTRIM(RTRIM(COALESCE(logEntry.LogTitle, N'')))) = N'REJECT'
                        THEN LEFT(header.RejectReason, 500)
                        ELSE logEntry.Reason
                    END
                FROM dbo.VPP03_Log AS logEntry
                INNER JOIN dbo.VPP01_RequestHeader AS header
                    ON header.Id = logEntry.VPP01_RequestHeaderId;

                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.VPP01_RequestHeader
                    WHERE PeriodId IS NULL
                       OR RequestSeriesId = '00000000-0000-0000-0000-000000000000'
                       OR RevisionNumber < 1
                )
                BEGIN
                    THROW 51003, 'LEAN-05 backfill failed: a request has incomplete period or revision lineage.', 1;
                END;

                IF EXISTS
                (
                    SELECT RequestSeriesId
                    FROM dbo.VPP01_RequestHeader
                    WHERE IsDeleted = 0
                      AND IsCurrentRevision = 1
                    GROUP BY RequestSeriesId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51004, 'LEAN-05 backfill failed: a request series has multiple current revisions.', 1;
                END;

                IF EXISTS
                (
                    SELECT CreateUserId, PeriodId
                    FROM dbo.VPP01_RequestHeader
                    WHERE IsDeleted = 0
                      AND IsCurrentRevision = 1
                      AND IsAdditionalOrder = 0
                    GROUP BY CreateUserId, PeriodId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51005, 'LEAN-05 backfill failed: multiple current regular requests exist for one user and period.', 1;
                END;

                IF EXISTS
                (
                    SELECT CreateUserId, PeriodId, BaseRequestSeriesId
                    FROM dbo.VPP01_RequestHeader
                    WHERE IsDeleted = 0
                      AND IsCurrentRevision = 1
                      AND IsAdditionalOrder = 1
                      AND Status = 6
                    GROUP BY CreateUserId, PeriodId, BaseRequestSeriesId
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51006, 'LEAN-05 backfill failed: multiple pending supplements target the same regular request.', 1;
                END;

                IF EXISTS
                (
                    SELECT CreateUserId, PeriodId, BaseRequestSeriesId, SupplementAttemptNumber
                    FROM dbo.VPP01_RequestHeader
                    WHERE IsDeleted = 0
                      AND IsCurrentRevision = 1
                      AND IsAdditionalOrder = 1
                      AND SupplementAttemptNumber IS NOT NULL
                    GROUP BY CreateUserId, PeriodId, BaseRequestSeriesId, SupplementAttemptNumber
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51007, 'LEAN-05 backfill failed: duplicate supplement attempt numbers were produced.', 1;
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_VPP01_RequestHeader_PeriodId",
                table: "VPP01_RequestHeader",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_CurrentRevisionSeries",
                table: "VPP01_RequestHeader",
                column: "RequestSeriesId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_IdempotencyKey",
                table: "VPP01_RequestHeader",
                columns: new[] { "CreateUserId", "IdempotencyKey" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_OnePendingSupplement",
                table: "VPP01_RequestHeader",
                columns: new[] { "CreateUserId", "PeriodId", "BaseRequestSeriesId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [Status] = 6");

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_OneRegularPerUserPeriod",
                table: "VPP01_RequestHeader",
                columns: new[] { "CreateUserId", "PeriodId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_VPP01_SupplementAttempt",
                table: "VPP01_RequestHeader",
                columns: new[] { "CreateUserId", "PeriodId", "BaseRequestSeriesId", "SupplementAttemptNumber" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsCurrentRevision] = 1 AND [IsAdditionalOrder] = 1 AND [SupplementAttemptNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VPP00_Period_Company_State_Deadline",
                table: "VPP00_Period",
                columns: new[] { "MemberCompanyCode", "State", "SubmissionDeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_VPP00_Period_Company_Year_Month_Active",
                table: "VPP00_Period",
                columns: new[] { "MemberCompanyCode", "Y", "M" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_VPP01_RequestHeader_VPP00_Period_PeriodId",
                table: "VPP01_RequestHeader",
                column: "PeriodId",
                principalTable: "VPP00_Period",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "LEAN-05 is a forward-only data migration. Restore a verified pre-migration database backup instead of running Down().");
        }
    }
}
