using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L05_VPPSupplierResDTO : BaseResDTO
    {
        public string? SupplierShortName { get; set; }
        public string? SupplierName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public ICollection<L06_VPPSupplierMappingResDTO>? L06_VPPSupplierMappings { get; set; }
    }
}
