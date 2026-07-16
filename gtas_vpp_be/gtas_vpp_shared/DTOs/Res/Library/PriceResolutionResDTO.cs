namespace gtas_vpp_shared.DTOs.Res.Library;

public sealed class PriceResolutionResDTO
{
    public bool IsResolved { get; set; }
    public PriceResolutionBlockerCode BlockerCode { get; set; }
    public string? BlockerMessage { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public Guid VppId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? PriceListId { get; set; }
    public string? PriceListCode { get; set; }
    public int? PriceListVersion { get; set; }
    public Guid? PriceBookItemId { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal Quantity { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal MinimumOrderQuantity { get; set; }
    public int LeadTimeDays { get; set; }
    public string? SupplierSku { get; set; }
    public string CalculationVersion { get; set; } = "price-vat-v1";
}
