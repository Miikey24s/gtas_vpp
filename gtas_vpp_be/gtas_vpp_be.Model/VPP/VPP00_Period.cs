using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.VPP;

/// <summary>
/// Company-scoped monthly VPP period.  A period is a durable aggregate rather
/// than a value inferred from request rows; this lets empty periods, downtime
/// recovery and settlement state be represented unambiguously.
/// </summary>
[Table("VPP00_Period")]
public class VPP00_Period : BaseModel
{
    [Required, StringLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    /// <summary>
    /// IANA business timezone used to derive the UTC boundary columns.  It is
    /// persisted with the aggregate so a future timezone-policy change cannot
    /// reinterpret an already-created period.
    /// </summary>
    [Required, StringLength(64)]
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";

    public int Y { get; set; }

    public int M { get; set; }

    /// <summary>Inclusive period start in UTC (00:00 on local day 05).</summary>
    public DateTime StartAtUtc { get; set; }

    /// <summary>Regular request submission deadline in UTC.</summary>
    public DateTime SubmissionDeadlineUtc { get; set; }

    /// <summary>
    /// Last time a pending supplement may be approved, in UTC.  This is kept
    /// separate from the regular submission deadline by policy.
    /// </summary>
    public DateTime SupplementApprovalDeadlineUtc { get; set; }

    public VppPeriodState State { get; set; } = VppPeriodState.Open;

    public int? LastTransitionUserId { get; set; }

    public DateTime? LastTransitionAtUtc { get; set; }

    [StringLength(500)]
    public string? LastTransitionReason { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<VPP01_RequestHeader> RequestHeaders { get; set; } =
        new List<VPP01_RequestHeader>();
}
