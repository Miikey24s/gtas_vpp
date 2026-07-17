namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class PriceListCreateReqDTO
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public Guid? SupplierId { get; set; }
        public int Version { get; set; } = 1;
        public DateTime? EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        public string VatPolicy { get; set; } = "item-rate";
        public string? ContractCode { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal RebateAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal ShippingAmount { get; set; }
    }
}
