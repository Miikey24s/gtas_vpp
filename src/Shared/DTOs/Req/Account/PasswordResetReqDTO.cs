using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class PasswordResetReqDTO
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
