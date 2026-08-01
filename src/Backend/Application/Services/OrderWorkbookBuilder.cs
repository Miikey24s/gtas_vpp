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
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Phiếu chi tiết đơn văn phòng phẩm", null },
            new object?[] { "Mã đơn", order.VppCode },
            new object?[] { "Kỳ", order.Period },
            new object?[] { "Loại đơn", order.IsAdditionalOrder ? "Đơn bổ sung" : "Đơn thường" },
            new object?[] { "Trạng thái", order.StatusText },
            new object?[] { "Người đặt", order.RequesterName },
            new object?[] { "Phòng ban", order.DepartmentCode },
            new object?[]
            {
                "Gửi lúc",
                order.SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-"
            },
            new object?[] { "Phiên bản", order.RevisionNumber },
            new object?[] { "Tổng mặt hàng", order.TotalLines },
            new object?[] { "Tổng số lượng", order.TotalQty },
            new object?[] { "Ghi chú đơn", string.IsNullOrWhiteSpace(order.Description) ? "-" : order.Description }
        };

        if (order.IsAdditionalOrder)
        {
            summaryRows.Add(new object?[] { "Lý do bổ sung", order.SupplementReason ?? "-" });
        }

        var itemRows = order.Items
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.VppCode,
                item.VppName,
                item.CategoryName,
                item.UomName,
                item.Qty,
                string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description
            })
            .ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet(
                "Tổng quan",
                [new("Trường", 24), new("Giá trị", 48)],
                summaryRows),
            new SimpleWorkbookSheet(
                "Mặt hàng",
                [
                    new("#", 8, SimpleWorkbookCellFormat.Integer),
                    new("Mã mặt hàng", 24),
                    new("Tên mặt hàng", 36),
                    new("Danh mục", 28),
                    new("Đơn vị", 14),
                    new("Số lượng", 14, SimpleWorkbookCellFormat.Decimal),
                    new("Ghi chú", 36)
                ],
                itemRows)
        ]);
    }
}
