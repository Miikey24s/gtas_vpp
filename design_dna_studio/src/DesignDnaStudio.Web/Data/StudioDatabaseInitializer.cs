using DesignDnaStudio.Web.Data.Seed;
using DesignDnaStudio.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DesignDnaStudio.Web.Data;

public sealed class StudioDatabaseInitializer(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<StudioDatabaseInitializer> logger,
    TimeProvider timeProvider)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        var existing = await context.Studies
            .Include(item => item.Stimuli)
            .SingleOrDefaultAsync(item => item.Slug == BuiltInStudyCatalog.StudySlug, cancellationToken);

        if (existing is null)
        {
            context.Studies.Add(BuiltInStudyCatalog.Create());
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created the built-in GTAS VPP Personal Design DNA study.");
            return;
        }

        var catalog = BuiltInStudyCatalog.Create();
        var modelChanged = !string.Equals(existing.ModelVersion, catalog.ModelVersion, StringComparison.Ordinal);
        SyncStudy(existing, catalog);

        var existingById = existing.Stimuli.ToDictionary(item => item.Id);
        var catalogIds = catalog.Stimuli.Select(item => item.Id).ToHashSet();
        foreach (var source in catalog.Stimuli)
        {
            if (existingById.TryGetValue(source.Id, out var target))
            {
                SyncStimulus(target, source);
            }
            else
            {
                existing.Stimuli.Add(source);
            }
        }

        foreach (var obsolete in existing.Stimuli.Where(item =>
                     item.SourceReference == "Built-in generated stimulus" && !catalogIds.Contains(item.Id)))
        {
            obsolete.IsActive = false;
        }

        if (modelChanged)
        {
            var staleSessions = await context.AssessmentSessions
                .Where(item => item.StudyId == existing.Id &&
                               item.Status == AssessmentStatus.InProgress &&
                               item.ModelVersion != catalog.ModelVersion)
                .ToListAsync(cancellationToken);
            foreach (var session in staleSessions)
            {
                session.Status = AssessmentStatus.Abandoned;
                session.LastActivityAt = timeProvider.GetUtcNow();
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            existing.UpdatedAt = timeProvider.GetUtcNow();
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Reconciled the built-in DesignDNA study and {StimulusCount} stimuli.", catalog.Stimuli.Count);
        }
    }

    private static void SyncStudy(Study target, Study source)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Status = source.Status;
        target.IsBuiltIn = source.IsBuiltIn;
        target.MinimumComparisons = source.MinimumComparisons;
        target.MaximumComparisons = source.MaximumComparisons;
        target.ConsistencyProbeInterval = source.ConsistencyProbeInterval;
        target.ModelVersion = source.ModelVersion;
        target.SettingsJson = source.SettingsJson;
    }

    private static void SyncStimulus(Stimulus target, Stimulus source)
    {
        target.Title = source.Title;
        target.Description = source.Description;
        target.Kind = source.Kind;
        target.PreviewType = source.PreviewType;
        target.ContentFamily = source.ContentFamily;
        target.CalibrationPairKey = source.CalibrationPairKey;
        target.FeatureVectorJson = source.FeatureVectorJson;
        target.PreviewSpecJson = source.PreviewSpecJson;
        target.SourceReference = source.SourceReference;
        target.AssetVersion = source.AssetVersion;
        target.AccessibilityPass = source.AccessibilityPass;
        target.IsActive = source.IsActive;
        target.SortOrder = source.SortOrder;
    }
}
