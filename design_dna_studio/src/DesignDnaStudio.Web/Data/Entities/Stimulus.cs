namespace DesignDnaStudio.Web.Data.Entities;

public sealed class Stimulus
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudyId { get; set; }

    public Study Study { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StimulusKind Kind { get; set; }

    public StimulusPreviewType PreviewType { get; set; }

    public string ContentFamily { get; set; } = "dashboard";

    public string? CalibrationPairKey { get; set; }

    public string FeatureVectorJson { get; set; } = "{}";

    public string PreviewSpecJson { get; set; } = "{}";

    public string SourceReference { get; set; } = string.Empty;

    public string AssetVersion { get; set; } = "4";

    public bool AccessibilityPass { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
