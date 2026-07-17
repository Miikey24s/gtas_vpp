using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class AdminPasswordResetReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [Required, StringLength(128, MinimumLength = 10)]
    public string TemporaryPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(TemporaryPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; set; }
}
