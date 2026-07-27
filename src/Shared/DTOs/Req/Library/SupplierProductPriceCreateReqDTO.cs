namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class SupplierProductPriceCreateReqDTO
    {
        public Guid VppItemId { get; set; }
        public Guid SupplierId { get; set; }
        public Guid PriceListId { get; set; }
        public decimal Price { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal MinimumOrderQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public string? SupplierSku { get; set; }
        public bool IsDefault { get; set; }
        public string? Description { get; set; }
    }
}
