namespace DesignDnaStudio.Web.Data.Entities;

public sealed class AssessmentSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudyId { get; set; }

    public Study Study { get; set; } = null!;

    public Guid ParticipantId { get; set; }

    public Participant Participant { get; set; } = null!;

    public string ParticipantDisplayName { get; set; } = string.Empty;

    public AssessmentStatus Status { get; set; } = AssessmentStatus.NotStarted;

    public AssessmentPhase Phase { get; set; } = AssessmentPhase.Calibration;

    public int CurrentSequence { get; set; }

    public int TargetComparisons { get; set; } = 40;

    public string ModelVersion { get; set; } = "ordinal-bt-v4-visual-contract";

    public string ModelWeightsJson { get; set; } = "[]";

    public string ModelInformationJson { get; set; } = "[]";

    public int ModelObservationCount { get; set; }

    public double SideBias { get; set; }

    public double SideBiasInformation { get; set; }

    public double CoverageScore { get; set; }

    public double ConsistencyScore { get; set; } = 1d;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ComparisonResponse> Comparisons { get; set; } = new List<ComparisonResponse>();

}
