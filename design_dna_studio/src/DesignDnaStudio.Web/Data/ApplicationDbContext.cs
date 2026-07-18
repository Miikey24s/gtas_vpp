using Microsoft.EntityFrameworkCore;
using DesignDnaStudio.Web.Data.Entities;

namespace DesignDnaStudio.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Study> Studies => Set<Study>();

    public DbSet<Stimulus> Stimuli => Set<Stimulus>();

    public DbSet<Participant> Participants => Set<Participant>();

    public DbSet<AssessmentSession> AssessmentSessions => Set<AssessmentSession>();

    public DbSet<ComparisonResponse> ComparisonResponses => Set<ComparisonResponse>();

    public DbSet<DesignProfile> DesignProfiles => Set<DesignProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Study>(entity =>
        {
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.Slug).HasMaxLength(120);
            entity.Property(item => item.ModelVersion).HasMaxLength(64);
        });

        builder.Entity<Stimulus>(entity =>
        {
            entity.HasIndex(item => new { item.StudyId, item.SortOrder });
            entity.HasIndex(item => new { item.StudyId, item.ContentFamily, item.CalibrationPairKey });
            entity.Property(item => item.Title).HasMaxLength(160);
            entity.Property(item => item.ContentFamily).HasMaxLength(80);
            entity.Property(item => item.CalibrationPairKey).HasMaxLength(80);
            entity.Property(item => item.AssetVersion).HasMaxLength(40);
        });

        builder.Entity<Participant>(entity =>
        {
            entity.HasIndex(item => item.AnonymousCode).IsUnique();
            entity.Property(item => item.DisplayName).HasMaxLength(120);
            entity.Property(item => item.AnonymousCode).HasMaxLength(40);
        });

        builder.Entity<AssessmentSession>(entity =>
        {
            entity.HasIndex(item => new { item.ParticipantId, item.StudyId, item.Status });
            entity.Property(item => item.ParticipantDisplayName).HasMaxLength(120);
            entity.Property(item => item.ModelVersion).HasMaxLength(64);
        });

        builder.Entity<ComparisonResponse>(entity =>
        {
            entity.HasIndex(item => new { item.AssessmentSessionId, item.Sequence }).IsUnique();
            entity.HasOne(item => item.ProbeOfComparison)
                .WithMany()
                .HasForeignKey(item => item.ProbeOfComparisonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<DesignProfile>(entity =>
        {
            entity.HasIndex(item => new { item.ParticipantId, item.StudyId, item.Version }).IsUnique();
            entity.Property(item => item.ModelVersion).HasMaxLength(64);
        });
    }
}
