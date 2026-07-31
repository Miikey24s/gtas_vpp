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
            new object?[] { "GTAS VPP — Order sheet", null },
            new object?[] { "Order code", order.VppCode },
            new object?[] { "Period", order.Period },
            new object?[] { "Order type", order.IsAdditionalOrder ? "Additional" : "Regular" },
            new object?[] { "Status", order.StatusText },
            new object?[] { "Requester", order.RequesterName },
            new object?[] { "Department", order.DepartmentCode },
            new object?[]
            {
                "Submitted at",
                order.SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-"
            },
            new object?[] { "Revision", order.RevisionNumber },
            new object?[] { "Total lines", order.TotalLines },
            new object?[] { "Total quantity", order.TotalQty },
            new object?[] { "Order note", string.IsNullOrWhiteSpace(order.Description) ? "-" : order.Description }
        };

        if (order.IsAdditionalOrder)
        {
            summaryRows.Add(new object?[] { "Supplement reason", order.SupplementReason ?? "-" });
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
            new SimpleWorkbookSheet("Order", ["Field", "Value"], summaryRows),
            new SimpleWorkbookSheet(
                "Items",
                ["#", "Item code", "Item name", "Category", "Unit", "Quantity", "Note"],
                itemRows)
        ]);
    }
}
