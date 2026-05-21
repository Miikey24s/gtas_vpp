using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_shared.UI;
using System.Globalization;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP01_RequestHeaderResDTO : BaseResDTO
    {
        public string? VPPCode { get; set; }
        public int Y { get; set; }
        public int M { get; set; }
        public int Status { get; set; }
        public string? DepartmentCode { get; set; }
        public string? MemberCompanyCode { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? SettledAt { get; set; }
        public int? SettledByUserId { get; set; }
        public string? SettledByUserName { get; set; }
        public Guid? SettledByPriceListId { get; set; }
        public string? SettledByPriceListName { get; set; }
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public bool IsAdditionalOrder { get; set; }

        // Display-only computed properties (no clock dependency)
        public string Period => $"{M:00}/{Y}";
        // P4/F-16: Delegate to the shared StatusDisplay helper; the only
        // DTO-specific twist is the "Period Closed" annotation when a
        // regular submitted order has passed its deadline.
        public string StatusText => StatusDisplay.GetText(Status, IsDeadlinePassed, IsAdditionalOrder);
        public string SubmittedDateText => SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-";
        public string? RequesterName { get; set; }

        // P1: BE materializes these flags using PeriodCalculator + IDateTimeProvider so
        // FE doesn't recompute them with its own clock (was F-02 / F-33 root cause).
        public bool IsDeadlinePassed { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }

        public List<VPP02_RequestDetailResDTO> Items { get; set; } = new();
    }
}
