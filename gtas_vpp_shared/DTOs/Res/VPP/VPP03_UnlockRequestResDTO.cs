using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP03_UnlockRequestResDTO : BaseResDTO
    {
        public Guid VPP01_RequestHeaderId { get; set; }
        public string? VPPCode { get; set; }
        public int Y { get; set; }
        public int M { get; set; }
        public string? RequesterName { get; set; }
        public string? DepartmentCode { get; set; }
        public int Status { get; set; }
        public int? ApproverUserId { get; set; }
        public string? ApproverName { get; set; }
    }
}
