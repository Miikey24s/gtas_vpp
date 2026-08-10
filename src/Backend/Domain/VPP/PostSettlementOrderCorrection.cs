using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Helpers;

namespace gtas_vpp_be.Model.VPP;

public enum PostSettlementOrderCorrectionAction
{
    Adjust = 0,
    Cancel = 1
}

public enum PostSettlementOrderCorrectionStatus
{
    Pending = 0,
    Confirmed = 1,
    Rejected = 2,
    Superseded = 3
}

[Table("PostSettlementOrderCorrections")]
public sealed class PostSettlementOrderCorrection : BaseModel
{
    public Guid PeriodId { get; set; }
    public VppPeriod Period { get; set; } = default!;
    public Guid RequestId { get; set; }
    public VppRequest Request { get; set; } = default!;
    public Guid RequestSeriesId { get; set; }
    public int RequestRevisionNumber { get; set; }
    public Guid SettlementId { get; set; }
    public Settlement Settlement { get; set; } = default!;

    [Required, StringLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    public PostSettlementOrderCorrectionAction Action { get; set; }
    public PostSettlementOrderCorrectionStatus Status { get; set; }

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string EmployeeNote { get; set; } = string.Empty;

    [StringLength(500)]
    public string? DecisionReason { get; set; }

    public int RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? ResultRequestId { get; set; }
    public Guid? ResultSettlementId { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<PostSettlementOrderCorrectionItem> Items { get; set; } = [];
}

[Table("PostSettlementOrderCorrectionItems")]
public sealed class PostSettlementOrderCorrectionItem : BaseModel
{
    public Guid CorrectionId { get; set; }
    public PostSettlementOrderCorrection Correction { get; set; } = default!;
    public Guid VppId { get; set; }
    public int Qty { get; set; }
}
