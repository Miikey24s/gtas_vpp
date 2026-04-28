using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L02_ClassDetailResDTO : BaseResDTO
    {
        [GridColumnProperty(ignore: true)]
        public L01_ClassResDTO? Class { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public Guid? ClassId { get; set; }
        
        [GridColumnProperty("Code", "150px")]
        public string? ClassDetailCode { get; set; }
        
        [GridColumnProperty("Value", "200px")]
        public string? ClassDetailValue { get; set; }
        
        [GridColumnProperty("Extra 1", "120px")]
        public string? ExtraField1 { get; set; }
        
        [GridColumnProperty("Extra 2", "120px")]
        public string? ExtraField2 { get; set; }
        
        [GridColumnProperty("Extra 3", "120px")]
        public string? ExtraField3 { get; set; }
        
        [GridColumnProperty("Sort", "80px")]
        public int Sort { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public ICollection<L04_VPPResDTO>? VPPs_UOM { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public string? CreateUserName { get; set; }
        
        [GridColumnProperty(ignore: true)]
        public string? UpdateUserName { get; set; }
    }
}
