using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class VppItemCreateRequest
{
    [Required, StringLength(64)]
    public string? VppCode { get; set; }

    [Required, StringLength(250)]
    public string? VppName { get; set; }

    public Guid UomId { get; set; }
    public Guid VppCategoryId { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
