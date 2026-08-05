using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class VppCategoryResDTO : BaseResDTO
    {
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
        public ICollection<VppItemResDTO>? VppItems { get; set; }
    }
}
