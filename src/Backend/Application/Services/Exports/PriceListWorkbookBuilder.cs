namespace gtas_vpp_be.Service.Services;

public sealed record PriceListWorkbookRow(
    string? ItemCode,
    string? SupplierSku,
    string? ItemName,
    decimal UnitPrice,
    decimal VatRate,
    decimal MinimumOrderQuantity,
    int LeadTimeDays,
    bool IsDefault,
    string? Note);

/// <summary>
/// Giữ file mẫu và file xuất bảng giá dùng cùng một cấu trúc để file đã xuất có thể nhập lại.
/// </summary>
public static class PriceListWorkbookBuilder
{
    private static readonly IReadOnlyList<SimpleWorkbookColumn> Columns =
    [
        new("ItemCode", 22),
        new("SupplierSku", 22),
        new("ItemName", 34),
        new("UnitPrice", 18, SimpleWorkbookCellFormat.Decimal),
        new("VatRate", 14, SimpleWorkbookCellFormat.Decimal),
        new("MinimumOrderQuantity", 24, SimpleWorkbookCellFormat.Decimal),
        new("LeadTimeDays", 18, SimpleWorkbookCellFormat.Integer),
        new("IsDefault", 14),
        new("Note", 32)
    ];

    private static readonly IReadOnlyList<IReadOnlyList<object?>> GuideRows =
    [
        ["ItemCode", "Bắt buộc. Nhập đúng mã mặt hàng trong hệ thống."],
        ["UnitPrice", "Bắt buộc. Đơn giá VND, lớn hơn hoặc bằng 0."],
        ["VatRate", "Không bắt buộc. Từ 0 đến 100; để trống sẽ giữ giá trị cũ hoặc dùng 0 khi thêm mới."],
        ["MinimumOrderQuantity", "Không bắt buộc. Số lượng đặt tối thiểu."],
        ["LeadTimeDays", "Không bắt buộc. Số ngày giao hàng."],
        ["IsDefault", "Không bắt buộc. Có/Không, Yes/No, True/False hoặc 1/0."],
        ["Tên cột khác mẫu", "Hệ thống sẽ yêu cầu ghép cột trước khi kiểm tra dữ liệu."],
        ["Lưu ý", "Không thêm mặt hàng mới vào danh mục bằng file bảng giá."]
    ];

    public static byte[] BuildTemplate() => Build([]);

    public static byte[] Build(IReadOnlyList<PriceListWorkbookRow> rows)
    {
        var dataRows = rows
            .Select(row => (IReadOnlyList<object?>)
            [
                row.ItemCode,
                row.SupplierSku,
                row.ItemName,
                row.UnitPrice,
                row.VatRate,
                row.MinimumOrderQuantity,
                row.LeadTimeDays,
                row.IsDefault ? "Có" : "Không",
                row.Note
            ])
            .ToArray();

        return SimpleWorkbookBuilder.Build(
        [
            new SimpleWorkbookSheet("BangGia", Columns, dataRows),
            new SimpleWorkbookSheet(
                "HuongDan",
                [new("Cột", 28), new("Cách nhập", 60)],
                GuideRows,
                new SimpleWorkbookSheetOptions(Orientation: "portrait"))
        ]);
    }
}
