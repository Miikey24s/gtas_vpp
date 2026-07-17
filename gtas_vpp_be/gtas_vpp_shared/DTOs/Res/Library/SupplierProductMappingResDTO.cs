using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class SupplierProductMappingResDTO : BaseResDTO
    {
        public decimal Price { get; set; }
        public decimal NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal MinimumOrderQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public string? SupplierSku { get; set; }
        public byte[]? RowVersion { get; set; }
        public bool IsDefault { get; set; }
        public Guid VppItemId { get; set; }
        public string? VppItemName { get; set; }
        public VppItemResDTO? VppItem { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public SupplierResDTO? Supplier { get; set; }
        public Guid PriceListId { get; set; }
        public string? PriceListName { get; set; }
        public PriceListResDTO? PriceList { get; set; }
    }
}
