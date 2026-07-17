using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

[Table("SettlementCharges")]
public sealed class SettlementCharge : BaseModel
{
    public Guid SettlementId { get; set; }
    public Settlement Settlement { get; set; } = default!;

    [Required, StringLength(32)]
    public string ChargeType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Required, StringLength(32)]
    public string AllocationBasis { get; set; } = "net-amount";
}
