using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.VPP;

/// <summary>
/// Phiên bản cấu hình dùng khi scheduler tạo kỳ mới. Kỳ đã tạo luôn giữ timestamp
/// chính xác của riêng nó nên cấu hình tương lai không diễn giải lại lịch sử.
/// </summary>
[Table("OrderPeriodSettingsVersions")]
public sealed class VppOrderPeriodSettingsVersion : BaseModel
{
    [Required, StringLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    [Required, StringLength(160)]
    public string Name { get; set; } = string.Empty;

    public int DefaultOpenPeriodCount { get; set; } = 1;

    public int DefaultNewPeriodOpenDay { get; set; } = 5;

    public int DefaultPeriodCloseDay { get; set; } = 5;

    public TimeSpan LocalTimeOfDay { get; set; } = TimeSpan.Zero;

    [Required, StringLength(64)]
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";

    public int SupplementApprovalGraceDays { get; set; } = 5;

    public int PostCloseAdjustmentDays { get; set; } = 10;

    // Giữ lại để đọc schema đã phát hành; luồng mở lại chốt không còn được dùng.
    public int SettlementReopenWindowDays { get; set; } = 3;

    public int EffectiveFromYear { get; set; }

    public int EffectiveFromMonth { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<VppPeriod> Periods { get; set; } = new List<VppPeriod>();
}
