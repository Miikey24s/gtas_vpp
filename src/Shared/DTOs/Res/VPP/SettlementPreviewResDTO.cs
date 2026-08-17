using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class SettlementPreviewResDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public int RequestedItemCount { get; set; }
    public int RequestedLineCount { get; set; }
    public int PendingAdditionalCount { get; set; }
    public Guid? PrimarySupplierId { get; set; }
    public Guid? PrimaryPriceListId { get; set; }
    public int? PrimaryPriceListVersion { get; set; }
    public PriceBookQuoteResDTO? PrimaryQuote { get; set; }
    public List<PriceBookQuoteResDTO> Quotes { get; set; } = [];
    public List<SettlementFinancialAllocationResDTO> Allocations { get; set; } = [];
    public List<string> Blockers { get; set; } = [];
    public List<SettlementExceptionResDTO> Exceptions { get; set; } = [];
    public SettlementSupplierRecommendationResDTO? SupplierRecommendation { get; set; }
}

public sealed class SettlementExceptionResDTO
{
    public Guid VppId { get; set; }
    public Guid SupplierId { get; set; }
    public string? Reason { get; set; }
    public bool IsValid { get; set; }
    public Guid? PriceListId { get; set; }
    public int? PriceListVersion { get; set; }
    public Guid? PriceBookItemId { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }
    public string? Blocker { get; set; }
}

public sealed class SettlementSupplierRecommendationResDTO
{
    public bool IsAvailable { get; set; }
    public bool IsRecommended { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public decimal MinimumSavingsPercent { get; set; }
    public decimal? BaselineGrandTotal { get; set; }
    public Guid? BaselineSupplierId { get; set; }
    public Guid? BaselinePriceListId { get; set; }
    public decimal RecommendedGrandTotal { get; set; }
    public decimal SavingsAmount { get; set; }
    public decimal SavingsPercent { get; set; }
    public Guid PrimarySupplierId { get; set; }
    public Guid PrimaryPriceListId { get; set; }
    public List<SettlementSupplierAllocationResDTO> Suppliers { get; set; } = [];
    public List<SettlementSupplierExceptionSuggestionResDTO> SuggestedExceptions { get; set; } = [];
}

public sealed class SettlementSupplierAllocationResDTO
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid PriceListId { get; set; }
    public string PriceListCode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int ItemCount { get; set; }
    public decimal GrossAmount { get; set; }
}

public sealed class SettlementSupplierExceptionSuggestionResDTO
{
    public Guid VppId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid PriceListId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
