using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_shared.Constants;
using System.Globalization;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VppRequestResDTO : BaseResDTO
    {
        public string? VppCode { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public Guid? PeriodId { get; set; }
        public Guid RequestSeriesId { get; set; }
        public int RevisionNumber { get; set; }
        public bool IsCurrentRevision { get; set; }
        public Guid? SupersedesRequestId { get; set; }
        public Guid? SupersededByRequestId { get; set; }
        public Guid? BaseRequestId { get; set; }
        public Guid? BaseRequestSeriesId { get; set; }
        public int? SupplementSequence { get; set; }
        public int? SupplementAttemptNumber { get; set; }
        public string? SupplementReason { get; set; }
        public int Status { get; set; }
        public string? DepartmentCode { get; set; }
        public string? MemberCompanyCode { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RejectedById { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectReason { get; set; }
        public int? CancelledById { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? SettledAt { get; set; }
        public int? SettledByUserId { get; set; }
        public string? SettledByUserName { get; set; }
        public Guid? SettledByPriceListId { get; set; }
        public string? SettledByPriceListName { get; set; }
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
        public bool IsAdditionalOrder { get; set; }
        public byte[]? RowVersion { get; set; }

        // Display-only computed properties (no clock dependency)
        public string Period => $"{Month:00}/{Year}";
        // P4/F-16: Delegate to the shared status contract; the only
        // DTO-specific twist is the "Period Closed" annotation when a
        // regular submitted order has passed its deadline.
        public string StatusText => VppStatusContract.GetText(Status, IsDeadlinePassed, IsAdditionalOrder);
        public string SubmittedDateText => SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-";
        public string? RequesterName { get; set; }

        // P1: BE materializes these flags using PeriodCalculator + IDateTimeProvider so
        // FE doesn't recompute them with its own clock (was F-02 / F-33 root cause).
        public bool IsDeadlinePassed { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool CanReplace { get; set; }

        public List<VppRequestDetailResDTO> Items { get; set; } = new();
    }
}
