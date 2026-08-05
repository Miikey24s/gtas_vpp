using System.Globalization;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Tạo workbook hai sheet cho một đơn hàng. Phần SpreadsheetML dùng chung nằm
/// trong <see cref="SimpleWorkbookBuilder"/> để export khác không tạo thêm pipeline.
/// </summary>
public static class OrderWorkbookBuilder
{
    public static byte[] Build(VppRequestResDTO order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // Phiếu đơn của nhân viên không xuất giá; giá chỉ thuộc báo cáo/chốt kỳ.
        var submittedAt = order.SubmittedDate?.ToString(
            "HH:mm dd/MM/yyyy",
            CultureInfo.GetCultureInfo("vi-VN")) ?? "-";
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Mã đơn", order.VppCode, "Phiên bản", order.RevisionNumber },
            new object?[] { "Kỳ", order.Period, "Tổng mặt hàng", order.TotalLines },
            new object?[]
            {
                "Loại đơn",
                order.IsAdditionalOrder ? "Đơn bổ sung" : "Đơn thường",
                "Tổng số lượng",
                order.TotalQty
            },
            new object?[] { "Trạng thái", order.StatusText, "Gửi lúc", submittedAt },
            new object?[] { "Người đặt", order.RequesterName, "Phòng ban", order.DepartmentCode },
            new object?[]
            {
                "Ghi chú đơn",
                string.IsNullOrWhiteSpace(order.Description) ? "-" : order.Description,
                "Lý do bổ sung",
                order.IsAdditionalOrder ? order.SupplementReason ?? "-" : "-"
            }
        };

        var itemRows = order.Items
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.CategoryName,
                item.VppCode,
                item.VppName,
                item.UomName,
                item.Qty,
                string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description
            })
            .ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet(
                "Tổng quan",
                [
                    new("Trường", 20, Role: SimpleWorkbookColumnRole.Label),
                    new("Giá trị", 42),
                    new("Trường", 20, Role: SimpleWorkbookColumnRole.Label),
                    new("Giá trị", 32)
                ],
                summaryRows,
                new SimpleWorkbookSheetOptions(
                    Theme: SimpleWorkbookTheme.VppRegistration,
                    RowsBeforeHeader:
                    [
                        new(["GTAS VPP — PHIẾU ĐĂNG KÝ VĂN PHÒNG PHẨM"], SimpleWorkbookRowStyle.Title),
                        new([], SimpleWorkbookRowStyle.Spacer),
                        new(["THÔNG TIN ĐƠN", null, "TỔNG HỢP", null], SimpleWorkbookRowStyle.Section)
                    ],
                    MergedRanges: ["A1:D1", "A3:B3", "C3:D3"],
                    FreezeRows: 4,
                    ShowAutoFilter: false,
                    ShowGridLines: false)),
            new SimpleWorkbookSheet(
                "Mặt hàng",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Nhóm VPP", 30),
                    new("Mã VPP", 24),
                    new("Tên VPP", 38),
                    new("Đơn vị", 14),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Ghi chú", 36)
                ],
                itemRows,
                new SimpleWorkbookSheetOptions(
                    Theme: SimpleWorkbookTheme.VppRegistration,
                    RowsBeforeHeader:
                    [
                        new([$"ĐƠN VỊ: {order.DepartmentCode?.ToUpperInvariant() ?? "-"}"], SimpleWorkbookRowStyle.Title),
                        new([], SimpleWorkbookRowStyle.Spacer),
                        new(["DANH SÁCH VĂN PHÒNG PHẨM", null, null, null, null, "ĐĂNG KÝ", null], SimpleWorkbookRowStyle.Section),
                        new(["TỔNG CỘNG", null, null, null, null, order.TotalQty, $"{order.TotalLines:N0} mặt hàng"], SimpleWorkbookRowStyle.Summary)
                    ],
                    MergedRanges: ["A1:G1", "A3:E3", "F3:G3", "A4:E4"],
                    FreezeRows: 5,
                    ShowAutoFilter: true,
                    ShowGridLines: false))
        ]);
    }
}
