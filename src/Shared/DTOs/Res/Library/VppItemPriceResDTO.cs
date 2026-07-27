namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class VppItemPriceResDTO
    {
        public Guid VppId { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? CategoryName { get; set; }
        public string? UomName { get; set; }
        public Guid? PriceMappingId { get; set; }
        public decimal? Price { get; set; }
        public decimal? NetPrice { get; set; }
        public decimal VatRate { get; set; }
        public decimal MinimumOrderQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        public string? SupplierSku { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
        public string? Description { get; set; }
    }
}
