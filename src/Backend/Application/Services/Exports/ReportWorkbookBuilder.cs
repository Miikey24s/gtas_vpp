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
            new object?[] { "GTAS VPP — Báo cáo tổng hợp văn phòng phẩm", null },
            new object?[] { "Phạm vi", summary.Scope },
            new object?[]
            {
                "Kỳ",
                summary.Year.HasValue && summary.Month.HasValue
                    ? $"{summary.Month:00}/{summary.Year}"
                    : "Tất cả kỳ"
            },
            new object?[] { "Tạo lúc (UTC)", summary.GeneratedAt.ToUniversalTime().ToString("O") },
            new object?[] { "Tổng đơn", summary.TotalOrders },
            new object?[] { "Tổng mặt hàng", summary.TotalLines },
            new object?[] { "Tổng số lượng", summary.TotalQuantity },
            new object?[] { "Tổng giá trị (VND)", summary.TotalAmount },
            new object?[] { "Đã đối chiếu chốt kỳ", summary.IsSettlementReconciled ? "Có" : "Chưa / Chưa chốt kỳ" },
            new object?[] { "Phiên bản chốt kỳ", summary.SettlementRevisionNumber },
            new object?[] { "Nhà cung cấp chính", summary.SettlementPrimarySupplierName },
            new object?[] { "Tổng giá trị chốt kỳ", summary.SettlementGrandTotal },
            new object?[] { "Tổng phân bổ", summary.SettlementAllocationTotal },
            new object?[] { "Chênh lệch chốt kỳ", summary.SettlementVariance }
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
            item.IsSupplierException ? "Có" : "Không"
        }).ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet(
                "Tổng quan",
                [new("Chỉ số", 28), new("Giá trị", 46)],
                summaryRows),
            new SimpleWorkbookSheet(
                "Mặt hàng",
                [
                    new("Kỳ", 14), new("Phòng ban", 18),
                    new("Mã người yêu cầu", 18, SimpleWorkbookCellFormat.Integer),
                    new("Mã mặt hàng", 24), new("Tên mặt hàng", 36),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Đơn giá trước thuế", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Thuế VAT (%)", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền trước thuế", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền thuế", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Điều chỉnh", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Thành tiền", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Nhà cung cấp", 28), new("Bảng giá", 28), new("Ngoại lệ NCC", 16)
                ],
                itemRows),
            new SimpleWorkbookSheet(
                "Phòng ban",
                [
                    new("Phòng ban", 24),
                    new("Số đơn", 14, SimpleWorkbookCellFormat.Integer),
                    new("Tổng số lượng", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Tổng giá trị (VND)", 22, SimpleWorkbookCellFormat.Decimal)
                ],
                summary.DepartmentBreakdown.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.Code, item.OrderCount, item.TotalQuantity, item.TotalAmount
                }).ToArray()),
            new SimpleWorkbookSheet(
                "Xu hướng",
                [
                    new("Kỳ", 14),
                    new("Số đơn", 14, SimpleWorkbookCellFormat.Integer),
                    new("Tổng số lượng", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Tổng giá trị (VND)", 22, SimpleWorkbookCellFormat.Decimal)
                ],
                summary.PeriodTrend.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.Period, item.OrderCount, item.TotalQuantity, item.TotalAmount
                }).ToArray()),
            new SimpleWorkbookSheet(
                "Mặt hàng nổi bật",
                [
                    new("Mã mặt hàng", 24), new("Tên mặt hàng", 36),
                    new("Tổng số lượng", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Tổng giá trị (VND)", 22, SimpleWorkbookCellFormat.Decimal)
                ],
                summary.TopProducts.Select(item => (IReadOnlyList<object?>)new object?[]
                {
                    item.ProductCode, item.ProductName, item.TotalQuantity, item.TotalAmount
                }).ToArray())
        ]);
    }
}
