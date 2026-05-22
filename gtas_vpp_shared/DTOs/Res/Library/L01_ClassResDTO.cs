using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L01_ClassResDTO : BaseResDTO
    {
        [GridColumnProperty("Class Code", "150px")]
        public string? ClassCode { get; set; }
        
        [GridColumnProperty("Class Name", "250px")]
        public string? ClassName { get; set; }
        
        [GridColumnProperty("Class Module", "150px")]
        public string? ClassModul { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public string? CreateUserName { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public string? UpdateUserName { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public virtual ICollection<L02_ClassDetailResDTO>? L02_ClassDetails { get; set; }
    }
}
