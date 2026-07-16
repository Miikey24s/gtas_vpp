using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class PasswordChangeReqDTO
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
