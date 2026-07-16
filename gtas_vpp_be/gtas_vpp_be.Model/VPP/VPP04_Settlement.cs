using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

[Table("VPP04_Settlement")]
public sealed class VPP04_Settlement : BaseModel
{
    public Guid PeriodId { get; set; }
    public VPP00_Period Period { get; set; } = default!;

    [Required, StringLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    public int Y { get; set; }
    public int M { get; set; }
    public int RevisionNumber { get; set; } = 1;
    public bool IsCurrentRevision { get; set; } = true;
    public bool IsCorrection { get; set; }
    public Guid? SupersedesSettlementId { get; set; }
    public VPP04_Settlement? SupersedesSettlement { get; set; }
    public Guid? SupersededBySettlementId { get; set; }

    [StringLength(500)]
    public string? CorrectionReason { get; set; }

    public Guid PrimarySupplierId { get; set; }

    [Required, StringLength(250)]
    public string PrimarySupplierName { get; set; } = string.Empty;

    public Guid PriceListId { get; set; }

    [StringLength(50)]
    public string? PriceListCode { get; set; }

    [Required, StringLength(200)]
    public string PriceListName { get; set; } = string.Empty;

    public int PriceListVersion { get; set; }
    public DateTime PriceAsOfUtc { get; set; }

    [Required, StringLength(32)]
    public string CalculationVersion { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string InputHash { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string CommandPayloadHash { get; set; } = string.Empty;

    [Required, StringLength(3)]
    public string CurrencyCode { get; set; } = "VND";

    [Column(TypeName = "decimal(19,4)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal RebateAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal FeeAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal ShippingAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal VatAmount { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal RoundingAdjustment { get; set; }

    [Column(TypeName = "decimal(19,4)")]
    public decimal GrandTotal { get; set; }

    public DateTime ConfirmedAtUtc { get; set; }
    public int ConfirmedByUserId { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<VPP05_SettlementItem> Items { get; set; } = [];
    public ICollection<VPP06_SettlementCharge> Charges { get; set; } = [];
    public ICollection<VPP07_SettlementAllocation> Allocations { get; set; } = [];
}
