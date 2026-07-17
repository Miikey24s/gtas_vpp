using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

[Table("VPP06_SettlementCharge")]
public sealed class VPP06_SettlementCharge : BaseModel
{
    public Guid SettlementId { get; set; }
    public VPP04_Settlement Settlement { get; set; } = default!;

    [Required, StringLength(32)]
    public string ChargeType { get; set; } = string.Empty;

    [Column(TypeName = "decimal(19,4)")]
    public decimal Amount { get; set; }

    [Required, StringLength(32)]
    public string AllocationBasis { get; set; } = "net-amount";
}
