using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

[Table("SettlementAllocations")]
public sealed class SettlementAllocation : BaseModel
{
    public Guid SettlementId { get; set; }
    public Settlement Settlement { get; set; } = default!;
    public Guid SettlementItemId { get; set; }
    public SettlementItem SettlementItem { get; set; } = default!;
    public Guid RequestHeaderId { get; set; }
    public Guid RequestDetailId { get; set; }

    [StringLength(64)]
    public string? DepartmentCode { get; set; }

    public int RequesterUserId { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal NetAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal CommercialAdjustmentAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal RoundingAdjustment { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal GrossAmount { get; set; }
}
