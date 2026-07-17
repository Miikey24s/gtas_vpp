using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Auth;

/// <summary>
/// Append-only audit record for account, membership, session and permission
/// administration. It intentionally excludes credentials and token material.
/// </summary>
[Table("SecurityAudits")]
public sealed class SecurityAudit
{
    [Key]
    public Guid Id { get; set; }

    public int? ActorUserId { get; set; }

    public int? TargetUserId { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ResourceType { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ResourceId { get; set; }

    [Required, StringLength(40)]
    public string Outcome { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Summary { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(100)]
    public string? CorrelationId { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
