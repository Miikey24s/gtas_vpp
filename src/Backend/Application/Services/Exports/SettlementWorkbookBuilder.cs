using System.Globalization;
using gtas_vpp_be.Model.VPP;

namespace gtas_vpp_be.Service.Services;

public static class SettlementWorkbookBuilder
{
    public static byte[] Build(
        Settlement settlement,
        IReadOnlyDictionary<Guid, string>? supplierNames = null,
        IReadOnlyDictionary<Guid, string>? priceListNames = null,
        IReadOnlyDictionary<Guid, string>? unitNames = null,
        IReadOnlyDictionary<Guid, SettlementRequestExportInfo>? requests = null,
        string? confirmedByName = null)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        supplierNames ??= new Dictionary<Guid, string>();
        priceListNames ??= new Dictionary<Guid, string>();
        unitNames ??= new Dictionary<Guid, string>();
        requests ??= new Dictionary<Guid, SettlementRequestExportInfo>();

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var supplierCount = settlement.Items.Select(item => item.SupplierId).Distinct().Count();
        var orderCount = settlement.Allocations.Select(item => item.RequestHeaderId).Distinct().Count();
        var totalQuantity = settlement.Items.Sum(item => item.Quantity);
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Kỳ", $"{settlement.Month:00}/{settlement.Year}", "Bản chốt", settlement.RevisionNumber },
            new object?[] { "Trạng thái", settlement.IsCurrentRevision ? "Đang áp dụng" : "Bản cũ", "Loại bản", settlement.IsCorrection ? "Chốt lại" : "Chốt lần đầu" },
            new object?[] { "Nhà cung cấp", settlement.PrimarySupplierName, "Số nhà cung cấp", supplierCount },
            new object?[] { "Bảng giá", settlement.PriceListName, "Mã bảng giá", settlement.PriceListCode ?? "-" },
            new object?[] { "Giá áp dụng lúc", settlement.PriceAsOfUtc.ToString("HH:mm dd/MM/yyyy", culture), "Chốt lúc", settlement.ConfirmedAtUtc.ToString("HH:mm dd/MM/yyyy", culture) },
            new object?[] { "Người chốt", string.IsNullOrWhiteSpace(confirmedByName) ? settlement.ConfirmedByUserId : confirmedByName, "Tiền tệ", settlement.CurrencyCode },
            new object?[] { "Số đơn", orderCount, "Số mặt hàng", settlement.Items.Count },
            new object?[] { "Tổng số lượng", totalQuantity, "Điều chỉnh làm tròn", settlement.RoundingAdjustment },
            new object?[] { "Thành tiền trước VAT", settlement.Subtotal, "VAT", settlement.VatAmount },
            new object?[] { "Tổng cộng", settlement.GrandTotal, "Lý do chốt lại", settlement.CorrectionReason ?? "-" },
            new object?[] { "Mã đối chiếu", settlement.InputHash, null, null }
        };

        var itemRows = settlement.Items
            .OrderBy(item => item.VppName)
            .ThenBy(item => item.VppCode)
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.VppCode,
                item.VppName,
                SettlementExportValueResolver.ResolveUnitName(item, unitNames),
                supplierNames.GetValueOrDefault(item.SupplierId, item.SupplierId.ToString()),
                priceListNames.GetValueOrDefault(item.PriceListId, item.PriceListId.ToString()),
                item.Quantity,
                item.NetUnitPrice,
                item.VatRate,
                item.NetAmount,
                item.VatAmount,
                item.GrossAmount,
                item.IsSupplierException ? "Có" : "Không",
                item.SupplierExceptionReason ?? "-"
            })
            .ToArray();

        var allocationRows = settlement.Allocations
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.RequesterUserId)
            .ThenBy(item => item.RequestDetailId)
            .Select((item, index) =>
            {
                requests.TryGetValue(item.RequestHeaderId, out var request);
                return (IReadOnlyList<object?>)new object?[]
                {
                    index + 1,
                    item.DepartmentCode ?? "-",
                    request?.RequestCode ?? item.RequestHeaderId.ToString(),
                    request?.RequestType ?? "-",
                    request?.Status ?? "-",
                    request?.RequesterName ?? item.RequesterUserId.ToString(),
                    item.SettlementItem.VppCode,
                    item.SettlementItem.VppName,
                    SettlementExportValueResolver.ResolveUnitName(item.SettlementItem, unitNames),
                    item.Quantity,
                    item.NetAmount,
                    item.VatAmount,
                    item.CommercialAdjustmentAmount,
                    item.RoundingAdjustment,
                    item.GrossAmount
                };
            })
            .ToArray();

        return SimpleWorkbookBuilder.Build(
        [
            new SimpleWorkbookSheet(
                "Tổng quan",
                [
                    new("Thông tin", 24, Role: SimpleWorkbookColumnRole.Label),
                    new("Giá trị", 38),
                    new("Thông tin", 24, Role: SimpleWorkbookColumnRole.Label),
                    new("Giá trị", 38)
                ],
                summaryRows,
                new SimpleWorkbookSheetOptions(
                    Theme: SimpleWorkbookTheme.VppRegistration,
                    RowsBeforeHeader:
                    [
                        new(["GTAS VPP — BIÊN BẢN CHỐT KỲ VĂN PHÒNG PHẨM"], SimpleWorkbookRowStyle.Title),
                        new([$"Kỳ {settlement.Month:00}/{settlement.Year} · Bản chốt {settlement.RevisionNumber}"], SimpleWorkbookRowStyle.Section)
                    ],
                    MergedRanges: ["A1:D1", "A2:D2", "B14:D14"],
                    FreezeRows: 3,
                    ShowAutoFilter: false,
                    ShowGridLines: false,
                    Orientation: "portrait")),
            new SimpleWorkbookSheet(
                "Mặt hàng",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Mã mặt hàng", 24), new("Mặt hàng", 38), new("Đơn vị", 14),
                    new("Nhà cung cấp", 28), new("Bảng giá", 28),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Đơn giá trước VAT", 20, SimpleWorkbookCellFormat.Decimal),
                    new("VAT (%)", 12, SimpleWorkbookCellFormat.Decimal),
                    new("Thành tiền trước VAT", 22, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền VAT", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Tổng cộng", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Ngoại lệ NCC", 16), new("Lý do ngoại lệ", 36)
                ],
                itemRows,
                DataSheetOptions(
                    "DANH SÁCH MẶT HÀNG ĐÃ CHỐT",
                    $"{settlement.Items.Count:N0} mặt hàng · {totalQuantity:N0} tổng số lượng · {settlement.GrandTotal:N0} {settlement.CurrencyCode}",
                    14)),
            new SimpleWorkbookSheet(
                "Phân bổ",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Phòng ban", 18), new("Mã đơn", 28), new("Loại đơn", 16),
                    new("Trạng thái", 16), new("Người đặt", 24),
                    new("Mã mặt hàng", 24), new("Mặt hàng", 36), new("Đơn vị", 14),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Thành tiền trước VAT", 22, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền VAT", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Điều chỉnh", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Làm tròn", 16, SimpleWorkbookCellFormat.Decimal),
                    new("Tổng cộng", 20, SimpleWorkbookCellFormat.Decimal)
                ],
                allocationRows,
                DataSheetOptions(
                    "PHÂN BỔ THEO ĐƠN VÀ PHÒNG BAN",
                    $"{orderCount:N0} đơn · {settlement.Allocations.Count:N0} dòng phân bổ · {settlement.GrandTotal:N0} {settlement.CurrencyCode}",
                    15))
        ]);
    }

    private static SimpleWorkbookSheetOptions DataSheetOptions(
        string title,
        string summary,
        int columnCount)
    {
        var lastColumn = ColumnName(columnCount);
        return new SimpleWorkbookSheetOptions(
            Theme: SimpleWorkbookTheme.VppRegistration,
            RowsBeforeHeader:
            [
                new([title], SimpleWorkbookRowStyle.Title),
                new([summary], SimpleWorkbookRowStyle.Section)
            ],
            MergedRanges: [$"A1:{lastColumn}1", $"A2:{lastColumn}2"],
            FreezeRows: 3,
            ShowAutoFilter: true,
            ShowGridLines: false);
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }

        return name;
    }
}
