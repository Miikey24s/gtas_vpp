namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP_PeriodInfoResDTO
    {
        public int CurrentPeriodYear { get; set; }
        public int CurrentPeriodMonth { get; set; }
        public int PreviousPeriodYear { get; set; }
        public int PreviousPeriodMonth { get; set; }
        public DateTime DeadlineDate { get; set; }
        public bool IsDeadlinePassed { get; set; }
        public bool HasCurrentPeriodOrder { get; set; }
        public int AdditionalOrderCount { get; set; }
        public int MaxAdditionalOrders { get; set; }
        public bool HasPendingAdditional { get; set; }
        public bool CanCreateOrder { get; set; }
        public bool CanCreateAdditional { get; set; }
        public bool HasPreviousOrder { get; set; }
        public bool CanCopyPrevious { get; set; }
    }
}
