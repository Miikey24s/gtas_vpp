using System.Globalization;
using System.Reflection;
using gtas_vpp_shared.DTOs.Res.VPP;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Render một đơn hàng thành trang PDF A4 bằng QuestPDF (Community license).
/// Layout theo motif phiếu đơn đã duyệt trong Atlas: header có mã và trạng thái,
/// lưới thông tin, bảng mặt hàng và ghi chú đơn.
/// </summary>
public static class OrderPdfBuilder
{
    private const string FontFamily = "Poppins";
    private static readonly object InitLock = new();
    private static bool _initialized;

    // Xuất theo đơn không chứa đơn giá hoặc thành tiền: màn hình nhân viên
    // không hiển thị giá (luận văn §3.3.2.3).
    public static byte[] Build(VppRequestResDTO order)
    {
        EnsureInitialized();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(9.5f));

                page.Header().Column(header =>
                {
                    header.Item().Text("GTAS VPP — Phiếu chi tiết đơn văn phòng phẩm")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    header.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(order.VppCode ?? "-")
                            .FontSize(16).Bold();
                        row.ConstantItem(140).AlignRight().AlignMiddle()
                            .Text(order.StatusText).FontSize(10).SemiBold();
                    });
                    header.Item().PaddingTop(10).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    content.Item().Row(fields =>
                    {
                        void Field(RowDescriptor row, string label, string? value) =>
                            row.RelativeItem().Column(cell =>
                            {
                                cell.Item().Text(label).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                                cell.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value).FontSize(10).SemiBold();
                            });

                        Field(fields, "Kỳ", order.Period);
                        Field(fields, "Loại đơn", order.IsAdditionalOrder ? "Đơn bổ sung" : "Đơn thường");
                        Field(fields, "Người đặt", order.RequesterName);
                        Field(fields, "Phòng ban", order.DepartmentCode);
                        Field(fields, "Gửi lúc", order.SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")));
                    });

                    if (order.IsAdditionalOrder && !string.IsNullOrWhiteSpace(order.SupplementReason))
                    {
                        content.Item().PaddingTop(8).Text(text =>
                        {
                            text.Span("Lý do bổ sung: ").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            text.Span(order.SupplementReason).FontSize(9.5f);
                        });
                    }

                    content.Item().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn(1.4f);
                            columns.ConstantColumn(56);
                            columns.RelativeColumn(2.6f);
                        });

                        static IContainer HeaderCell(IContainer cell) => cell
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingVertical(5).PaddingHorizontal(4);
                        static IContainer BodyCell(IContainer cell) => cell
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(4).PaddingHorizontal(4);

                        table.Header(headerRow =>
                        {
                            headerRow.Cell().Element(HeaderCell).Text("#").FontSize(8).SemiBold();
                            headerRow.Cell().Element(HeaderCell).Text("Mặt hàng").FontSize(8).SemiBold();
                            headerRow.Cell().Element(HeaderCell).Text("Danh mục").FontSize(8).SemiBold();
                            headerRow.Cell().Element(HeaderCell).Text("Đơn vị").FontSize(8).SemiBold();
                            headerRow.Cell().Element(HeaderCell).AlignRight().Text("Số lượng").FontSize(8).SemiBold();
                            headerRow.Cell().Element(HeaderCell).Text("Ghi chú").FontSize(8).SemiBold();
                        });

                        var index = 0;
                        foreach (var item in order.Items)
                        {
                            index++;
                            table.Cell().Element(BodyCell).Text(index.ToString()).FontSize(8.5f);
                            table.Cell().Element(BodyCell).Column(cell =>
                            {
                                cell.Item().Text(item.VppName ?? "-").FontSize(9).SemiBold();
                                cell.Item().Text(item.VppCode ?? "-").FontSize(7).FontColor(Colors.Grey.Darken1);
                            });
                            table.Cell().Element(BodyCell).Text(item.CategoryName ?? "-").FontSize(8.5f);
                            table.Cell().Element(BodyCell).Text(item.UomName ?? "-").FontSize(8.5f);
                            table.Cell().Element(BodyCell).AlignRight().Text(item.Qty.ToString(CultureInfo.InvariantCulture)).FontSize(8.5f);
                            table.Cell().Element(BodyCell).Text(string.IsNullOrWhiteSpace(item.Description) ? "-" : item.Description).FontSize(8.5f);
                        }
                    });

                    content.Item().PaddingTop(8).Text($"Tổng cộng {order.TotalLines} mặt hàng · tổng số lượng {order.TotalQty}")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrWhiteSpace(order.Description))
                    {
                        content.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("Ghi chú đơn: ").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                            text.Span(order.Description).FontSize(9.5f);
                        });
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text(text =>
                    {
                        text.Span("Phiên bản ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.Span(order.RevisionNumber.ToString()).FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                    footer.RelativeItem().AlignRight().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.Span(" / ").FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                        text.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }
        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }
            QuestPDF.Settings.License = LicenseType.Community;
            // Poppins nhúng trong assembly để container Linux không cần font hệ thống.
            RegisterFont("gtas_vpp_be.Service.Fonts.Poppins-SemiBold.ttf");
            RegisterFont("gtas_vpp_be.Service.Fonts.Poppins-Bold.ttf");
            _initialized = true;
        }
    }

    private static void RegisterFont(string resourceName)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded font resource '{resourceName}' was not found.");
        FontManager.RegisterFont(stream);
    }
}
