using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class AdminAccountInvitationReqDTO
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    public Guid GroupId { get; set; }

    public Guid PrimaryDepartmentId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
