using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L06_VPPSupplierMappingResDTO : BaseResDTO
    {
        public decimal Price { get; set; }
        public Guid L04_VPPId { get; set; }
        public L04_VPPResDTO? L04_VPP { get; set; }
        public Guid L05_VPPSupplierId { get; set; }
        public L05_VPPSupplierResDTO? L05_VPPSupplier { get; set; }
    }
}
