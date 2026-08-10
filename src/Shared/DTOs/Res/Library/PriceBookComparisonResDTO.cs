namespace gtas_vpp_shared.DTOs.Res.Library;

public sealed class PriceBookComparisonResDTO
{
    public DateTime PriceAsOfUtc { get; set; }
    public string CalculationVersion { get; set; } = "price-vat-v2-vnd-whole";
    public int RequestedItemCount { get; set; }
    public List<PriceBookQuoteResDTO> Quotes { get; set; } = [];
}

public sealed class PriceBookQuoteResDTO
{
    public int Rank { get; set; }
    public Guid PriceListId { get; set; }
    public string? PriceListCode { get; set; }
    public int Version { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public bool IsEligible { get; set; }
    public int CoveredItemCount { get; set; }
    public int RequestedItemCount { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RebateAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public int MaximumLeadTimeDays { get; set; }
    public List<PriceBookQuoteLineResDTO> Lines { get; set; } = [];
    public List<Guid> MissingVppIds { get; set; } = [];
    public List<string> Blockers { get; set; } = [];
}

public sealed class PriceBookQuoteLineResDTO
{
    public Guid VppId { get; set; }
    public decimal Quantity { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }
}
