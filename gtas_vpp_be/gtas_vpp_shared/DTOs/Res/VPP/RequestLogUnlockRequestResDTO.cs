using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class RequestLogUnlockRequestResDTO : BaseResDTO
    {
        public Guid RequestId { get; set; }
        public string? VppCode { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string? RequesterName { get; set; }
        public string? Code { get; set; }
        public int Status { get; set; }
        public int? ApproverUserId { get; set; }
        public string? ApproverName { get; set; }
    }
}
