using DesignDnaStudio.Engine;
using DesignDnaStudio.Web.Data;
using DesignDnaStudio.Web.Data.Entities;
using DesignDnaStudio.Web.Data.Seed;
using DesignDnaStudio.Web.Services.Assessment;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesignDnaStudio.Tests;

public sealed class AssessmentServiceIntegrationTests
{
    [Fact]
    public async Task Skips_DoNotCompleteOrTrainTheAssessment()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Skip Tester", 30);

        for (var index = 0; index < 30; index++)
        {
            var prompt = Require(await harness.Service.GetPromptAsync(started.SessionId));
            Assert.True(await harness.Service.SubmitAsync(Submission(prompt, null, SkipReason.CannotEvaluate)));
        }

        var nextPrompt = Require(await harness.Service.GetPromptAsync(started.SessionId));
        Assert.Equal(0, nextPrompt.AnswerCount);
        Assert.Null(await harness.Service.GetResultAsync(started.SessionId));

        await using var context = await harness.Factory.CreateDbContextAsync();
        var session = await context.AssessmentSessions.SingleAsync(item => item.Id == started.SessionId);
        Assert.Equal(AssessmentStatus.InProgress, session.Status);
        Assert.Equal(30, session.CurrentSequence);
        Assert.Equal(0, session.ModelObservationCount);
        Assert.Equal(AssessmentPhase.AdaptiveComparison, session.Phase);
        Assert.False(await context.ComparisonResponses.AnyAsync(item =>
            item.AssessmentSessionId == started.SessionId && item.IsConsistencyProbe));
    }

    [Fact]
    public async Task AllTies_CompleteWithoutInventingDirectionalPoles()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Tie Tester", 30);

        for (var index = 0; index < 30; index++)
        {
            var prompt = Require(await harness.Service.GetPromptAsync(started.SessionId));
            Assert.True(await harness.Service.SubmitAsync(Submission(
                prompt,
                PreferenceRating.Tie,
                tieReason: TieReason.NoVisibleDifference)));
        }

        var result = Require(await harness.Service.GetResultAsync(started.SessionId));
        Assert.All(result.Dimensions, item => Assert.InRange(Math.Abs(item.Weight), 0d, 0.000001d));
        Assert.Equal("exploring", result.MaturityLevel);
        var export = Require(await harness.Service.CreateExportAsync(started.SessionId));
        Assert.Equal("exploring", export.DesignBrief.GetProperty("maturity").GetString());
        Assert.Empty(export.DesignBrief.GetProperty("directions").EnumerateArray());
        Assert.Contains("giai đoạn khám phá", result.NarrativeMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompletedAssessment_CreatesStableSnapshotAndEvidenceExport()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Completion Tester", 30);

        await AnswerUntilCompletedAsync(harness.Service, started.SessionId, 30);

        Assert.Null(await harness.Service.GetPromptAsync(started.SessionId));
        var result = Require(await harness.Service.GetResultAsync(started.SessionId));
        Assert.Equal(30, result.AnswerCount);
        Assert.Equal(28, result.Dimensions.Count);

        var export = Require(await harness.Service.CreateExportAsync(started.SessionId));
        Assert.Equal("design-dna-assessment/v2", export.SchemaVersion);
        Assert.Equal(started.SessionId, export.Profile.SessionId);
        Assert.Equal(30, export.TargetComparisons);
        Assert.Equal(BuiltInStudyCatalog.StudySlug, export.StudySlug);
        Assert.Equal(30, export.Responses.Count(item => item.Rating is not null));
        Assert.Equal(2, export.Responses.Count(item => item.IsConsistencyProbe));
        Assert.All(export.Responses, item =>
        {
            Assert.Equal("4", item.Left.AssetVersion);
            Assert.Equal(DesignVector.DimensionCount, item.Left.FeatureVector.Count);
            Assert.False(string.IsNullOrWhiteSpace(item.Left.Preview.Context));
        });
        var responseById = export.Responses.ToDictionary(item => item.Id);
        Assert.All(export.Responses.Where(item => item.ProbeOfComparisonId.HasValue), item =>
        {
            Assert.True(responseById.TryGetValue(item.ProbeOfComparisonId!.Value, out var source));
            Assert.True(source!.Sequence < item.Sequence);
        });
        Assert.Equal("design-dna-brief/v1", export.DesignBrief.GetProperty("schemaVersion").GetString());

        await using var context = await harness.Factory.CreateDbContextAsync();
        Assert.Equal(AssessmentStatus.Completed,
            await context.AssessmentSessions.Where(item => item.Id == started.SessionId).Select(item => item.Status).SingleAsync());
        var storedProfile = await context.DesignProfiles.SingleAsync(item => item.SourceAssessmentSessionId == started.SessionId);
        Assert.Contains("design-dna-brief/v1", storedProfile.DesignBriefJson, StringComparison.Ordinal);
        Assert.Contains("design-dna-presentation/v1", storedProfile.PresentationJson, StringComparison.Ordinal);

        var session = await context.AssessmentSessions.SingleAsync(item => item.Id == started.SessionId);
        session.ModelWeightsJson = JsonSerializer.Serialize(Enumerable.Repeat(-1d, DesignVector.DimensionCount));
        session.ModelInformationJson = JsonSerializer.Serialize(new double[DesignVector.DimensionCount]);
        session.ModelObservationCount = 0;
        var study = await context.Studies.SingleAsync(item => item.Id == session.StudyId);
        study.Name = "Tên study đã thay đổi";
        await context.SaveChangesAsync();

        var persistedResult = Require(await harness.Service.GetResultAsync(started.SessionId));
        Assert.Equal(result.StudyName, persistedResult.StudyName);
        Assert.Equal(
            result.Dimensions.Select(item => item.Weight),
            persistedResult.Dimensions.Select(item => item.Weight));
    }

    [Fact]
    public async Task MaximumAssessment_HasEnoughComparablePairs()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Sixty Tester", 60);

        await AnswerUntilCompletedAsync(harness.Service, started.SessionId, 60);

        var result = Require(await harness.Service.GetResultAsync(started.SessionId));
        Assert.Equal(60, result.AnswerCount);
    }

    [Fact]
    public async Task DuplicateSubmission_IsRejectedWithoutDuplicateEvent()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Duplicate Tester", 30);
        var prompt = Require(await harness.Service.GetPromptAsync(started.SessionId));
        var submission = Submission(prompt, PreferenceRating.SlightLeft);

        Assert.True(await harness.Service.SubmitAsync(submission));
        Assert.False(await harness.Service.SubmitAsync(submission));

        await using var context = await harness.Factory.CreateDbContextAsync();
        Assert.Equal(1, await context.ComparisonResponses.CountAsync(item => item.AssessmentSessionId == started.SessionId));
    }

    [Fact]
    public async Task TieWithoutReason_IsRejected()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Tie Validation Tester", 30);
        var prompt = Require(await harness.Service.GetPromptAsync(started.SessionId));

        await Assert.ThrowsAsync<ArgumentException>(() => harness.Service.SubmitAsync(
            Submission(prompt, PreferenceRating.Tie)));
    }

    [Fact]
    public async Task RecreatedService_ResumesWithPersistedAnswerCount()
    {
        await using var harness = await TestHarness.CreateAsync();
        var started = await harness.Service.StartAsync("Resume Tester", 30);

        for (var index = 0; index < 3; index++)
        {
            var prompt = Require(await harness.Service.GetPromptAsync(started.SessionId));
            Assert.True(await harness.Service.SubmitAsync(Submission(prompt, PreferenceRating.SlightRight)));
        }

        var recreated = TestHarness.CreateService(harness.Factory);
        var resumed = Require(await recreated.GetPromptAsync(started.SessionId));
        Assert.Equal(3, resumed.Sequence);
        Assert.Equal(3, resumed.AnswerCount);
    }

    [Fact]
    public async Task SameDisplayName_ReusesParticipantAndIncrementsProfileVersion()
    {
        await using var harness = await TestHarness.CreateAsync();
        var first = await harness.Service.StartAsync("Longitudinal Tester", 30);
        await AnswerUntilCompletedAsync(harness.Service, first.SessionId, 30);
        var second = await harness.Service.StartAsync("longitudinal tester", 30, first.ParticipantId);
        await AnswerUntilCompletedAsync(harness.Service, second.SessionId, 30);

        Assert.Equal(first.ParticipantId, second.ParticipantId);
        await using var context = await harness.Factory.CreateDbContextAsync();
        var versions = await context.DesignProfiles
            .Where(item => item.ParticipantId == first.ParticipantId)
            .OrderBy(item => item.Version)
            .Select(item => item.Version)
            .ToListAsync();
        Assert.Equal(new[] { 1, 2 }, versions);
    }

    [Fact]
    public async Task SameDisplayName_WithoutExplicitProfileSelectionCreatesDifferentParticipant()
    {
        await using var harness = await TestHarness.CreateAsync();

        var first = await harness.Service.StartAsync("Trùng tên", 30);
        var second = await harness.Service.StartAsync("Trùng tên", 30);

        Assert.NotEqual(first.ParticipantId, second.ParticipantId);
    }

    [Fact]
    public async Task RenamingSelectedParticipant_DoesNotRewriteCompletedSessionEvidence()
    {
        await using var harness = await TestHarness.CreateAsync();
        var first = await harness.Service.StartAsync("Tên lúc khảo sát", 30);
        await AnswerUntilCompletedAsync(harness.Service, first.SessionId, 30);

        await harness.Service.StartAsync("Tên hồ sơ mới", 30, first.ParticipantId);

        var originalResult = Require(await harness.Service.GetResultAsync(first.SessionId));
        var originalExport = Require(await harness.Service.CreateExportAsync(first.SessionId));
        Assert.Equal("Tên lúc khảo sát", originalResult.ParticipantName);
        Assert.Equal("Tên lúc khảo sát", originalExport.Profile.ParticipantName);
    }

    [Fact]
    public async Task Initializer_ReconcilesExistingCatalog()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"design-dna-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;
        var factory = new TestDbContextFactory(options);
        var initializer = new StudioDatabaseInitializer(
            factory,
            NullLogger<StudioDatabaseInitializer>.Instance,
            TimeProvider.System);

        try
        {
            await initializer.InitializeAsync();
            await using (var context = await factory.CreateDbContextAsync())
            {
                var study = await context.Studies.Include(item => item.Stimuli).SingleAsync();
                study.MaximumComparisons = 31;
                study.Stimuli.First().ContentFamily = "legacy:isolated";
                await context.SaveChangesAsync();
            }

            await initializer.InitializeAsync();

            await using var verified = await factory.CreateDbContextAsync();
            var reconciled = await verified.Studies.Include(item => item.Stimuli).SingleAsync();
            Assert.Equal(60, reconciled.MaximumComparisons);
            Assert.All(reconciled.Stimuli, item => Assert.StartsWith("context:", item.ContentFamily));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    private static async Task AnswerUntilCompletedAsync(AssessmentService service, Guid sessionId, int expectedAnswers)
    {
        for (var index = 0; index < expectedAnswers; index++)
        {
            var prompt = Require(await service.GetPromptAsync(sessionId));
            var rating = index % 2 == 0 ? PreferenceRating.SlightLeft : PreferenceRating.SlightRight;
            Assert.True(await service.SubmitAsync(Submission(prompt, rating)));
        }
    }

    private static ComparisonSubmission Submission(
        ComparisonPrompt prompt,
        PreferenceRating? rating,
        SkipReason skipReason = SkipReason.None,
        TieReason tieReason = TieReason.None) => new(
            prompt.SessionId,
            prompt.Sequence,
            prompt.Left.Id,
            prompt.Right.Id,
            rating,
            tieReason,
            skipReason,
            750);

    private static T Require<T>(T? value) where T : class
    {
        Assert.NotNull(value);
        return value!;
    }

    private sealed class TestHarness(SqliteConnection connection, TestDbContextFactory factory) : IAsyncDisposable
    {
        public TestDbContextFactory Factory { get; } = factory;

        public AssessmentService Service { get; } = CreateService(factory);

        public static async Task<TestHarness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var factory = new TestDbContextFactory(options);

            await using var context = await factory.CreateDbContextAsync();
            await context.Database.EnsureCreatedAsync();
            context.Studies.Add(BuiltInStudyCatalog.Create());
            await context.SaveChangesAsync();

            return new TestHarness(connection, factory);
        }

        public static AssessmentService CreateService(IDbContextFactory<ApplicationDbContext> factory)
        {
            var learner = new PreferenceLearner();
            return new AssessmentService(
                factory,
                learner,
                new AdaptivePairSelector(learner),
                new DeterministicDesignProfileNarrativeService(),
                TimeProvider.System);
        }

        public async ValueTask DisposeAsync() => await connection.DisposeAsync();
    }

    public sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
