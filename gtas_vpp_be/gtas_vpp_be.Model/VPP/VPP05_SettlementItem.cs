using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

[Table("VPP05_SettlementItem")]
public sealed class VPP05_SettlementItem : BaseModel
{
    public Guid SettlementId { get; set; }
    public VPP04_Settlement Settlement { get; set; } = default!;
    public Guid VppId { get; set; }

    [Required, StringLength(64)]
    public string VppCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string VppName { get; set; } = string.Empty;

    public Guid UomId { get; set; }

    [Required, StringLength(64)]
    public string UomCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string UomName { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Guid PriceListId { get; set; }
    public Guid PriceBookItemId { get; set; }

    [StringLength(128)]
    public string? SupplierSku { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal NetUnitPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal NetAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal GrossAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal MinimumOrderQuantity { get; set; }

    public int LeadTimeDays { get; set; }
    public bool IsSupplierException { get; set; }

    [StringLength(500)]
    public string? SupplierExceptionReason { get; set; }
}
