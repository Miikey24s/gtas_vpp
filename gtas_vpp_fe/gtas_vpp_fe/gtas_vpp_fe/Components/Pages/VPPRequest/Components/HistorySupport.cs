using System.Globalization;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Components;

// Hỗ trợ dùng chung cho màn Lịch sử (Atlas history): Tab_History là coordinator giữ toàn bộ state,
// 5 component con (HistoryScopeBar, HistoryKpiCards, HistoryTrendChart, HistoryOrderList,
// HistoryOrderDetailSheet) chỉ nhận tham số và bắn EventCallback.

/// <summary>Một dòng trong bảng chi tiết phiếu (số thứ tự sau khi lọc + mặt hàng).</summary>
public sealed record HistoryDetailRow(int Number, VppRequestDetailResDTO Item);

/// <summary>
/// Khóa chuỗi trạng thái UI của màn Lịch sử. Giá trị phải giữ nguyên vì được so sánh
/// với chuỗi runtime (scope query, tên menu lọc, khóa KPI, khóa series biểu đồ).
/// </summary>
public static class HistoryUiKeys
{
    public const string AllScope = "all";
    public const string Last1Scope = "last1";
    public const string Last3Scope = "last3";
    public const string Last6Scope = "last6";
    public const string Last12Scope = "last12";
    public const string CustomScope = "custom";
    public const string StatusMenu = "status";
    public const string OrderTypeMenu = "order-type";
    public const string CategoryMenu = "category";
    public const string UomMenu = "uom";
    public const string KpiPeriods = "periods";
    public const string KpiOrders = "orders";
    public const string KpiLines = "lines";
    public const string KpiQuantity = "quantity";
    public const string RegularSeries = "regular";
    public const string AdditionalSeries = "additional";
}

/// <summary>Helper định dạng số dùng chung giữa Tab_History và các component con của màn Lịch sử.</summary>
public static class HistoryFormat
{
    public static string FormatNumber(int value) => value.ToString("N0", CultureInfo.CurrentCulture);

    public static string FormatRatio(int numerator, int denominator) => denominator == 0
        ? "–"
        : (numerator / (decimal)denominator).ToString("N1", CultureInfo.CurrentCulture);
}
