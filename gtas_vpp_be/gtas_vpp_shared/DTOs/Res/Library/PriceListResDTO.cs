using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class PriceListResDTO : BaseResDTO
    {
        public string? PriceListCode { get; set; }
        public string? PriceListName { get; set; }
        public bool IsDefault { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int Version { get; set; }
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "VND";
        public string VatPolicy { get; set; } = "item-rate";
        public string? ContractCode { get; set; }
        public string? LegacyBackfillStatus { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal RebateAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public int? PublishedByUserId { get; set; }
        public DateTime? ExpiredAtUtc { get; set; }
        public int? ExpiredByUserId { get; set; }
        public string? StatusReason { get; set; }
        public byte[]? RowVersion { get; set; }
        public int ItemCount { get; set; }
        public string? CreatedByUserName { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
