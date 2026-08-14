using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class SupplierResDTO : BaseResDTO
    {
        public string? SupplierShortName { get; set; }
        public string? SupplierName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public int ItemCount { get; set; }
        public int PriceListCount { get; set; }
        public ICollection<SupplierProductMappingResDTO>? SupplierProductMappings { get; set; }
    }
}
