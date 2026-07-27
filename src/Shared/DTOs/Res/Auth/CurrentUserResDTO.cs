namespace gtas_vpp_shared.DTOs.Res.Auth;

public sealed class CurrentUserResDTO
{
    public int UserId { get; set; }
    public string UserLogin { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public long SessionVersion { get; set; }
    public Guid GroupId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public long MemberCompanyCode { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public string PrimaryDepartmentCode { get; set; } = string.Empty;
    public string PrimaryDepartmentName { get; set; } = string.Empty;
}
