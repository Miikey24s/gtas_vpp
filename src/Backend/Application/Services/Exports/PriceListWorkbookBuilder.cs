namespace gtas_vpp_be.Service.Services;

public sealed record PriceListWorkbookRow(
    string? ItemCode,
    string? ItemName,
    string? UnitName,
    decimal? UnitPrice,
    decimal? VatRate,
    string? Note);

/// <summary>
/// Giữ file mẫu và file xuất bảng giá dùng cùng một cấu trúc để file đã xuất có thể nhập lại.
/// </summary>
public static class PriceListWorkbookBuilder
{
    private static readonly IReadOnlyList<SimpleWorkbookColumn> Columns =
    [
        new("ItemCode", 22),
        new("ItemName", 34),
        new("UnitName", 18),
        new("UnitPrice", 18, SimpleWorkbookCellFormat.Decimal),
        new("VatRate", 14, SimpleWorkbookCellFormat.Decimal),
        new("Note", 32)
    ];

    private static readonly IReadOnlyList<IReadOnlyList<object?>> GuideRows =
    [
        ["ItemCode", "Bắt buộc. Giữ nguyên mã mặt hàng do hệ thống cung cấp."],
        ["ItemName", "Tên mặt hàng được điền sẵn để đối chiếu; hệ thống vẫn nhận diện theo mã mặt hàng."],
        ["UnitName", "Đơn vị được điền sẵn. Không đổi đơn vị trong file bảng giá."],
        ["UnitPrice", "Bắt buộc với dòng cần cập nhật. Đơn giá VND, lớn hơn hoặc bằng 0."],
        ["VatRate", "Không bắt buộc. Từ 0 đến 100; để trống sẽ giữ VAT hiện tại hoặc dùng 0 khi thêm mới."],
        ["Dòng chưa nhập giá", "Hệ thống bỏ qua và giữ nguyên dữ liệu hiện tại."],
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
                row.ItemName,
                row.UnitName,
                row.UnitPrice,
                row.VatRate,
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
