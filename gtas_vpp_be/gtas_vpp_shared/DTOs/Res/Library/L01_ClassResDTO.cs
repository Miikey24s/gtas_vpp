using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L01_ClassResDTO : BaseResDTO
    {
        public string? ClassCode { get; set; }
        
        public string? ClassName { get; set; }
        
        public string? ClassModul { get; set; }
        
        public string? CreateUserName { get; set; }
        
        public string? UpdateUserName { get; set; }
        
        public virtual ICollection<L02_ClassDetailResDTO>? L02_ClassDetails { get; set; }
    }
}
