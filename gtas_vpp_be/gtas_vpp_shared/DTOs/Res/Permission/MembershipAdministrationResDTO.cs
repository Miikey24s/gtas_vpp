namespace gtas_vpp_shared.DTOs.Res.Permission;

/// <summary>
/// Authoritative membership state returned after an administrative command.
/// </summary>
public sealed class MembershipAdministrationResDTO
{
    public Guid MembershipId { get; init; }
    public int AccountId { get; init; }
    public string UserLogin { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AccountStatus { get; init; } = string.Empty;
    public long SessionVersion { get; init; }
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public Guid PrimaryDepartmentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RowVersion { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
