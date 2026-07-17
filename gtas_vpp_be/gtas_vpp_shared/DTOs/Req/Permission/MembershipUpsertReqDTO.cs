using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Permission;

/// <summary>
/// Administrative command for creating or replacing an account's single
/// active authorization membership. Audit fields are deliberately server-owned.
/// </summary>
public sealed class MembershipUpsertReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; init; }

    public Guid GroupId { get; init; }

    public Guid PrimaryDepartmentId { get; init; }

    /// <summary>
    /// Base64 row version returned by the server. It is omitted only when the
    /// account does not yet have an active membership.
    /// </summary>
    [StringLength(200)]
    public string? ExpectedRowVersion { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}
