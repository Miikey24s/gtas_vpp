using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Builds a two-sheet XLSX workbook for a single order without any external
/// spreadsheet library, mirroring the SpreadsheetML approach of
/// <see cref="ReportWorkbookBuilder"/>.
/// </summary>
public static class OrderWorkbookBuilder
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Build(VppRequestResDTO order)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", ContentTypes());
            WriteXml(archive, "_rels/.rels", RootRelationships());
            WriteXml(archive, "xl/workbook.xml", Workbook());
            WriteXml(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteXml(archive, "xl/styles.xml", Styles());
            WriteXml(archive, "xl/worksheets/sheet1.xml", OrderSheet(order));
            WriteXml(archive, "xl/worksheets/sheet2.xml", ItemsSheet(order));
        }
        return stream.ToArray();
    }

    private static XElement OrderSheet(VppRequestResDTO order)
    {
        // Xuất theo đơn không chứa đơn giá hoặc thành tiền: màn hình nhân viên
        // không hiển thị giá (luận văn §3.3.2.3); giá chỉ tồn tại trong phạm vi
        // báo cáo/chốt kỳ với quyền tương ứng.
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Order sheet" },
            new object?[] { "Order code", order.VppCode },
            new object?[] { "Period", order.Period },
            new object?[] { "Order type", order.IsAdditionalOrder ? "Additional" : "Regular" },
            new object?[] { "Status", order.StatusText },
            new object?[] { "Requester", order.RequesterName },
            new object?[] { "Department", order.DepartmentCode },
            new object?[] { "Submitted at", order.SubmittedDate?.ToString("HH:mm dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")) ?? "-" },
            new object?[] { "Revision", order.RevisionNumber },
            new object?[] { "Total lines", order.TotalLines },
            new object?[] { "Total quantity", order.TotalQty },
            new object?[] { "Order note", string.IsNullOrWhiteSpace(order.Description) ? "-" : order.Description }
        };
        if (order.IsAdditionalOrder)
        {
            rows.Add(new object?[] { "Supplement reason", order.SupplementReason ?? "-" });
        }
        return Sheet(new[] { "Field", "Value" }, rows);
    }

    private static XElement ItemsSheet(VppRequestResDTO order)
    {
        var rows = order.Items
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
            .ToList();
        return Sheet(new[] { "#", "Item code", "Item name", "Category", "Unit", "Quantity", "Note" }, rows);
    }

    private static XElement Sheet(
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
            Enumerable.Range(1, 2).Select(index => new XElement(ContentTypesNamespace + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

    private static XElement RootRelationships() =>
        new(PackageRelationships + "Relationships",
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml")));

    private static XElement Workbook() =>
        new(Main + "workbook",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheets",
                new XElement(Main + "sheet", new XAttribute("name", "Order"), new XAttribute("sheetId", 1), new XAttribute(Relationships + "id", "rId1")),
                new XElement(Main + "sheet", new XAttribute("name", "Items"), new XAttribute("sheetId", 2), new XAttribute(Relationships + "id", "rId2"))));

    private static XElement WorkbookRelationships() =>
        new(PackageRelationships + "Relationships",
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId2"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet2.xml")),
            new XElement(PackageRelationships + "Relationship", new XAttribute("Id", "rId3"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"), new XAttribute("Target", "styles.xml")));

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
