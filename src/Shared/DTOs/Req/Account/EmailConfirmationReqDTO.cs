using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class EmailConfirmationReqDTO
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;
}
