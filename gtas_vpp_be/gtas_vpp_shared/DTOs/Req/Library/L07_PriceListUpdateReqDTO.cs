namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class L07_PriceListUpdateReqDTO
    {
        public Guid Id { get; set; }
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
    }
}
