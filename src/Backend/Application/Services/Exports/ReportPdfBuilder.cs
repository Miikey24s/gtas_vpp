using System.Globalization;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Xuất bản PDF tóm tắt cho báo cáo. Excel giữ dữ liệu chi tiết nhiều sheet;
/// PDF ưu tiên KPI, đối chiếu chốt kỳ và hai bảng evidence để đọc/in nhanh.
/// </summary>
public static class ReportPdfBuilder
{
    public static byte[] Build(ReportSummaryResDTO summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
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
                    .FontSize(8.5f)
                    .FontColor(VppPdfTheme.PrimaryText));

                page.Header().Column(header =>
                {
                    header.Item().Text("GTAS VPP — Báo cáo tổng hợp văn phòng phẩm")
                        .FontSize(8).FontColor(VppPdfTheme.SecondaryText);
                    header.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text(PeriodLabel(summary)).FontSize(16).Bold();
                        row.ConstantItem(150).AlignRight().Text(ScopeLabel(summary.Scope))
                            .FontSize(9).SemiBold().FontColor(VppPdfTheme.Accent);
                    });
                    header.Item().PaddingTop(8).LineHorizontal(1).LineColor(VppPdfTheme.Border);
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Spacing(10);
                    content.Item().Row(metrics =>
                    {
                        Metric(metrics, "Tổng đơn", summary.TotalOrders.ToString("N0", culture));
                        Metric(metrics, "Tổng mặt hàng", summary.TotalLines.ToString("N0", culture));
                        Metric(metrics, "Tổng số lượng", summary.TotalQuantity.ToString("N0", culture));
                        Metric(metrics, "Tổng giá trị", $"{summary.TotalAmount:N0} đ");
                    });

                    if (summary.SettlementId.HasValue)
                    {
                        content.Item().Border(1).BorderColor(VppPdfTheme.Border)
                            .Background(VppPdfTheme.HeaderBackground)
                            .Padding(8).Row(row =>
                            {
                                Metric(row, "Phiên bản chốt kỳ", summary.SettlementRevisionNumber?.ToString(culture) ?? "-");
                                Metric(row, "Nhà cung cấp chính", summary.SettlementPrimarySupplierName ?? "-");
                                Metric(row, "Tổng giá trị chốt kỳ", FormatAmount(summary.SettlementGrandTotal, culture));
                                Metric(row, "Chênh lệch", FormatAmount(summary.SettlementVariance, culture));
                            });
                    }

                    content.Item().Text("Tổng hợp theo phòng ban").FontSize(11).SemiBold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.8f);
                        });
                        table.Header(header =>
                        {
                            Header(header, "#");
                            Header(header, "Phòng ban");
                            Header(header, "Số đơn", true);
                            Header(header, "Số lượng", true);
                            Header(header, "Tổng giá trị", true);
                        });

                        var index = 0;
                        foreach (var item in summary.DepartmentBreakdown)
                        {
                            index++;
                            Body(table, index.ToString(culture));
                            Body(table, item.Code);
                            Body(table, item.OrderCount.ToString("N0", culture), true);
                            Body(table, item.TotalQuantity.ToString("N0", culture), true);
                            Body(table, $"{item.TotalAmount:N0} đ", true);
                        }
                    });

                    content.Item().Text("Mặt hàng được yêu cầu nhiều nhất").FontSize(11).SemiBold();
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.RelativeColumn(1.7f);
                            columns.RelativeColumn(3.2f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.7f);
                        });
                        table.Header(header =>
                        {
                            Header(header, "#");
                            Header(header, "Mã mặt hàng");
                            Header(header, "Tên mặt hàng");
                            Header(header, "Số lượng", true);
                            Header(header, "Tổng giá trị", true);
                        });

                        var index = 0;
                        foreach (var item in summary.TopProducts)
                        {
                            index++;
                            Body(table, index.ToString(culture));
                            Body(table, item.ProductCode);
                            Body(table, item.ProductName);
                            Body(table, item.TotalQuantity.ToString("N0", culture), true);
                            Body(table, $"{item.TotalAmount:N0} đ", true);
                        }
                    });

                    if (summary.StatusBreakdown.Count > 0)
                    {
                        var statuses = string.Join(" · ", summary.StatusBreakdown.Select(item =>
                            $"{VppStatusContract.GetText(item.Status, culture: culture)}: {item.OrderCount:N0}"));
                        content.Item().Text($"Cơ cấu trạng thái: {statuses}")
                            .FontSize(7.5f).FontColor(VppPdfTheme.SecondaryText);
                    }
                });

                page.Footer().Row(footer =>
                {
                    footer.RelativeItem().Text($"Tạo lúc {summary.GeneratedAt.ToUniversalTime():HH:mm dd/MM/yyyy} UTC")
                        .FontSize(7).FontColor(VppPdfTheme.SecondaryText);
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

    private static void Metric(RowDescriptor row, string label, string value)
        => row.RelativeItem().Column(cell =>
        {
            cell.Item().Text(label).FontSize(7).FontColor(VppPdfTheme.SecondaryText);
            cell.Item().Text(value).FontSize(10).SemiBold();
        });

    private static void Header(TableCellDescriptor header, string text, bool right = false)
    {
        var cell = header.Cell().Element(VppPdfTheme.TableHeaderCell);
        if (right) cell = cell.AlignRight();
        cell.Text(text).SemiBold();
    }

    private static void Body(TableDescriptor table, string? text, bool right = false)
    {
        var cell = table.Cell().Element(VppPdfTheme.TableBodyCell);
        if (right) cell = cell.AlignRight();
        cell.Text(string.IsNullOrWhiteSpace(text) ? "-" : text);
    }

    private static string PeriodLabel(ReportSummaryResDTO summary)
        => summary.Year.HasValue
            ? summary.Month.HasValue ? $"Kỳ {summary.Month:00}/{summary.Year}" : $"Năm {summary.Year}"
            : "Tất cả kỳ";

    private static string ScopeLabel(string scope) => scope switch
    {
        ReportScopes.Own => "Cá nhân",
        ReportScopes.Department => "Phòng ban",
        ReportScopes.All => "Toàn doanh nghiệp",
        _ => scope
    };

    private static string FormatAmount(decimal? value, CultureInfo culture)
        => value.HasValue ? $"{value.Value.ToString("N0", culture)} đ" : "-";
}
