using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Web.Data.Entities;

public sealed class ComparisonResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssessmentSessionId { get; set; }

    public AssessmentSession AssessmentSession { get; set; } = null!;

    public int Sequence { get; set; }

    public Guid LeftStimulusId { get; set; }

    public Stimulus LeftStimulus { get; set; } = null!;

    public Guid RightStimulusId { get; set; }

    public Stimulus RightStimulus { get; set; } = null!;

    public PreferenceRating? Rating { get; set; }

    public TieReason TieReason { get; set; }

    public SkipReason SkipReason { get; set; }

    public long ResponseTimeMilliseconds { get; set; }

    public bool IsConsistencyProbe { get; set; }

    public Guid? ProbeOfComparisonId { get; set; }

    public ComparisonResponse? ProbeOfComparison { get; set; }

    public string ModelVersion { get; set; } = "ordinal-bt-v4-visual-contract";

    public string LeftAssetVersion { get; set; } = "4";

    public string RightAssetVersion { get; set; } = "4";

    public StimulusPreviewType LeftPreviewType { get; set; }

    public StimulusPreviewType RightPreviewType { get; set; }

    public string LeftFeatureVectorJson { get; set; } = "{}";

    public string RightFeatureVectorJson { get; set; } = "{}";

    public string LeftPreviewSpecJson { get; set; } = "{}";

    public string RightPreviewSpecJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
