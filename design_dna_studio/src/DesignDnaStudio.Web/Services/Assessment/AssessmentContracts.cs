using System.Text.Json;
using DesignDnaStudio.Engine;
using DesignDnaStudio.Web.Data.Entities;
using DesignDnaStudio.Web.Models;

namespace DesignDnaStudio.Web.Services.Assessment;

public sealed record StudySummary(
    Guid Id,
    string Name,
    string Description,
    int StimulusCount,
    int MinimumComparisons,
    int MaximumComparisons);

public sealed record AssessmentStartResult(
    Guid SessionId,
    Guid ParticipantId,
    string ParticipantCode,
    int TargetComparisons);

public sealed record ParticipantSummary(
    Guid Id,
    string DisplayName,
    int CompletedProfileCount);

public sealed record StimulusView(
    Guid Id,
    StimulusPreviewType PreviewType,
    StimulusPreviewSpec Spec);

public sealed record ComparisonPrompt(
    Guid SessionId,
    int Sequence,
    int AnswerCount,
    int TargetComparisons,
    AssessmentPhase Phase,
    StimulusView Left,
    StimulusView Right,
    bool IsConsistencyProbe,
    DateTimeOffset PresentedAt);

public sealed record ComparisonSubmission(
    Guid SessionId,
    int Sequence,
    Guid LeftStimulusId,
    Guid RightStimulusId,
    PreferenceRating? Rating,
    TieReason TieReason,
    SkipReason SkipReason,
    long ResponseTimeMilliseconds);

public sealed record DimensionResult(
    DesignDimension Dimension,
    string Group,
    string NegativePole,
    string PositivePole,
    double Weight,
    double Confidence,
    string ConfidenceLevel);

public sealed record ProfileDimensionPresentation(
    DesignDimension Dimension,
    string Group,
    string NegativePole,
    string PositivePole);

public sealed record ProfilePresentationSnapshot(
    string SchemaVersion,
    string ParticipantName,
    string StudyName,
    IReadOnlyList<ProfileDimensionPresentation> Dimensions);

public sealed record AssessmentResult(
    Guid SessionId,
    string ParticipantName,
    string StudyName,
    int AnswerCount,
    double Coverage,
    double Consistency,
    int ConsistencyProbeCount,
    double LeftSelectionRate,
    bool HasPotentialSideBias,
    string MaturityLevel,
    string NarrativeMarkdown,
    IReadOnlyList<DimensionResult> Dimensions);

public sealed record RecentAssessment(
    Guid SessionId,
    string ParticipantName,
    AssessmentStatus Status,
    int CurrentSequence,
    int AnswerCount,
    int TargetComparisons,
    DateTimeOffset LastActivityAt);

public sealed record AssessmentExportStimulus(
    Guid Id,
    string AssetVersion,
    StimulusPreviewType PreviewType,
    StimulusPreviewSpec Preview,
    IReadOnlyDictionary<string, double> FeatureVector);

public sealed record AssessmentExportResponse(
    Guid Id,
    int Sequence,
    AssessmentExportStimulus Left,
    AssessmentExportStimulus Right,
    PreferenceRating? Rating,
    TieReason TieReason,
    SkipReason SkipReason,
    long ResponseTimeMilliseconds,
    bool IsConsistencyProbe,
    Guid? ProbeOfComparisonId,
    DateTimeOffset CreatedAt);

public sealed record AssessmentExport(
    string SchemaVersion,
    DateTimeOffset ExportedAt,
    Guid StudyId,
    string StudySlug,
    string ParticipantCode,
    string ModelVersion,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int TargetComparisons,
    AssessmentResult Profile,
    JsonElement DesignBrief,
    IReadOnlyList<AssessmentExportResponse> Responses);
