using System.Text.Json;
using DesignDnaStudio.Engine;
using DesignDnaStudio.Web.Data;
using DesignDnaStudio.Web.Data.Entities;
using DesignDnaStudio.Web.Data.Seed;
using DesignDnaStudio.Web.Infrastructure;
using DesignDnaStudio.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DesignDnaStudio.Web.Services.Assessment;

public sealed class AssessmentService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    PreferenceLearner learner,
    AdaptivePairSelector pairSelector,
    IDesignProfileNarrativeService narrativeService,
    TimeProvider timeProvider)
{
    public async Task<StudySummary> GetBuiltInStudyAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Studies
            .Where(item => item.Slug == BuiltInStudyCatalog.StudySlug)
            .Select(item => new StudySummary(
                item.Id,
                item.Name,
                item.Description,
                item.Stimuli.Count(stimulus => stimulus.IsActive),
                item.MinimumComparisons,
                item.MaximumComparisons))
            .SingleAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentAssessment>> GetRecentAssessmentsAsync(
        int take = 8,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await context.AssessmentSessions
            .AsNoTracking()
            .Where(item => item.Status != AssessmentStatus.Abandoned)
            .Select(item => new RecentAssessment(
                item.Id,
                item.ParticipantDisplayName,
                item.Status,
                item.CurrentSequence,
                item.Comparisons.Count(response => response.Rating != null),
                item.TargetComparisons,
                item.LastActivityAt))
            .ToListAsync(cancellationToken);

        // SQLite can't translate ORDER BY for DateTimeOffset. This companion tool is
        // local-first and keeps a bounded session history, so ordering the projection
        // in memory preserves the instant and avoids provider-specific schema fields.
        return sessions
            .OrderByDescending(item => item.LastActivityAt)
            .Take(Math.Clamp(take, 1, 50))
            .ToArray();
    }

    public async Task<IReadOnlyList<ParticipantSummary>> GetParticipantsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Participants
            .AsNoTracking()
            .OrderBy(item => item.DisplayName)
            .Select(item => new ParticipantSummary(
                item.Id,
                item.DisplayName,
                item.Profiles.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssessmentStartResult> StartAsync(
        string displayName,
        int targetComparisons = 40,
        Guid? participantId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = string.IsNullOrWhiteSpace(displayName) ? "Nam" : displayName.Trim();
        if (normalizedName.Length > 120)
        {
            throw new ArgumentException("Tên người tham gia không được vượt quá 120 ký tự.", nameof(displayName));
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var study = await context.Studies.SingleAsync(
            item => item.Slug == BuiltInStudyCatalog.StudySlug && item.Status == StudyStatus.Active,
            cancellationToken);

        var participant = participantId.HasValue
            ? await context.Participants.SingleAsync(item => item.Id == participantId.Value, cancellationToken)
            : new Participant();
        participant.DisplayName = normalizedName;
        var session = new AssessmentSession
        {
            StudyId = study.Id,
            Participant = participant,
            ParticipantDisplayName = normalizedName,
            Status = AssessmentStatus.InProgress,
            Phase = AssessmentPhase.Calibration,
            CurrentSequence = 0,
            TargetComparisons = Math.Clamp(targetComparisons, study.MinimumComparisons, study.MaximumComparisons),
            ModelVersion = study.ModelVersion,
            ModelWeightsJson = JsonSerializer.Serialize(new double[DesignVector.DimensionCount], StudioJson.Options),
            ModelInformationJson = JsonSerializer.Serialize(new double[DesignVector.DimensionCount], StudioJson.Options),
            StartedAt = timeProvider.GetUtcNow(),
            LastActivityAt = timeProvider.GetUtcNow()
        };

        context.AssessmentSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        return new AssessmentStartResult(session.Id, participant.Id, participant.AnonymousCode, session.TargetComparisons);
    }

    public async Task<ComparisonPrompt?> GetPromptAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await BuildPromptAsync(context, sessionId, cancellationToken);
    }

    public async Task<bool> SubmitAsync(
        ComparisonSubmission submission,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var session = await context.AssessmentSessions
            .Include(item => item.Study)
            .SingleAsync(item => item.Id == submission.SessionId, cancellationToken);

        if (session.Status != AssessmentStatus.InProgress || session.CurrentSequence != submission.Sequence)
        {
            return false;
        }

        if ((submission.Rating is null && submission.SkipReason == SkipReason.None) ||
            (submission.Rating is not null && submission.SkipReason != SkipReason.None) ||
            (submission.Rating == PreferenceRating.Tie && submission.TieReason == TieReason.None) ||
            (submission.Rating != PreferenceRating.Tie && submission.TieReason != TieReason.None))
        {
            throw new ArgumentException("Phản hồi khảo sát không hợp lệ.", nameof(submission));
        }

        var expected = await BuildPromptAsync(context, submission.SessionId, cancellationToken);
        if (expected is null ||
            expected.Left.Id != submission.LeftStimulusId ||
            expected.Right.Id != submission.RightStimulusId)
        {
            return false;
        }

        var left = await context.Stimuli.SingleAsync(item => item.Id == submission.LeftStimulusId, cancellationToken);
        var right = await context.Stimuli.SingleAsync(item => item.Id == submission.RightStimulusId, cancellationToken);
        var state = ReadState(session);

        learner.Update(state, new PreferenceObservation(
            ReadVector(left),
            ReadVector(right),
            submission.Rating,
            submission.TieReason,
            submission.SkipReason));

        var response = new ComparisonResponse
        {
            AssessmentSessionId = session.Id,
            Sequence = session.CurrentSequence,
            LeftStimulusId = left.Id,
            RightStimulusId = right.Id,
            Rating = submission.Rating,
            TieReason = submission.TieReason,
            SkipReason = submission.SkipReason,
            ResponseTimeMilliseconds = Math.Clamp(submission.ResponseTimeMilliseconds, 0, 3_600_000),
            IsConsistencyProbe = expected.IsConsistencyProbe,
            ProbeOfComparisonId = expected.IsConsistencyProbe
                ? FindProbeSourceId(await GetHistoryAsync(context, session.Id, cancellationToken), left.Id, right.Id)
                : null,
            ModelVersion = session.ModelVersion,
            LeftAssetVersion = left.AssetVersion,
            RightAssetVersion = right.AssetVersion,
            LeftPreviewType = left.PreviewType,
            RightPreviewType = right.PreviewType,
            LeftFeatureVectorJson = left.FeatureVectorJson,
            RightFeatureVectorJson = right.FeatureVectorJson,
            LeftPreviewSpecJson = left.PreviewSpecJson,
            RightPreviewSpecJson = right.PreviewSpecJson,
            CreatedAt = timeProvider.GetUtcNow()
        };

        context.ComparisonResponses.Add(response);
        session.CurrentSequence++;
        session.Phase = state.ObservationCount < AdaptivePairSelector.CalibrationAnswerTarget &&
                        session.CurrentSequence < AdaptivePairSelector.CalibrationAnswerTarget
            ? AssessmentPhase.Calibration
            : AssessmentPhase.AdaptiveComparison;
        session.ModelWeightsJson = JsonSerializer.Serialize(state.Weights, StudioJson.Options);
        session.ModelInformationJson = JsonSerializer.Serialize(state.Information, StudioJson.Options);
        session.SideBias = state.SideBias;
        session.SideBiasInformation = state.SideBiasInformation;
        session.ModelObservationCount = state.ObservationCount;
        session.LastActivityAt = timeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);

        var history = await GetHistoryAsync(context, session.Id, cancellationToken);
        var consistency = ConsistencyAnalyzer.Analyze(history);
        var snapshot = ProfileSnapshotBuilder.Build(state, consistency);
        session.CoverageScore = snapshot.Coverage;
        session.ConsistencyScore = consistency.AgreementScore;

        if (snapshot.AnswerCount >= session.TargetComparisons)
        {
            await CompleteAsync(context, session, snapshot, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<AssessmentResult?> GetResultAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await context.AssessmentSessions
            .Include(item => item.Study)
            .SingleOrDefaultAsync(
                item => item.Id == sessionId && item.Status == AssessmentStatus.Completed,
                cancellationToken);

        if (session is null)
        {
            return null;
        }

        var profile = await context.DesignProfiles
            .Where(item => item.SourceAssessmentSessionId == session.Id)
            .OrderByDescending(item => item.Version)
            .FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var snapshot = JsonSerializer.Deserialize<ProfileSnapshot>(profile.RawProfileJson, StudioJson.Options);
        if (snapshot is null)
        {
            var history = await GetHistoryAsync(context, session.Id, cancellationToken);
            snapshot = ProfileSnapshotBuilder.Build(ReadState(session), ConsistencyAnalyzer.Analyze(history));
        }

        var presentation = JsonSerializer.Deserialize<ProfilePresentationSnapshot>(
            profile.PresentationJson,
            StudioJson.Options);
        if (presentation is null ||
            !string.Equals(presentation.SchemaVersion, "design-dna-presentation/v1", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(presentation.ParticipantName) ||
            string.IsNullOrWhiteSpace(presentation.StudyName) ||
            presentation.Dimensions is null)
        {
            presentation = null;
        }
        var presentationByDimension = presentation?.Dimensions
            .GroupBy(item => item.Dimension)
            .ToDictionary(group => group.Key, group => group.First());

        return new AssessmentResult(
            session.Id,
            presentation?.ParticipantName ?? session.ParticipantDisplayName,
            presentation?.StudyName ?? session.Study.Name,
            snapshot.AnswerCount,
            snapshot.Coverage,
            snapshot.Consistency.AgreementScore,
            snapshot.Consistency.ProbeCount,
            snapshot.Consistency.LeftSelectionRate,
            snapshot.Consistency.HasPotentialSideBias,
            snapshot.MaturityLevel,
            profile.NarrativeMarkdown,
            snapshot.Dimensions.Select(item =>
            {
                var definition = presentationByDimension?.GetValueOrDefault(item.Dimension);
                var catalogDefinition = DesignDimensionCatalog.Get(item.Dimension);
                return new DimensionResult(
                    item.Dimension,
                    definition?.Group ?? catalogDefinition.Group,
                    definition?.NegativePole ?? catalogDefinition.NegativePole,
                    definition?.PositivePole ?? catalogDefinition.PositivePole,
                    item.Weight,
                    item.Confidence,
                    item.ConfidenceLevel);
            }).ToArray());
    }

    public async Task<AssessmentExport?> CreateExportAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResultAsync(sessionId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await context.AssessmentSessions
            .AsNoTracking()
            .Include(item => item.Participant)
            .Include(item => item.Study)
            .SingleAsync(item => item.Id == sessionId && item.Status == AssessmentStatus.Completed, cancellationToken);
        var responseEvents = await context.ComparisonResponses
            .AsNoTracking()
            .Where(item => item.AssessmentSessionId == sessionId)
            .OrderBy(item => item.Sequence)
            .ToListAsync(cancellationToken);
        var storedProfile = await context.DesignProfiles
            .AsNoTracking()
            .SingleAsync(item => item.SourceAssessmentSessionId == sessionId, cancellationToken);
        var responses = responseEvents.Select(item => new AssessmentExportResponse(
                item.Id,
                item.Sequence,
                ToExportStimulus(
                    item.LeftStimulusId,
                    item.LeftAssetVersion,
                    item.LeftPreviewType,
                    item.LeftPreviewSpecJson,
                    item.LeftFeatureVectorJson),
                ToExportStimulus(
                    item.RightStimulusId,
                    item.RightAssetVersion,
                    item.RightPreviewType,
                    item.RightPreviewSpecJson,
                    item.RightFeatureVectorJson),
                item.Rating,
                item.TieReason,
                item.SkipReason,
                item.ResponseTimeMilliseconds,
                item.IsConsistencyProbe,
                item.ProbeOfComparisonId,
                item.CreatedAt))
            .ToArray();

        return new AssessmentExport(
            "design-dna-assessment/v2",
            timeProvider.GetUtcNow(),
            session.StudyId,
            session.Study.Slug,
            session.Participant.AnonymousCode,
            session.ModelVersion,
            session.StartedAt,
            session.CompletedAt!.Value,
            session.TargetComparisons,
            profile,
            JsonSerializer.Deserialize<JsonElement>(storedProfile.DesignBriefJson),
            responses);
    }

    private async Task<ComparisonPrompt?> BuildPromptAsync(
        ApplicationDbContext context,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var session = await context.AssessmentSessions
            .AsNoTracking()
            .Include(item => item.Study)
            .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);

        if (session is null || session.Status != AssessmentStatus.InProgress)
        {
            return null;
        }

        var stimuli = await context.Stimuli
            .AsNoTracking()
            .Where(item => item.StudyId == session.StudyId && item.IsActive && item.AccessibilityPass)
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        var history = await GetHistoryAsync(context, session.Id, cancellationToken);
        var answerCount = history.Count(item => item.Rating is not null);
        if (answerCount >= session.TargetComparisons)
        {
            return null;
        }

        var exposure = history
            .SelectMany(item => new[] { item.LeftStimulusId, item.RightStimulusId })
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        var candidates = stimuli.Select(item => new StimulusCandidate(
            item.Id,
            ReadVector(item),
            exposure.GetValueOrDefault(item.Id),
            item.ContentFamily,
            item.CalibrationPairKey)).ToArray();

        var selected = pairSelector.SelectNext(
            candidates,
            ReadState(session),
            history,
            session.CurrentSequence,
            session.Study.ConsistencyProbeInterval);
        if (selected is null)
        {
            return null;
        }

        var left = stimuli.Single(item => item.Id == selected.LeftStimulusId);
        var right = stimuli.Single(item => item.Id == selected.RightStimulusId);

        return new ComparisonPrompt(
            session.Id,
            session.CurrentSequence,
            answerCount,
            session.TargetComparisons,
            session.Phase,
            ToView(left),
            ToView(right),
            selected.IsConsistencyProbe,
            timeProvider.GetUtcNow());
    }

    private async Task CompleteAsync(
        ApplicationDbContext context,
        AssessmentSession session,
        ProfileSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        session.Status = AssessmentStatus.Completed;
        session.Phase = AssessmentPhase.Completed;
        session.CompletedAt = timeProvider.GetUtcNow();

        var nextVersion = await context.DesignProfiles
            .Where(item => item.ParticipantId == session.ParticipantId && item.StudyId == session.StudyId)
            .Select(item => (int?)item.Version)
            .MaxAsync(cancellationToken) ?? 0;
        var narrative = await narrativeService.CreateAsync(snapshot, cancellationToken);
        var rawProfile = JsonSerializer.Serialize(snapshot, StudioJson.Options);
        var presentation = JsonSerializer.Serialize(new ProfilePresentationSnapshot(
            "design-dna-presentation/v1",
            session.ParticipantDisplayName,
            session.Study.Name,
            Enum.GetValues<DesignDimension>()
                .Select(dimension =>
                {
                    var definition = DesignDimensionCatalog.Get(dimension);
                    return new ProfileDimensionPresentation(
                        dimension,
                        definition.Group,
                        definition.NegativePole,
                        definition.PositivePole);
                })
                .ToArray()), StudioJson.Options);
        var reliability = JsonSerializer.Serialize(new
        {
            snapshot.Coverage,
            snapshot.Consistency.AgreementScore,
            snapshot.Consistency.ProbeCount,
            snapshot.Consistency.LeftSelectionRate,
            snapshot.Consistency.HasPotentialSideBias,
            snapshot.MaturityLevel
        }, StudioJson.Options);
        var designBrief = JsonSerializer.Serialize(new
        {
            schemaVersion = "design-dna-brief/v1",
            maturity = snapshot.MaturityLevel,
            directions = snapshot.Strongest.Take(6).Select(item =>
            {
                var definition = DesignDimensionCatalog.Get(item.Dimension);
                return new
                {
                    dimension = item.Dimension.ToString(),
                    preferredPole = item.Weight >= 0d ? definition.PositivePole : definition.NegativePole,
                    strength = Math.Abs(item.Weight),
                    item.Confidence,
                    item.ConfidenceLevel
                };
            }),
            guardrails = new[]
            {
                "Accessibility và khả năng hoàn thành nghiệp vụ luôn ưu tiên hơn sở thích cá nhân.",
                "Mọi art direction phải được kiểm chứng lại trên màn hình GTAS thật.",
                "Không suy rộng hồ sơ cá nhân thành đại diện cho toàn bộ người dùng PPJ."
            }
        }, StudioJson.Options);

        context.DesignProfiles.Add(new DesignProfile
        {
            ParticipantId = session.ParticipantId,
            StudyId = session.StudyId,
            SourceAssessmentSessionId = session.Id,
            Version = nextVersion + 1,
            ModelVersion = session.ModelVersion,
            RawProfileJson = rawProfile,
            ReliabilityJson = reliability,
            NarrativeMarkdown = narrative,
            DesignBriefJson = designBrief,
            PresentationJson = presentation,
            CreatedAt = timeProvider.GetUtcNow()
        });
    }

    private static StimulusView ToView(Stimulus stimulus) => new(
        stimulus.Id,
        stimulus.PreviewType,
        JsonSerializer.Deserialize<StimulusPreviewSpec>(stimulus.PreviewSpecJson, StudioJson.Options)
            ?? throw new InvalidOperationException($"Stimulus {stimulus.Id} has an invalid preview specification."));

    private static AssessmentExportStimulus ToExportStimulus(
        Guid id,
        string assetVersion,
        StimulusPreviewType previewType,
        string previewSpecJson,
        string featureVectorJson) => new(
            id,
            assetVersion,
            previewType,
            JsonSerializer.Deserialize<StimulusPreviewSpec>(previewSpecJson, StudioJson.Options)
                ?? throw new InvalidOperationException($"Stimulus {id} has an invalid persisted preview specification."),
            JsonSerializer.Deserialize<Dictionary<string, double>>(featureVectorJson, StudioJson.Options)
                ?? throw new InvalidOperationException($"Stimulus {id} has an invalid persisted feature vector."));

    private static DesignVector ReadVector(Stimulus stimulus)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, double>>(stimulus.FeatureVectorJson, StudioJson.Options)
            ?? new Dictionary<string, double>();
        return DesignVector.FromDictionary(values);
    }

    private static PreferenceModelState ReadState(AssessmentSession session)
    {
        var weights = JsonSerializer.Deserialize<double[]>(session.ModelWeightsJson, StudioJson.Options)
            ?? new double[DesignVector.DimensionCount];
        var information = JsonSerializer.Deserialize<double[]>(session.ModelInformationJson, StudioJson.Options)
            ?? new double[DesignVector.DimensionCount];
        return new PreferenceModelState(
            weights,
            information,
            session.ModelObservationCount,
            session.SideBias,
            session.SideBiasInformation);
    }

    private static async Task<List<ComparisonHistory>> GetHistoryAsync(
        ApplicationDbContext context,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        await context.ComparisonResponses
            .AsNoTracking()
            .Where(item => item.AssessmentSessionId == sessionId)
            .OrderBy(item => item.Sequence)
            .Select(item => new ComparisonHistory(
                item.Id,
                item.LeftStimulusId,
                item.RightStimulusId,
                item.Rating,
                item.IsConsistencyProbe,
                item.ProbeOfComparisonId))
            .ToListAsync(cancellationToken);

    private static Guid? FindProbeSourceId(
        IReadOnlyList<ComparisonHistory> history,
        Guid leftStimulusId,
        Guid rightStimulusId) =>
        history
            .Where(item => !item.IsConsistencyProbe && item.ProbeOfComparisonId is null)
            .Where(item => item.LeftStimulusId == rightStimulusId && item.RightStimulusId == leftStimulusId)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefault();
}
