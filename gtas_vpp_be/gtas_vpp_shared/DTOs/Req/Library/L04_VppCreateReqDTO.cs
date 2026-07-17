using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class L04_VppCreateReqDTO
{
    [Required, StringLength(64)]
    public string? VPPCode { get; set; }

    [Required, StringLength(250)]
    public string? VPPName { get; set; }

    public Guid UOMId { get; set; }
    public Guid VPPCategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
