using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DesignDnaStudio.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDesignDnaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AnonymousCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Studies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumComparisons = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumComparisons = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsistencyProbeInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SettingsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Studies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantDisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Phase = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetComparisons = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ModelWeightsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ModelInformationJson = table.Column<string>(type: "TEXT", nullable: false),
                    ModelObservationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SideBias = table.Column<double>(type: "REAL", nullable: false),
                    SideBiasInformation = table.Column<double>(type: "REAL", nullable: false),
                    CoverageScore = table.Column<double>(type: "REAL", nullable: false),
                    ConsistencyScore = table.Column<double>(type: "REAL", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LastActivityAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentSessions_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentSessions_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stimuli",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviewType = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentFamily = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CalibrationPairKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    FeatureVectorJson = table.Column<string>(type: "TEXT", nullable: false),
                    PreviewSpecJson = table.Column<string>(type: "TEXT", nullable: false),
                    SourceReference = table.Column<string>(type: "TEXT", nullable: false),
                    AssetVersion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AccessibilityPass = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stimuli", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stimuli_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DesignProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceAssessmentSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelVersion = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RawProfileJson = table.Column<string>(type: "TEXT", nullable: false),
                    ReliabilityJson = table.Column<string>(type: "TEXT", nullable: false),
                    NarrativeMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    DesignBriefJson = table.Column<string>(type: "TEXT", nullable: false),
                    PresentationJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignProfiles_AssessmentSessions_SourceAssessmentSessionId",
                        column: x => x.SourceAssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignProfiles_Participants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignProfiles_Studies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "Studies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComparisonResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssessmentSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Sequence = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftStimulusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RightStimulusId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: true),
                    TieReason = table.Column<int>(type: "INTEGER", nullable: false),
                    SkipReason = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponseTimeMilliseconds = table.Column<long>(type: "INTEGER", nullable: false),
                    IsConsistencyProbe = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProbeOfComparisonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModelVersion = table.Column<string>(type: "TEXT", nullable: false),
                    LeftAssetVersion = table.Column<string>(type: "TEXT", nullable: false),
                    RightAssetVersion = table.Column<string>(type: "TEXT", nullable: false),
                    LeftPreviewType = table.Column<int>(type: "INTEGER", nullable: false),
                    RightPreviewType = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftFeatureVectorJson = table.Column<string>(type: "TEXT", nullable: false),
                    RightFeatureVectorJson = table.Column<string>(type: "TEXT", nullable: false),
                    LeftPreviewSpecJson = table.Column<string>(type: "TEXT", nullable: false),
                    RightPreviewSpecJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComparisonResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComparisonResponses_AssessmentSessions_AssessmentSessionId",
                        column: x => x.AssessmentSessionId,
                        principalTable: "AssessmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComparisonResponses_ComparisonResponses_ProbeOfComparisonId",
                        column: x => x.ProbeOfComparisonId,
                        principalTable: "ComparisonResponses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComparisonResponses_Stimuli_LeftStimulusId",
                        column: x => x.LeftStimulusId,
                        principalTable: "Stimuli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComparisonResponses_Stimuli_RightStimulusId",
                        column: x => x.RightStimulusId,
                        principalTable: "Stimuli",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_ParticipantId_StudyId_Status",
                table: "AssessmentSessions",
                columns: new[] { "ParticipantId", "StudyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentSessions_StudyId",
                table: "AssessmentSessions",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonResponses_AssessmentSessionId_Sequence",
                table: "ComparisonResponses",
                columns: new[] { "AssessmentSessionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonResponses_LeftStimulusId",
                table: "ComparisonResponses",
                column: "LeftStimulusId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonResponses_ProbeOfComparisonId",
                table: "ComparisonResponses",
                column: "ProbeOfComparisonId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonResponses_RightStimulusId",
                table: "ComparisonResponses",
                column: "RightStimulusId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignProfiles_ParticipantId_StudyId_Version",
                table: "DesignProfiles",
                columns: new[] { "ParticipantId", "StudyId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignProfiles_SourceAssessmentSessionId",
                table: "DesignProfiles",
                column: "SourceAssessmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignProfiles_StudyId",
                table: "DesignProfiles",
                column: "StudyId");

            migrationBuilder.CreateIndex(
                name: "IX_Participants_AnonymousCode",
                table: "Participants",
                column: "AnonymousCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stimuli_StudyId_ContentFamily_CalibrationPairKey",
                table: "Stimuli",
                columns: new[] { "StudyId", "ContentFamily", "CalibrationPairKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Stimuli_StudyId_SortOrder",
                table: "Stimuli",
                columns: new[] { "StudyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Studies_Slug",
                table: "Studies",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComparisonResponses");

            migrationBuilder.DropTable(
                name: "DesignProfiles");

            migrationBuilder.DropTable(
                name: "Stimuli");

            migrationBuilder.DropTable(
                name: "AssessmentSessions");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Studies");
        }
    }
}
