using System.ComponentModel.DataAnnotations;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class VppItemUpdateRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required, StringLength(64)]
    public string? VppCode { get; set; }

    [Required, StringLength(250)]
    public string? VppName { get; set; }

    public Guid UomId { get; set; }
    public Guid VppCategoryId { get; set; }

    [Range(VppOrderQuantityLimits.Minimum, VppOrderQuantityLimits.Maximum)]
    public int MaxQuantityPerOrder { get; set; } = VppOrderQuantityLimits.Default;

    [StringLength(500)]
    public string? Description { get; set; }
}
