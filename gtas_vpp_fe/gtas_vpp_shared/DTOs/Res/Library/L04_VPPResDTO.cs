using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L04_VPPResDTO : BaseResDTO
    {
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        [GridColumnProperty("UOM", iscompobox: true)]
        public Guid UOMId { get; set; }
        [GridColumnProperty(ignore: true)]
        public L02_ClassDetailResDTO? UOM { get; set; }
        [GridColumnProperty("VPP Category", iscompobox: true)]
        public Guid VPPCategoryId { get; set; }
        [GridColumnProperty("Supplier", "150px", readOnly: true)]
        public string? DefaultSupplierName { get; set; }
        [GridColumnProperty("Price", "112px", readOnly: true)]
        public decimal? DefaultPrice { get; set; }
        [GridColumnProperty(ignore: true)]
        public decimal DefaultVatRate { get; set; } = VppPricingDefaults.VatRate;
        [GridColumnProperty(ignore: true)]
        public L03_VPPCategoryResDTO? VPPCategory { get; set; }
        [GridColumnProperty(ignore: true)]
        public ICollection<L06_VPPSupplierMappingResDTO>? L06_VPPSupplierMappings { get; set; }
    }
}
