namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class SettlementRevisionResDTO
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int RevisionNumber { get; set; }
    public bool IsCurrentRevision { get; set; }
    public bool IsCorrection { get; set; }
    public Guid? SupersedesSettlementId { get; set; }
    public string? CorrectionReason { get; set; }
    public Guid PrimarySupplierId { get; set; }
    public string PrimarySupplierName { get; set; } = string.Empty;
    public Guid PriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public int PriceListVersion { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public string CalculationVersion { get; set; } = string.Empty;
    public string InputHash { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "VND";
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RebateAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal RoundingAdjustment { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime ConfirmedAtUtc { get; set; }
    public int ConfirmedByUserId { get; set; }
    public bool HasExternalProcurementImpact { get; set; }
    public byte[]? RowVersion { get; set; }
    public int ItemCount { get; set; }
    public int AllocationCount { get; set; }
    public List<SettlementFinancialItemResDTO> Items { get; set; } = [];
    public List<SettlementFinancialAllocationResDTO> Allocations { get; set; } = [];
}

public sealed class SettlementFinancialItemResDTO
{
    public Guid VppId { get; set; }
    public string VppCode { get; set; } = string.Empty;
    public string VppName { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrossAmount { get; set; }
}

public sealed class SettlementFinancialAllocationResDTO
{
    public Guid RequestHeaderId { get; set; }
    public Guid RequestDetailId { get; set; }
    public Guid VppId { get; set; }
    public string? DepartmentCode { get; set; }
    public int RequesterUserId { get; set; }
    public decimal Quantity { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal CommercialAdjustmentAmount { get; set; }
    public decimal GrossAmount { get; set; }
}
