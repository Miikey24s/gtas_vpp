using System.Globalization;
using gtas_vpp_be.Model.VPP;

namespace gtas_vpp_be.Service.Services;

public static class SettlementWorkbookBuilder
{
    public static byte[] Build(Settlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Biên bản chốt kỳ văn phòng phẩm", null },
            new object?[] { "Kỳ", $"{settlement.Month:00}/{settlement.Year}" },
            new object?[] { "Phiên bản", settlement.RevisionNumber },
            new object?[] { "Phiên bản hiện hành", settlement.IsCurrentRevision ? "Có" : "Không" },
            new object?[] { "Phiên hiệu chỉnh", settlement.IsCorrection ? "Có" : "Không" },
            new object?[] { "Lý do hiệu chỉnh", settlement.CorrectionReason ?? "-" },
            new object?[] { "Nhà cung cấp chính", settlement.PrimarySupplierName },
            new object?[] { "Bảng giá", settlement.PriceListName },
            new object?[] { "Phiên bản bảng giá", settlement.PriceListVersion },
            new object?[] { "Giá áp dụng lúc", settlement.PriceAsOfUtc.ToString("HH:mm dd/MM/yyyy", culture) },
            new object?[] { "Tiền tệ", settlement.CurrencyCode },
            new object?[] { "Tạm tính", settlement.Subtotal },
            new object?[] { "Giảm giá", settlement.DiscountAmount },
            new object?[] { "Chiết khấu", settlement.RebateAmount },
            new object?[] { "Phí", settlement.FeeAmount },
            new object?[] { "Vận chuyển", settlement.ShippingAmount },
            new object?[] { "Thuế VAT", settlement.VatAmount },
            new object?[] { "Điều chỉnh làm tròn", settlement.RoundingAdjustment },
            new object?[] { "Tổng giá trị", settlement.GrandTotal },
            new object?[] { "Chốt lúc", settlement.ConfirmedAtUtc.ToString("HH:mm dd/MM/yyyy", culture) },
            new object?[] { "Người chốt", settlement.ConfirmedByUserId },
            new object?[] { "Phiên bản tính toán", settlement.CalculationVersion },
            new object?[] { "Mã đối chiếu", settlement.InputHash }
        };

        var itemRows = settlement.Items
            .OrderBy(item => item.VppName)
            .ThenBy(item => item.VppCode)
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.VppCode,
                item.VppName,
                item.UomName,
                item.Quantity,
                item.NetUnitPrice,
                item.VatRate,
                item.NetAmount,
                item.VatAmount,
                item.GrossAmount,
                item.SupplierSku,
                item.IsSupplierException ? "Có" : "Không",
                item.SupplierExceptionReason ?? "-"
            })
            .ToArray();

        var allocationRows = settlement.Allocations
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.RequesterUserId)
            .ThenBy(item => item.RequestDetailId)
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.DepartmentCode,
                item.RequesterUserId,
                item.SettlementItem.VppCode,
                item.SettlementItem.VppName,
                item.Quantity,
                item.NetAmount,
                item.VatAmount,
                item.CommercialAdjustmentAmount,
                item.RoundingAdjustment,
                item.GrossAmount
            })
            .ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet(
                "Tổng quan",
                [new("Trường", 26), new("Giá trị", 48)],
                summaryRows),
            new SimpleWorkbookSheet(
                "Mặt hàng",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Mã mặt hàng", 24), new("Tên mặt hàng", 36), new("Đơn vị", 14),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Đơn giá trước thuế", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Thuế VAT (%)", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền trước thuế", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền thuế", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Thành tiền", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Mã NCC", 22), new("Ngoại lệ NCC", 16), new("Lý do ngoại lệ", 36)
                ],
                itemRows),
            new SimpleWorkbookSheet(
                "Phân bổ",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Phòng ban", 18), new("Mã người yêu cầu", 18, SimpleWorkbookCellFormat.Integer),
                    new("Mã mặt hàng", 24), new("Tên mặt hàng", 36),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền trước thuế", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Tiền thuế", 18, SimpleWorkbookCellFormat.Decimal),
                    new("Điều chỉnh thương mại", 22, SimpleWorkbookCellFormat.Decimal),
                    new("Điều chỉnh làm tròn", 20, SimpleWorkbookCellFormat.Decimal),
                    new("Thành tiền", 20, SimpleWorkbookCellFormat.Decimal)
                ],
                allocationRows)
        ]);
    }
}
