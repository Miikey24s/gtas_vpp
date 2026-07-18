namespace DesignDnaStudio.Web.Data.Entities;

public sealed class Study
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public StudyStatus Status { get; set; } = StudyStatus.Draft;

    public bool IsBuiltIn { get; set; }

    public int MinimumComparisons { get; set; } = 30;

    public int MaximumComparisons { get; set; } = 60;

    public int ConsistencyProbeInterval { get; set; } = 10;

    public string ModelVersion { get; set; } = "ordinal-bt-v4-visual-contract";

    public string SettingsJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Stimulus> Stimuli { get; set; } = new List<Stimulus>();

    public ICollection<AssessmentSession> AssessmentSessions { get; set; } = new List<AssessmentSession>();
}
