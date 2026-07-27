using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class AccountRegistrationReqDTO
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? EmployeeCode { get; set; }

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), StringLength(128, MinimumLength = 10)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
