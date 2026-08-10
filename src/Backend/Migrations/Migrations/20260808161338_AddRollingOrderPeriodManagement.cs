using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace gtas_vpp_be.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRollingOrderPeriodManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods");

            migrationBuilder.AddColumn<Guid>(
                name: "SettingsVersionId",
                table: "Periods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderPeriodSettingsVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DefaultOpenPeriodCount = table.Column<int>(type: "int", nullable: false),
                    DefaultNewPeriodOpenDay = table.Column<int>(type: "int", nullable: false),
                    DefaultPeriodCloseDay = table.Column<int>(type: "int", nullable: false),
                    LocalTimeOfDay = table.Column<TimeSpan>(type: "time", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SupplementApprovalGraceDays = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromYear = table.Column<int>(type: "int", nullable: false),
                    EffectiveFromMonth = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPeriodSettingsVersions", x => x.Id);
                    table.CheckConstraint("CK_OrderPeriodSettings_Ranges", "[VersionNumber] > 0 AND [DefaultOpenPeriodCount] BETWEEN 0 AND 12 AND [DefaultNewPeriodOpenDay] BETWEEN 1 AND 31 AND [DefaultPeriodCloseDay] BETWEEN 1 AND 31 AND [SupplementApprovalGraceDays] BETWEEN 0 AND 31 AND [EffectiveFromYear] BETWEEN 1 AND 9999 AND [EffectiveFromMonth] BETWEEN 1 AND 12");
                });

            migrationBuilder.CreateTable(
                name: "PostSettlementOrderCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestRevisionNumber = table.Column<int>(type: "int", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberCompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmployeeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DecisionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultSettlementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSettlementOrderCorrections", x => x.Id);
                    table.CheckConstraint("CK_PostSettlementCorrections_State", "[Action] IN (0, 1) AND [Status] IN (0, 1, 2, 3) AND [RequestRevisionNumber] > 0");
                    table.ForeignKey(
                        name: "FK_PostSettlementOrderCorrections_Periods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "Periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostSettlementOrderCorrections_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostSettlementOrderCorrections_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostSettlementOrderCorrectionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VppId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSettlementOrderCorrectionItems", x => x.Id);
                    table.CheckConstraint("CK_PostSettlementCorrectionItems_Qty", "[Qty] > 0");
                    table.ForeignKey(
                        name: "FK_PostSettlementOrderCorrectionItems_PostSettlementOrderCorrections_CorrectionId",
                        column: x => x.CorrectionId,
                        principalTable: "PostSettlementOrderCorrections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Periods_SettingsVersionId",
                table: "Periods",
                column: "SettingsVersionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods",
                sql: "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND [State] IN (0, 1, 2, 3, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPeriodSettings_Company_Effective",
                table: "OrderPeriodSettingsVersions",
                columns: new[] { "MemberCompanyCode", "EffectiveFromYear", "EffectiveFromMonth", "VersionNumber" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_OrderPeriodSettings_Company_Version",
                table: "OrderPeriodSettingsVersions",
                columns: new[] { "MemberCompanyCode", "VersionNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PostSettlementOrderCorrectionItems_CorrectionId_VppId",
                table: "PostSettlementOrderCorrectionItems",
                columns: new[] { "CorrectionId", "VppId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostSettlementOrderCorrections_MemberCompanyCode_PeriodId_Status",
                table: "PostSettlementOrderCorrections",
                columns: new[] { "MemberCompanyCode", "PeriodId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSettlementOrderCorrections_PeriodId",
                table: "PostSettlementOrderCorrections",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSettlementOrderCorrections_RequestId",
                table: "PostSettlementOrderCorrections",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSettlementOrderCorrections_SettlementId",
                table: "PostSettlementOrderCorrections",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "UX_PostSettlementCorrections_PendingSeries",
                table: "PostSettlementOrderCorrections",
                columns: new[] { "MemberCompanyCode", "RequestSeriesId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [Status] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Periods_OrderPeriodSettingsVersions_SettingsVersionId",
                table: "Periods",
                column: "SettingsVersionId",
                principalTable: "OrderPeriodSettingsVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Periods_OrderPeriodSettingsVersions_SettingsVersionId",
                table: "Periods");

            migrationBuilder.DropTable(
                name: "OrderPeriodSettingsVersions");

            migrationBuilder.DropTable(
                name: "PostSettlementOrderCorrectionItems");

            migrationBuilder.DropTable(
                name: "PostSettlementOrderCorrections");

            migrationBuilder.DropIndex(
                name: "IX_Periods_SettingsVersionId",
                table: "Periods");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods");

            migrationBuilder.DropColumn(
                name: "SettingsVersionId",
                table: "Periods");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Periods_ValidRange",
                table: "Periods",
                sql: "[Year] BETWEEN 1 AND 9999 AND [Month] BETWEEN 1 AND 12 AND [SubmissionDeadlineUtc] > [StartAtUtc] AND [SupplementApprovalDeadlineUtc] >= [SubmissionDeadlineUtc] AND [State] IN (0, 1, 2, 3)");
        }
    }
}
