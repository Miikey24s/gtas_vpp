using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L02_ClassDetailResDTO : BaseResDTO
    {
        public L01_ClassResDTO? Class { get; set; }
        
        public Guid? ClassId { get; set; }
        
        public string? ClassDetailCode { get; set; }
        
        public string? ClassDetailValue { get; set; }
        
        public string? ExtraField1 { get; set; }
        
        public string? ExtraField2 { get; set; }
        
        public string? ExtraField3 { get; set; }
        
        public int Sort { get; set; }
        
        public ICollection<L04_VPPResDTO>? VPPs_UOM { get; set; }
        
        public string? CreateUserName { get; set; }
        
        public string? UpdateUserName { get; set; }
    }
}
