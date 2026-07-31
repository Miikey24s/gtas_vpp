using System.Globalization;
using gtas_vpp_be.Model.VPP;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace gtas_vpp_be.Service.Services;

public static class SettlementPdfBuilder
{
    public static byte[] Build(Settlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        VppPdfFontRegistry.EnsureInitialized();

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(style => style
                    .FontFamily(VppPdfFontRegistry.FontFamily)
                    .FontSize(8.5f));

                page.Header().Column(header =>
                {
                    header.Item().Text("GTAS VPP — Biên bản chốt kỳ văn phòng phẩm")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    header.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text($"Kỳ {settlement.Month:00}/{settlement.Year}")
                            .FontSize(16).Bold();
                        row.ConstantItem(150).AlignRight().Text($"Phiên bản {settlement.RevisionNumber}")
                            .FontSize(9).SemiBold();
                    });
                    header.Item().PaddingTop(8).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Item().Row(fields =>
                    {
                        static void Field(RowDescriptor row, string label, string? value)
                            => row.RelativeItem().Column(cell =>
                            {
                                cell.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
                                cell.Item().Text(string.IsNullOrWhiteSpace(value) ? "-" : value)
                                    .FontSize(9).SemiBold();
                            });

                        Field(fields, "Nhà cung cấp", settlement.PrimarySupplierName);
                        Field(fields, "Bảng giá", $"{settlement.PriceListName} · v{settlement.PriceListVersion}");
                        Field(fields, "Ngày chốt", settlement.ConfirmedAtUtc.ToString("HH:mm dd/MM/yyyy", culture));
                    });

                    content.Item().PaddingTop(10).Row(totals =>
                    {
                        static void Amount(RowDescriptor row, string label, decimal value, string currency)
                            => row.RelativeItem().Column(cell =>
                            {
                                cell.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
                                cell.Item().Text($"{value:N0} {currency}").FontSize(10).SemiBold();
                            });

                        Amount(totals, "Tạm tính", settlement.Subtotal, settlement.CurrencyCode);
                        Amount(totals, "Thuế VAT", settlement.VatAmount, settlement.CurrencyCode);
                        Amount(totals, "Tổng giá trị", settlement.GrandTotal, settlement.CurrencyCode);
                    });

                    content.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(3.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        static IContainer HeaderCell(IContainer cell) => cell
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingVertical(4).PaddingHorizontal(3);
                        static IContainer BodyCell(IContainer cell) => cell
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(3.5f).PaddingHorizontal(3);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("#").SemiBold();
                            header.Cell().Element(HeaderCell).Text("Mặt hàng").SemiBold();
                            header.Cell().Element(HeaderCell).Text("ĐVT").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("SL").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá").SemiBold();
                            header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền").SemiBold();
                        });

                        var index = 0;
                        foreach (var item in settlement.Items.OrderBy(item => item.VppName))
                        {
                            index++;
                            table.Cell().Element(BodyCell).Text(index.ToString(culture));
                            table.Cell().Element(BodyCell).Column(cell =>
                            {
                                cell.Item().Text(item.VppName).SemiBold();
                                cell.Item().Text(item.VppCode).FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                            });
                            table.Cell().Element(BodyCell).Text(item.UomName);
                            table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("N0", culture));
                            table.Cell().Element(BodyCell).AlignRight().Text(item.NetUnitPrice.ToString("N0", culture));
                            table.Cell().Element(BodyCell).AlignRight().Text(item.GrossAmount.ToString("N0", culture));
                        }
                    });

                    if (settlement.IsCorrection && !string.IsNullOrWhiteSpace(settlement.CorrectionReason))
                    {
                        content.Item().PaddingTop(8).Text(text =>
                        {
                            text.Span("Lý do hiệu chỉnh: ").FontColor(Colors.Grey.Darken1);
                            text.Span(settlement.CorrectionReason);
                        });
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"Mã đối chiếu: {settlement.InputHash}")
                        .FontSize(6.5f).FontColor(Colors.Grey.Darken1);
                    footer.ConstantItem(80).AlignRight().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(7);
                        text.Span(" / ").FontSize(7);
                        text.TotalPages().FontSize(7);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}
