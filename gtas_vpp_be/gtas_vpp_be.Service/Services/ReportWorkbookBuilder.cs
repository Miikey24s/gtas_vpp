using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using gtas_vpp_shared.DTOs.Res.Reports;

namespace gtas_vpp_be.Service.Services;

public sealed record ReportWorkbookItem(
    string Period,
    string DepartmentCode,
    int RequesterUserId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal NetUnitPrice,
    decimal VatRate,
    decimal NetAmount,
    decimal VatAmount,
    decimal CommercialAdjustment,
    decimal GrossAmount,
    string SupplierName,
    string PriceBook,
    bool IsSupplierException);

public static class ReportWorkbookBuilder
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Build(
        ReportSummaryResDTO summary,
        IReadOnlyList<ReportWorkbookItem> items)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", ContentTypes());
            WriteXml(archive, "_rels/.rels", RootRelationships());
            WriteXml(archive, "xl/workbook.xml", Workbook());
            WriteXml(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteXml(archive, "xl/styles.xml", Styles());
            WriteXml(archive, "xl/worksheets/sheet1.xml", SummarySheet(summary));
            WriteXml(archive, "xl/worksheets/sheet2.xml", ItemsSheet(items));
            WriteXml(archive, "xl/worksheets/sheet3.xml", DepartmentsSheet(summary));
            WriteXml(archive, "xl/worksheets/sheet4.xml", TrendSheet(summary));
            WriteXml(archive, "xl/worksheets/sheet5.xml", TopProductsSheet(summary));
        }
        return stream.ToArray();
    }

    private static XElement SummarySheet(ReportSummaryResDTO summary)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Settlement-aware report" },
            new object?[] { "Scope", summary.Scope },
            new object?[] { "Period", summary.Year.HasValue && summary.Month.HasValue ? $"{summary.Month:00}/{summary.Year}" : "All periods" },
            new object?[] { "Generated UTC", summary.GeneratedAt.ToUniversalTime().ToString("O") },
            new object?[] { "Total orders", summary.TotalOrders },
            new object?[] { "Total lines", summary.TotalLines },
            new object?[] { "Total quantity", summary.TotalQuantity },
            new object?[] { "Total amount (VND)", summary.TotalAmount },
            new object?[] { "Settlement reconciled", summary.IsSettlementReconciled ? "YES" : "NO / NOT SETTLED" },
            new object?[] { "Settlement revision", summary.SettlementRevisionNumber },
            new object?[] { "Primary supplier", summary.SettlementPrimarySupplierName },
            new object?[] { "Settlement grand total", summary.SettlementGrandTotal },
            new object?[] { "Allocation total", summary.SettlementAllocationTotal },
            new object?[] { "Settlement variance", summary.SettlementVariance }
        };
        return Worksheet("Summary", ["Metric", "Value"], rows);
    }

    private static XElement ItemsSheet(IReadOnlyList<ReportWorkbookItem> items)
    {
        var rows = items.Select(item => (IReadOnlyList<object?>)[
            item.Period,
            item.DepartmentCode,
            item.RequesterUserId,
            item.ProductCode,
            item.ProductName,
            item.Quantity,
            item.NetUnitPrice,
            item.VatRate,
            item.NetAmount,
            item.VatAmount,
            item.CommercialAdjustment,
            item.GrossAmount,
            item.SupplierName,
            item.PriceBook,
            item.IsSupplierException ? "YES" : "NO"
        ]).ToList();
        return Worksheet("Items", [
            "Period", "Department", "RequesterUserId", "ProductCode", "ProductName",
            "Quantity", "NetUnitPrice", "VatRate", "NetAmount", "VatAmount",
            "CommercialAdjustment", "GrossAmount", "Supplier", "PriceBook", "SupplierException"
        ], rows);
    }

    private static XElement DepartmentsSheet(ReportSummaryResDTO summary)
        => Worksheet("Departments", ["Department", "Orders", "Quantity", "AmountVND"],
            summary.DepartmentBreakdown.Select(item => (IReadOnlyList<object?>)[
                item.DepartmentCode, item.OrderCount, item.TotalQuantity, item.TotalAmount]).ToList());

    private static XElement TrendSheet(ReportSummaryResDTO summary)
        => Worksheet("Trend", ["Period", "Orders", "Quantity", "AmountVND"],
            summary.PeriodTrend.Select(item => (IReadOnlyList<object?>)[
                item.Period, item.OrderCount, item.TotalQuantity, item.TotalAmount]).ToList());

    private static XElement TopProductsSheet(ReportSummaryResDTO summary)
        => Worksheet("TopProducts", ["ProductCode", "ProductName", "Quantity", "AmountVND"],
            summary.TopProducts.Select(item => (IReadOnlyList<object?>)[
                item.ProductCode, item.ProductName, item.TotalQuantity, item.TotalAmount]).ToList());

    private static XElement Worksheet(
        string name,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var sheetRows = new List<XElement>
        {
            Row(headers.Cast<object?>().ToList(), 1)
        };
        sheetRows.AddRange(rows.Select((row, index) => Row(row, index + 2)));
        var lastColumn = ColumnName(headers.Count);
        return new XElement(Main + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0))),
            new XElement(Main + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Main + "sheetData", sheetRows),
            new XElement(Main + "autoFilter", new XAttribute("ref", $"A1:{lastColumn}{Math.Max(1, rows.Count + 1)}")),
            new XElement(Main + "pageMargins",
                new XAttribute("left", "0.3"), new XAttribute("right", "0.3"),
                new XAttribute("top", "0.5"), new XAttribute("bottom", "0.5"),
                new XAttribute("header", "0.2"), new XAttribute("footer", "0.2")));
    }

    private static XElement Row(IReadOnlyList<object?> values, int rowNumber)
        => new(Main + "row",
            new XAttribute("r", rowNumber),
            values.Select((value, index) => Cell(value, index + 1, rowNumber, rowNumber == 1)));

    private static XElement Cell(object? value, int column, int row, bool header)
    {
        var reference = $"{ColumnName(column)}{row}";
        if (value is null)
        {
            return new XElement(Main + "c", new XAttribute("r", reference),
                header ? new XAttribute("s", 1) : null);
        }
        if (value is byte or short or int or long or decimal or double or float)
        {
            var number = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            return new XElement(Main + "c", new XAttribute("r", reference),
                header ? new XAttribute("s", 1) : null,
                new XElement(Main + "v", number));
        }
        return new XElement(Main + "c",
            new XAttribute("r", reference),
            new XAttribute("t", "inlineStr"),
            header ? new XAttribute("s", 1) : null,
            new XElement(Main + "is", new XElement(Main + "t", value.ToString())));
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }

    private static XElement ContentTypes() =>
        new(ContentTypesNamespace + "Types",
            new XElement(ContentTypesNamespace + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(ContentTypesNamespace + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            new XElement(ContentTypesNamespace + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(ContentTypesNamespace + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")),
            Enumerable.Range(1, 5).Select(index => new XElement(ContentTypesNamespace + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

    private static XElement RootRelationships() =>
        new(PackageRelationships + "Relationships",
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml")));

    private static XElement Workbook() =>
        new(Main + "workbook",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheets",
                new XElement(Main + "sheet", new XAttribute("name", "Summary"), new XAttribute("sheetId", 1), new XAttribute(Relationships + "id", "rId1")),
                new XElement(Main + "sheet", new XAttribute("name", "Items"), new XAttribute("sheetId", 2), new XAttribute(Relationships + "id", "rId2")),
                new XElement(Main + "sheet", new XAttribute("name", "Departments"), new XAttribute("sheetId", 3), new XAttribute(Relationships + "id", "rId3")),
                new XElement(Main + "sheet", new XAttribute("name", "Trend"), new XAttribute("sheetId", 4), new XAttribute(Relationships + "id", "rId4")),
                new XElement(Main + "sheet", new XAttribute("name", "TopProducts"), new XAttribute("sheetId", 5), new XAttribute(Relationships + "id", "rId5"))));

    private static XElement WorkbookRelationships() =>
        new(PackageRelationships + "Relationships",
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId2"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet2.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId3"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet3.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId4"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet4.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId5"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet5.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId6"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"), new XAttribute("Target", "styles.xml")));

    private static XElement Styles() =>
        new(Main + "styleSheet",
            new XElement(Main + "fonts", new XAttribute("count", 2),
                new XElement(Main + "font", new XElement(Main + "sz", new XAttribute("val", 11)), new XElement(Main + "name", new XAttribute("val", "Aptos"))),
                new XElement(Main + "font", new XElement(Main + "b"), new XElement(Main + "sz", new XAttribute("val", 11)), new XElement(Main + "name", new XAttribute("val", "Aptos")))),
            new XElement(Main + "fills", new XAttribute("count", 2), new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))), new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125")))),
            new XElement(Main + "borders", new XAttribute("count", 1), new XElement(Main + "border", new XElement(Main + "left"), new XElement(Main + "right"), new XElement(Main + "top"), new XElement(Main + "bottom"), new XElement(Main + "diagonal"))),
            new XElement(Main + "cellStyleXfs", new XAttribute("count", 1), new XElement(Main + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
            new XElement(Main + "cellXfs", new XAttribute("count", 2),
                new XElement(Main + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0)),
                new XElement(Main + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 1), new XAttribute("fillId", 0), new XAttribute("borderId", 0), new XAttribute("applyFont", 1))),
            new XElement(Main + "cellStyles", new XAttribute("count", 1), new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0))));

    private static void WriteXml(ZipArchive archive, string path, XElement document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }
}
