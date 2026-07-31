using gtas_vpp_shared.DTOs.Res.Reports;

namespace gtas_vpp_be.Service.Services;

public sealed record ReportWorkbookItem(
    string Period,
    string Code,
    int RequesterUserId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal NetUnitPrice,
    decimal VatRate,
    decimal NetAmount,
    decimal VatAmount,
    decimal CommercialAdjustment,
    decimal GrossAmount,
    string SupplierName,
    string PriceBook,
    bool IsSupplierException);

public static class ReportWorkbookBuilder
{
    public static byte[] Build(
        ReportSummaryResDTO summary,
        IReadOnlyList<ReportWorkbookItem> items)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(items);

        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Settlement-aware report", null },
            new object?[] { "Scope", summary.Scope },
            new object?[]
            {
                "Period",
                summary.Year.HasValue && summary.Month.HasValue
                    ? $"{summary.Month:00}/{summary.Year}"
                    : "All periods"
            },
            new object?[] { "Generated UTC", summary.GeneratedAt.ToUniversalTime().ToString("O") },
            new object?[] { "Total orders", summary.TotalOrders },
            new object?[] { "Total lines", summary.TotalLines },
            new object?[] { "Total quantity", summary.TotalQuantity },
            new object?[] { "Total amount (VND)", summary.TotalAmount },
            new object?[] { "Settlement reconciled", summary.IsSettlementReconciled ? "YES" : "NO / NOT SETTLED" },
            new object?[] { "Settlement revision", summary.SettlementRevisionNumber },
            new object?[] { "Primary supplier", summary.SettlementPrimarySupplierName },
            new object?[] { "Settlement grand total", summary.SettlementGrandTotal },
            new object?[] { "Allocation total", summary.SettlementAllocationTotal },
            new object?[] { "Settlement variance", summary.SettlementVariance }
        };

        var itemRows = items.Select(item => (IReadOnlyList<object?>)new object?[]
        {
            item.Period,
            item.Code,
            item.RequesterUserId,
            item.ProductCode,
            item.ProductName,
            item.Quantity,
            item.NetUnitPrice,
            item.VatRate,
            item.NetAmount,
            item.VatAmount,
            item.CommercialAdjustment,
            item.GrossAmount,
            item.SupplierName,
            item.PriceBook,
            item.IsSupplierException ? "YES" : "NO"
        }).ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet("Summary", ["Metric", "Value"], summaryRows),
            new SimpleWorkbookSheet(
                "Items",
                [
                    "Period", "Department", "RequesterUserId", "ProductCode", "ProductName",
                    "Quantity", "NetUnitPrice", "VatRate", "NetAmount", "VatAmount",
                    "CommercialAdjustment", "GrossAmount", "Supplier", "PriceBook", "SupplierException"
                ],
                itemRows),
            new SimpleWorkbookSheet(
                "Departments",
                ["Department", "Orders", "Quantity", "AmountVND"],
                summary.DepartmentBreakdown.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.Code, item.OrderCount, item.TotalQuantity, item.TotalAmount
                }).ToArray()),
            new SimpleWorkbookSheet(
                "Trend",
                ["Period", "Orders", "Quantity", "AmountVND"],
                summary.PeriodTrend.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.Period, item.OrderCount, item.TotalQuantity, item.TotalAmount
                }).ToArray()),
            new SimpleWorkbookSheet(
                "TopProducts",
                ["ProductCode", "ProductName", "Quantity", "AmountVND"],
                summary.TopProducts.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.ProductCode, item.ProductName, item.TotalQuantity, item.TotalAmount
                }).ToArray())
        ]);
    }
}
