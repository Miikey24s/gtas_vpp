using gtas_vpp_shared.DTOs.Share;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class VppItemResDTO : BaseResDTO
    {
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public Guid UomId { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public LookupValueResDTO? Uom { get; set; }
        public Guid VppCategoryId { get; set; }
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
        public string? DefaultSupplierName { get; set; }
        public decimal? DefaultPrice { get; set; }
        public decimal DefaultVatRate { get; set; } = VppPricingDefaults.VatRate;
        public int SupplierCount { get; set; }
        public VppCategoryResDTO? VppCategory { get; set; }
        public ICollection<SupplierProductMappingResDTO>? SupplierProductMappings { get; set; }
    }
}
