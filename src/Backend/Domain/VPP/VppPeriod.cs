using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.VPP;

/// <summary>
/// Kỳ VPP theo tháng trong phạm vi công ty. Kỳ là aggregate bền vững thay vì giá trị
/// suy ra từ các dòng yêu cầu; nhờ đó kỳ rỗng, phục hồi sau downtime và trạng thái
/// chốt kỳ được biểu diễn rõ ràng.
/// </summary>
[Table("Periods")]
public class VppPeriod : BaseModel
{
    [Required, StringLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    /// <summary>
    /// Múi giờ nghiệp vụ IANA dùng để tính các cột mốc UTC. Giá trị được lưu cùng
    /// aggregate để thay đổi chính sách múi giờ sau này không diễn giải lại kỳ đã tạo.
    /// </summary>
    [Required, StringLength(64)]
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";

    public int Year { get; set; }

    public int Month { get; set; }

    /// <summary>Thời điểm bắt đầu kỳ, có tính biên, theo UTC (00:00 ngày 05 giờ địa phương).</summary>
    public DateTime StartAtUtc { get; set; }

    /// <summary>Hạn gửi yêu cầu thường theo UTC.</summary>
    public DateTime SubmissionDeadlineUtc { get; set; }

    /// <summary>
    /// Thời điểm cuối có thể duyệt yêu cầu bổ sung đang chờ, theo UTC. Theo chính sách,
    /// mốc này được tách khỏi hạn gửi yêu cầu thường.
    /// </summary>
    public DateTime SupplementApprovalDeadlineUtc { get; set; }

    public VppPeriodState State { get; set; } = VppPeriodState.Open;

    public int? LastTransitionUserId { get; set; }

    public DateTime? LastTransitionAtUtc { get; set; }

    [StringLength(500)]
    public string? LastTransitionReason { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<VppRequest> Requests { get; set; } =
        new List<VppRequest>();
}
