using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class PasswordRecoveryReqDTO
{
    [Required, StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
