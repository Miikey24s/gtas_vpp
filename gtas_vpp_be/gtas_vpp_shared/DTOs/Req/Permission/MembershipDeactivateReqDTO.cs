using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Permission;

/// <summary>
/// Administrative command that soft-deactivates an active membership. It does
/// not delete the account or any historical membership row.
/// </summary>
public sealed class MembershipDeactivateReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; init; }

    [Required, StringLength(200)]
    public string ExpectedRowVersion { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; init; }
}
