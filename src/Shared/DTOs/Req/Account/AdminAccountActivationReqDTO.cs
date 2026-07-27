using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Account;

public sealed class AdminAccountActivationReqDTO
{
    [Range(1, int.MaxValue)]
    public int AccountId { get; set; }

    public Guid GroupId { get; set; }

    public Guid PrimaryDepartmentId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
