using System.Globalization;
using System.Text;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_be.Service.Services;

public sealed record ReportCsvRow(
    int Year,
    int Month,
    string? DepartmentCode,
    string? OrderCode,
    int Status,
    bool IsAdditionalOrder,
    string? ProductCode,
    string? ProductName,
    int Quantity,
    long UnitPrice);

public static class ReportCsvBuilder
{
    public static byte[] Build(IReadOnlyList<ReportCsvRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var csv = new StringBuilder(Math.Max(1024, rows.Count * 120));
        csv.AppendLine("sep=,");
        csv.AppendLine("Kỳ,Phòng ban,Mã đơn,Trạng thái,Đơn bổ sung,Mã mặt hàng,Tên mặt hàng,Số lượng,Đơn giá,Thành tiền");
        foreach (var row in rows)
        {
            var amount = row.Quantity * row.UnitPrice;
            csv.AppendLine(string.Join(",",
            [
                EscapeCell($"{row.Month:00}/{row.Year}"),
                EscapeCell(row.DepartmentCode),
                EscapeCell(row.OrderCode),
                EscapeCell(VppStatusContract.GetText(
                    row.Status,
                    culture: CultureInfo.GetCultureInfo("vi-VN"))),
                EscapeCell(row.IsAdditionalOrder ? "Có" : "Không"),
                EscapeCell(row.ProductCode),
                EscapeCell(row.ProductName),
                row.Quantity.ToString(CultureInfo.InvariantCulture),
                row.UnitPrice.ToString(CultureInfo.InvariantCulture),
                amount.ToString(CultureInfo.InvariantCulture)
            ]));
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
    }

    public static string EscapeCell(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@')
        {
            safe = $"'{safe}";
        }

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
