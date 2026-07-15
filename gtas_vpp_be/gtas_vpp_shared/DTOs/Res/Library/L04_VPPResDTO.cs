using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L04_VPPResDTO : BaseResDTO
    {
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public Guid UOMId { get; set; }
        public L02_ClassDetailResDTO? UOM { get; set; }
        public Guid VPPCategoryId { get; set; }
        public string? DefaultSupplierName { get; set; }
        public decimal? DefaultPrice { get; set; }
        public decimal DefaultVatRate { get; set; } = VppPricingDefaults.VatRate;
        public L03_VPPCategoryResDTO? VPPCategory { get; set; }
        public ICollection<L06_VPPSupplierMappingResDTO>? L06_VPPSupplierMappings { get; set; }
    }
}
