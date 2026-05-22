using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L03_VPPCategoryResDTO : BaseResDTO
    {
        public string? VPPCategoryCode { get; set; }
        public string? VPPCategoryName { get; set; }
        public ICollection<L04_VPPResDTO>? VPPs { get; set; }
    }
}
