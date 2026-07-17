using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace gtas_vpp_be.Model.Auth;

/// <summary>
/// Application-owned human identity. Business authorization remains in
/// permission groups and memberships; ASP.NET Core Identity owns credentials and account security.
/// </summary>
public sealed class AppUser : IdentityUser<int>
{
    [Required]
    [StringLength(250)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    public long MemberCompanyCode { get; set; } = 77500;

    public AppAccountStatus AccountStatus { get; set; } = AppAccountStatus.PendingApproval;

    public bool MustChangePassword { get; set; }

    public long SessionVersion { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }

    public DateTime? DisabledAtUtc { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
