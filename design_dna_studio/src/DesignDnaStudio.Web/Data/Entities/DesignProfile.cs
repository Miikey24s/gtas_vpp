namespace DesignDnaStudio.Web.Data.Entities;

public sealed class DesignProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParticipantId { get; set; }

    public Participant Participant { get; set; } = null!;

    public Guid StudyId { get; set; }

    public Study Study { get; set; } = null!;

    public Guid SourceAssessmentSessionId { get; set; }

    public AssessmentSession SourceAssessmentSession { get; set; } = null!;

    public int Version { get; set; } = 1;

    public string ModelVersion { get; set; } = "ordinal-bt-v4-visual-contract";

    public string RawProfileJson { get; set; } = "{}";

    public string ReliabilityJson { get; set; } = "{}";

    public string NarrativeMarkdown { get; set; } = string.Empty;

    public string DesignBriefJson { get; set; } = "{}";

    public string PresentationJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
