namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VppPeriodInfoResDTO
    {
        public Guid? PeriodId { get; set; }
        public string PeriodState { get; set; } = "Open";
        public int CurrentPeriodYear { get; set; }
        public int CurrentPeriodMonth { get; set; }
        public int PreviousPeriodYear { get; set; }
        public int PreviousPeriodMonth { get; set; }
        public DateTime DeadlineDate { get; set; }
        public DateTime SupplementApprovalDeadlineDate { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsDeadlinePassed { get; set; }
        public bool IsSubmissionOpen { get; set; }
        public bool HasCurrentPeriodOrder { get; set; }
        public Guid? BaseRequestId { get; set; }
        public string? BaseRequestCode { get; set; }
        public int AdditionalOrderCount { get; set; }
        public int MaxAdditionalOrders { get; set; }
        public int ApprovedSupplementCount { get; set; }
        public int SupplementAttemptCount { get; set; }
        public int MaxSupplementAttempts { get; set; }
        public int RemainingApprovedSupplementQuota { get; set; }
        public int RemainingSupplementAttempts { get; set; }
        public bool HasPendingAdditional { get; set; }
        public bool CanCreateOrder { get; set; }
        public string? CanCreateOrderReason { get; set; }
        public bool CanCreateAdditional { get; set; }
        public string? CanCreateAdditionalReason { get; set; }
        public bool HasPreviousOrder { get; set; }
        public bool CanCopyPrevious { get; set; }
        public Guid? SelectedPeriodId { get; set; }
        public IReadOnlyList<VppOpenPeriodOptionResDTO> OpenPeriods { get; set; } = [];
    }
}
