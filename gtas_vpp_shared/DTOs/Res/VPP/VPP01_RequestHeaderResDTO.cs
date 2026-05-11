using gtas_vpp_shared.DTOs.Share;

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
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public bool IsAdditionalOrder { get; set; }

        // Display-only computed properties (no clock dependency)
        public string Period => $"{M:00}/{Y}";
        public string StatusText => Status switch
        {
            1 => IsDeadlinePassed && !IsAdditionalOrder ? "Submitted (Period Closed)" : "Submitted",
            4 => "Cancelled",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };
        public string? RequesterName { get; set; }

        // P1: BE materializes these flags using PeriodCalculator + IDateTimeProvider so
        // FE doesn't recompute them with its own clock (was F-02 / F-33 root cause).
        public bool IsDeadlinePassed { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }

        public List<VPP02_RequestDetailResDTO> Items { get; set; } = new();
    }
}
