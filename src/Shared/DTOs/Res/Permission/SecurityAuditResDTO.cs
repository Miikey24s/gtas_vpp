namespace gtas_vpp_shared.DTOs.Res.Permission;

public sealed class SecurityAuditResDTO
{
    public Guid Id { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public int? ActorUserId { get; init; }
    public string? ActorUserName { get; init; }
    public string? ActorFullName { get; init; }
    public int? TargetUserId { get; init; }
    public string? TargetUserName { get; init; }
    public string? TargetFullName { get; init; }
    public string Action { get; init; } = string.Empty;
    public string ResourceType { get; init; } = string.Empty;
    public string? ResourceId { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? Reason { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed class SecurityAuditFilterOptionsResDTO
{
    public List<string> Actions { get; init; } = [];
    public List<string> Outcomes { get; init; } = [];
}
