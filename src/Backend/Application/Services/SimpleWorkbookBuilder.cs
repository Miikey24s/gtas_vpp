using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Mô tả một worksheet đơn giản cho các file export nội bộ. Route nghiệp vụ chỉ
/// chuẩn bị cột và dữ liệu; lớp này sở hữu duy nhất phần đóng gói SpreadsheetML.
/// </summary>
public sealed record SimpleWorkbookSheet(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<object?>> Rows);

public static class SimpleWorkbookBuilder
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] Build(IReadOnlyList<SimpleWorkbookSheet> sheets)
    {
        ArgumentNullException.ThrowIfNull(sheets);
        if (sheets.Count == 0)
        {
            throw new ArgumentException("A workbook requires at least one worksheet.", nameof(sheets));
        }

        var normalizedSheets = sheets
            .Select((sheet, index) => NormalizeSheet(sheet, index))
            .ToArray();

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXml(archive, "[Content_Types].xml", ContentTypes(normalizedSheets.Length));
            WriteXml(archive, "_rels/.rels", RootRelationships());
            WriteXml(archive, "xl/workbook.xml", Workbook(normalizedSheets));
            WriteXml(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(normalizedSheets.Length));
            WriteXml(archive, "xl/styles.xml", Styles());

            for (var index = 0; index < normalizedSheets.Length; index++)
            {
                WriteXml(
                    archive,
                    $"xl/worksheets/sheet{index + 1}.xml",
                    Worksheet(normalizedSheets[index]));
            }
        }

        return stream.ToArray();
    }

    private static SimpleWorkbookSheet NormalizeSheet(SimpleWorkbookSheet sheet, int index)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (sheet.Headers.Count == 0)
        {
            throw new ArgumentException($"Worksheet {index + 1} requires at least one column.", nameof(sheet));
        }

        var name = string.IsNullOrWhiteSpace(sheet.Name) ? $"Sheet{index + 1}" : sheet.Name.Trim();
        name = string.Concat(name.Select(character => "[]:*?/\\".Contains(character) ? '_' : character));
        if (name.Length > 31)
        {
            name = name[..31];
        }

        return sheet with { Name = name };
    }

    private static XElement Worksheet(SimpleWorkbookSheet sheet)
    {
        var sheetRows = new List<XElement>
        {
            Row(sheet.Headers.Cast<object?>().ToArray(), 1)
        };
        sheetRows.AddRange(sheet.Rows.Select((row, index) => Row(row, index + 2)));

        var lastColumn = ColumnName(sheet.Headers.Count);
        return new XElement(Main + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheetViews",
                new XElement(Main + "sheetView", new XAttribute("workbookViewId", 0))),
            new XElement(Main + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Main + "sheetData", sheetRows),
            new XElement(Main + "autoFilter",
                new XAttribute("ref", $"A1:{lastColumn}{Math.Max(1, sheet.Rows.Count + 1)}")),
            new XElement(Main + "pageMargins",
                new XAttribute("left", "0.3"),
                new XAttribute("right", "0.3"),
                new XAttribute("top", "0.5"),
                new XAttribute("bottom", "0.5"),
                new XAttribute("header", "0.2"),
                new XAttribute("footer", "0.2")));
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
            return new XElement(Main + "c",
                new XAttribute("r", reference),
                header ? new XAttribute("s", 1) : null);
        }

        if (value is byte or short or int or long or decimal or double or float)
        {
            var number = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            return new XElement(Main + "c",
                new XAttribute("r", reference),
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

    private static XElement ContentTypes(int sheetCount)
        => new(ContentTypesNamespace + "Types",
            new XElement(ContentTypesNamespace + "Default",
                new XAttribute("Extension", "rels"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(ContentTypesNamespace + "Default",
                new XAttribute("Extension", "xml"),
                new XAttribute("ContentType", "application/xml")),
            new XElement(ContentTypesNamespace + "Override",
                new XAttribute("PartName", "/xl/workbook.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(ContentTypesNamespace + "Override",
                new XAttribute("PartName", "/xl/styles.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")),
            Enumerable.Range(1, sheetCount).Select(index =>
                new XElement(ContentTypesNamespace + "Override",
                    new XAttribute("PartName", $"/xl/worksheets/sheet{index}.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

    private static XElement RootRelationships()
        => new(PackageRelationships + "Relationships",
            new XElement(PackageRelationships + "Relationship",
                new XAttribute("Id", "rId1"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                new XAttribute("Target", "xl/workbook.xml")));

    private static XElement Workbook(IReadOnlyList<SimpleWorkbookSheet> sheets)
        => new(Main + "workbook",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheets",
                sheets.Select((sheet, index) =>
                    new XElement(Main + "sheet",
                        new XAttribute("name", sheet.Name),
                        new XAttribute("sheetId", index + 1),
                        new XAttribute(Relationships + "id", $"rId{index + 1}")))));

    private static XElement WorkbookRelationships(int sheetCount)
        => new(PackageRelationships + "Relationships",
            Enumerable.Range(1, sheetCount)
                .Select(index => new XElement(PackageRelationships + "Relationship",
                    new XAttribute("Id", $"rId{index}"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", $"worksheets/sheet{index}.xml")))
                .Append(new XElement(PackageRelationships + "Relationship",
                    new XAttribute("Id", $"rId{sheetCount + 1}"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                    new XAttribute("Target", "styles.xml"))));

    private static XElement Styles()
        => new(Main + "styleSheet",
            new XElement(Main + "fonts", new XAttribute("count", 2),
                new XElement(Main + "font",
                    new XElement(Main + "sz", new XAttribute("val", 11)),
                    new XElement(Main + "name", new XAttribute("val", "Aptos"))),
                new XElement(Main + "font",
                    new XElement(Main + "b"),
                    new XElement(Main + "sz", new XAttribute("val", 11)),
                    new XElement(Main + "name", new XAttribute("val", "Aptos")))),
            new XElement(Main + "fills", new XAttribute("count", 2),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125")))),
            new XElement(Main + "borders", new XAttribute("count", 1),
                new XElement(Main + "border",
                    new XElement(Main + "left"),
                    new XElement(Main + "right"),
                    new XElement(Main + "top"),
                    new XElement(Main + "bottom"),
                    new XElement(Main + "diagonal"))),
            new XElement(Main + "cellStyleXfs", new XAttribute("count", 1),
                new XElement(Main + "xf",
                    new XAttribute("numFmtId", 0),
                    new XAttribute("fontId", 0),
                    new XAttribute("fillId", 0),
                    new XAttribute("borderId", 0))),
            new XElement(Main + "cellXfs", new XAttribute("count", 2),
                new XElement(Main + "xf",
                    new XAttribute("numFmtId", 0),
                    new XAttribute("fontId", 0),
                    new XAttribute("fillId", 0),
                    new XAttribute("borderId", 0)),
                new XElement(Main + "xf",
                    new XAttribute("numFmtId", 0),
                    new XAttribute("fontId", 1),
                    new XAttribute("fillId", 0),
                    new XAttribute("borderId", 0),
                    new XAttribute("applyFont", 1))),
            new XElement(Main + "cellStyles", new XAttribute("count", 1),
                new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0))));

    private static void WriteXml(ZipArchive archive, string path, XElement document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }
}
