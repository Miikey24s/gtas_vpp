using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class AdminPasswordResetLinkReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
