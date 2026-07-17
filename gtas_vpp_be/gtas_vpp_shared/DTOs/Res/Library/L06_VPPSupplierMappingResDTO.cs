using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L06_VPPSupplierMappingResDTO : BaseResDTO
    {
        public decimal Price { get; set; }
        public decimal NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal MinimumOrderQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public string? SupplierSku { get; set; }
        public byte[]? RowVersion { get; set; }
        public bool IsDefault { get; set; }
        public Guid L04_VPPId { get; set; }
        public string? L04_VPPName { get; set; }
        public L04_VPPResDTO? L04_VPP { get; set; }
        public Guid L05_VPPSupplierId { get; set; }
        public string? L05_SupplierName { get; set; }
        public L05_VPPSupplierResDTO? L05_VPPSupplier { get; set; }
        public Guid L07_PriceListId { get; set; }
        public string? L07_PriceListName { get; set; }
        public L07_PriceListResDTO? L07_PriceList { get; set; }
    }
}
