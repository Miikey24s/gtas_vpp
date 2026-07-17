namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class L06_PriceCreateReqDTO
    {
        public Guid L04_VPPId { get; set; }
        public Guid L05_VPPSupplierId { get; set; }
        public Guid L07_PriceListId { get; set; }
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
