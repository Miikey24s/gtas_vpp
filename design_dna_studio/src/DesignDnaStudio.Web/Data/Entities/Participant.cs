namespace DesignDnaStudio.Web.Data.Entities;

public sealed class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string AnonymousCode { get; set; } = Guid.NewGuid().ToString("N")[..12];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AssessmentSession> AssessmentSessions { get; set; } = new List<AssessmentSession>();

    public ICollection<DesignProfile> Profiles { get; set; } = new List<DesignProfile>();
}
