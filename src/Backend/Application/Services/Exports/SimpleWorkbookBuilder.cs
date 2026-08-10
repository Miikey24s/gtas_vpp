using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Mô tả một worksheet đơn giản cho các file export nội bộ. Route nghiệp vụ chỉ
/// chuẩn bị cột và dữ liệu; lớp này sở hữu duy nhất phần đóng gói SpreadsheetML.
/// </summary>
public enum SimpleWorkbookCellFormat
{
    Text,
    Integer,
    Decimal
}

public enum SimpleWorkbookTheme
{
    Standard,
    VppRegistration
}

public enum SimpleWorkbookColumnRole
{
    Default,
    Label
}

public enum SimpleWorkbookRowStyle
{
    Default,
    Header,
    Title,
    Spacer,
    Section,
    Summary
}

public sealed record SimpleWorkbookColumn(
    string Header,
    double Width = 18,
    SimpleWorkbookCellFormat Format = SimpleWorkbookCellFormat.Text,
    SimpleWorkbookColumnRole Role = SimpleWorkbookColumnRole.Default);

public sealed record SimpleWorkbookDecorativeRow(
    IReadOnlyList<object?> Values,
    SimpleWorkbookRowStyle Style,
    double? Height = null);

public sealed record SimpleWorkbookSheetOptions(
    SimpleWorkbookTheme Theme = SimpleWorkbookTheme.Standard,
    IReadOnlyList<SimpleWorkbookDecorativeRow>? RowsBeforeHeader = null,
    IReadOnlyList<string>? MergedRanges = null,
    int FreezeRows = 1,
    bool ShowAutoFilter = true,
    bool ShowGridLines = true,
    string Orientation = "landscape");

public sealed record SimpleWorkbookSheet(
    string Name,
    IReadOnlyList<SimpleWorkbookColumn> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    SimpleWorkbookSheetOptions? Options = null)
{
    public IReadOnlyList<string> Headers => Columns.Select(column => column.Header).ToArray();
}

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
        if (sheet.Columns.Count == 0)
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
        var options = sheet.Options ?? new SimpleWorkbookSheetOptions();
        var leadingRows = options.RowsBeforeHeader ?? [];
        var sheetRows = leadingRows
            .Select((row, index) => Row(
                row.Values,
                sheet.Columns,
                index + 1,
                row.Style,
                options.Theme,
                row.Height))
            .ToList();
        var headerRow = leadingRows.Count + 1;
        sheetRows.Add(Row(
            sheet.Headers.Cast<object?>().ToArray(),
            sheet.Columns,
            headerRow,
            SimpleWorkbookRowStyle.Header,
            options.Theme));
        sheetRows.AddRange(sheet.Rows.Select((row, index) => Row(
            row,
            sheet.Columns,
            headerRow + index + 1,
            SimpleWorkbookRowStyle.Default,
            options.Theme)));

        var lastColumn = ColumnName(sheet.Columns.Count);
        var lastRow = Math.Max(headerRow, headerRow + sheet.Rows.Count);
        var frozenRows = Math.Clamp(options.FreezeRows, 0, lastRow);
        var mergedRanges = options.MergedRanges?
            .Where(range => !string.IsNullOrWhiteSpace(range))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        return new XElement(Main + "worksheet",
            new XAttribute(XNamespace.Xmlns + "r", Relationships),
            new XElement(Main + "sheetPr",
                new XElement(Main + "pageSetUpPr", new XAttribute("fitToPage", 1))),
            new XElement(Main + "dimension", new XAttribute("ref", $"A1:{lastColumn}{lastRow}")),
            new XElement(Main + "sheetViews",
                new XElement(Main + "sheetView",
                    new XAttribute("workbookViewId", 0),
                    new XAttribute("showGridLines", options.ShowGridLines ? 1 : 0),
                    frozenRows > 0
                        ? new XElement(Main + "pane",
                            new XAttribute("ySplit", frozenRows),
                            new XAttribute("topLeftCell", $"A{frozenRows + 1}"),
                            new XAttribute("activePane", "bottomLeft"),
                            new XAttribute("state", "frozen"))
                        : null)),
            new XElement(Main + "sheetFormatPr", new XAttribute("defaultRowHeight", 18)),
            new XElement(Main + "cols",
                sheet.Columns.Select((column, index) =>
                    new XElement(Main + "col",
                        new XAttribute("min", index + 1),
                        new XAttribute("max", index + 1),
                        new XAttribute("width", Math.Clamp(column.Width, 8, 60).ToString("0.##", CultureInfo.InvariantCulture)),
                        new XAttribute("customWidth", 1)))),
            new XElement(Main + "sheetData", sheetRows),
            options.ShowAutoFilter
                ? new XElement(Main + "autoFilter",
                    new XAttribute("ref", $"A{headerRow}:{lastColumn}{lastRow}"))
                : null,
            mergedRanges.Length > 0
                ? new XElement(Main + "mergeCells",
                    new XAttribute("count", mergedRanges.Length),
                    mergedRanges.Select(range =>
                        new XElement(Main + "mergeCell", new XAttribute("ref", range))))
                : null,
            new XElement(Main + "printOptions",
                new XAttribute("horizontalCentered", 0),
                new XAttribute("verticalCentered", 0)),
            new XElement(Main + "pageMargins",
                new XAttribute("left", "0.3"),
                new XAttribute("right", "0.3"),
                new XAttribute("top", "0.5"),
                new XAttribute("bottom", "0.5"),
                new XAttribute("header", "0.2"),
                new XAttribute("footer", "0.2")),
            new XElement(Main + "pageSetup",
                new XAttribute("paperSize", 9),
                new XAttribute("orientation", options.Orientation),
                new XAttribute("fitToWidth", 1),
                new XAttribute("fitToHeight", 0)));
    }

    private static XElement Row(
        IReadOnlyList<object?> values,
        IReadOnlyList<SimpleWorkbookColumn> columns,
        int rowNumber,
        SimpleWorkbookRowStyle rowStyle,
        SimpleWorkbookTheme theme,
        double? height = null)
        => new(Main + "row",
            new XAttribute("r", rowNumber),
            new XAttribute("ht", (height ?? DefaultRowHeight(rowStyle, theme)).ToString("0.##", CultureInfo.InvariantCulture)),
            new XAttribute("customHeight", 1),
            Enumerable.Range(0, columns.Count).Select(index => Cell(
                index < values.Count ? values[index] : null,
                index + 1,
                rowNumber,
                rowStyle,
                theme,
                columns[index])));

    private static XElement Cell(
        object? value,
        int column,
        int row,
        SimpleWorkbookRowStyle rowStyle,
        SimpleWorkbookTheme theme,
        SimpleWorkbookColumn columnDefinition)
    {
        var reference = $"{ColumnName(column)}{row}";
        var styleIndex = ResolveStyleIndex(theme, rowStyle, columnDefinition);
        if (value is null)
        {
            return new XElement(Main + "c",
                new XAttribute("r", reference),
                styleIndex > 0 ? new XAttribute("s", styleIndex) : null);
        }

        if (value is byte or short or int or long or decimal or double or float)
        {
            var number = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";
            return new XElement(Main + "c",
                new XAttribute("r", reference),
                styleIndex > 0 ? new XAttribute("s", styleIndex) : null,
                new XElement(Main + "v", number));
        }

        return new XElement(Main + "c",
            new XAttribute("r", reference),
            new XAttribute("t", "inlineStr"),
            styleIndex > 0 ? new XAttribute("s", styleIndex) : null,
            new XElement(Main + "is",
                new XElement(Main + "t",
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    value.ToString())));
    }

    private static double DefaultRowHeight(
        SimpleWorkbookRowStyle rowStyle,
        SimpleWorkbookTheme theme)
        => (theme, rowStyle) switch
        {
            (SimpleWorkbookTheme.VppRegistration, SimpleWorkbookRowStyle.Title) => 28,
            (SimpleWorkbookTheme.VppRegistration, SimpleWorkbookRowStyle.Spacer) => 10,
            (SimpleWorkbookTheme.VppRegistration, SimpleWorkbookRowStyle.Section) => 22,
            (SimpleWorkbookTheme.VppRegistration, SimpleWorkbookRowStyle.Summary) => 22,
            (SimpleWorkbookTheme.VppRegistration, SimpleWorkbookRowStyle.Header) => 28,
            (SimpleWorkbookTheme.VppRegistration, _) => 22,
            (_, SimpleWorkbookRowStyle.Header) => 24,
            _ => 18
        };

    private static int ResolveStyleIndex(
        SimpleWorkbookTheme theme,
        SimpleWorkbookRowStyle rowStyle,
        SimpleWorkbookColumn column)
    {
        if (theme == SimpleWorkbookTheme.Standard)
        {
            return rowStyle == SimpleWorkbookRowStyle.Header
                ? 1
                : column.Format switch
                {
                    SimpleWorkbookCellFormat.Integer => 2,
                    SimpleWorkbookCellFormat.Decimal => 3,
                    _ => 0
                };
        }

        return rowStyle switch
        {
            SimpleWorkbookRowStyle.Title => 8,
            SimpleWorkbookRowStyle.Spacer => 14,
            SimpleWorkbookRowStyle.Section => 9,
            SimpleWorkbookRowStyle.Summary => column.Format switch
            {
                SimpleWorkbookCellFormat.Integer => 11,
                SimpleWorkbookCellFormat.Decimal => 12,
                _ => 10
            },
            SimpleWorkbookRowStyle.Header => 5,
            _ when column.Role == SimpleWorkbookColumnRole.Label => 13,
            _ => column.Format switch
            {
                SimpleWorkbookCellFormat.Integer => 6,
                SimpleWorkbookCellFormat.Decimal => 7,
                _ => 4
            }
        };
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
    {
        var fonts = new[]
        {
            Font("Aptos", 11),
            Font("Aptos", 11, bold: true),
            Font("Times New Roman", 10),
            Font("Times New Roman", 10, bold: true),
            Font("Times New Roman", 18, bold: true, color: "FFFF0000", underline: true),
            Font("Times New Roman", 10, bold: true, color: "FFFFFFFF"),
            Font("Times New Roman", 10, color: "FF0000FF")
        };
        var fills = new[]
        {
            new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
            new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125"))),
            SolidFill("FFE8F4FB"),
            SolidFill("FFFFFF00"),
            SolidFill("FF0000FF"),
            SolidFill("FFFF0000"),
            SolidFill("FFD9D9D9"),
            SolidFill("FFFFF2CC")
        };
        var borders = new[]
        {
            Border(),
            Border(bottomColor: "FFB7D7EA"),
            Border(allColor: "FF000000")
        };
        var cellFormats = new[]
        {
            CellFormat(0, 0, 0),
            CellFormat(1, 2, 1, vertical: "center"),
            CellFormat(0, 0, 0, numberFormatId: 3),
            CellFormat(0, 0, 0, numberFormatId: 4),
            CellFormat(2, 6, 2, vertical: "center", wrapText: true),
            CellFormat(3, 3, 2, horizontal: "center", vertical: "center", wrapText: true),
            CellFormat(6, 6, 2, numberFormatId: 3, horizontal: "right", vertical: "center"),
            CellFormat(6, 6, 2, numberFormatId: 4, horizontal: "right", vertical: "center"),
            CellFormat(4, 0, 0, vertical: "center"),
            CellFormat(5, 4, 2, horizontal: "center", vertical: "center"),
            CellFormat(5, 5, 2, horizontal: "center", vertical: "center", wrapText: true),
            CellFormat(5, 5, 2, numberFormatId: 3, horizontal: "right", vertical: "center"),
            CellFormat(5, 5, 2, numberFormatId: 4, horizontal: "right", vertical: "center"),
            CellFormat(3, 7, 2, vertical: "center", wrapText: true),
            CellFormat(2, 0, 0)
        };

        return new XElement(Main + "styleSheet",
            new XElement(Main + "fonts", new XAttribute("count", fonts.Length), fonts),
            new XElement(Main + "fills", new XAttribute("count", fills.Length), fills),
            new XElement(Main + "borders", new XAttribute("count", borders.Length), borders),
            new XElement(Main + "cellStyleXfs", new XAttribute("count", 1),
                new XElement(Main + "xf",
                    new XAttribute("numFmtId", 0),
                    new XAttribute("fontId", 0),
                    new XAttribute("fillId", 0),
                    new XAttribute("borderId", 0))),
            new XElement(Main + "cellXfs", new XAttribute("count", cellFormats.Length), cellFormats),
            new XElement(Main + "cellStyles", new XAttribute("count", 1),
                new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", 0))));
    }

    private static XElement Font(
        string name,
        double size,
        bool bold = false,
        string? color = null,
        bool underline = false)
        => new(Main + "font",
            bold ? new XElement(Main + "b") : null,
            underline ? new XElement(Main + "u") : null,
            new XElement(Main + "sz", new XAttribute("val", size.ToString("0.##", CultureInfo.InvariantCulture))),
            color is null ? null : new XElement(Main + "color", new XAttribute("rgb", color)),
            new XElement(Main + "name", new XAttribute("val", name)));

    private static XElement SolidFill(string color)
        => new(Main + "fill",
            new XElement(Main + "patternFill",
                new XAttribute("patternType", "solid"),
                new XElement(Main + "fgColor", new XAttribute("rgb", color)),
                new XElement(Main + "bgColor", new XAttribute("indexed", 64))));

    private static XElement Border(string? bottomColor = null, string? allColor = null)
    {
        XElement Edge(string name, string? color) => color is null
            ? new XElement(Main + name)
            : new XElement(Main + name,
                new XAttribute("style", "thin"),
                new XElement(Main + "color", new XAttribute("rgb", color)));

        return new XElement(Main + "border",
            Edge("left", allColor),
            Edge("right", allColor),
            Edge("top", allColor),
            Edge("bottom", allColor ?? bottomColor),
            new XElement(Main + "diagonal"));
    }

    private static XElement CellFormat(
        int fontId,
        int fillId,
        int borderId,
        int numberFormatId = 0,
        string? horizontal = null,
        string? vertical = null,
        bool wrapText = false)
    {
        var usesAlignment = horizontal is not null || vertical is not null || wrapText;
        return new XElement(Main + "xf",
            new XAttribute("numFmtId", numberFormatId),
            new XAttribute("fontId", fontId),
            new XAttribute("fillId", fillId),
            new XAttribute("borderId", borderId),
            fontId > 0 ? new XAttribute("applyFont", 1) : null,
            fillId > 0 ? new XAttribute("applyFill", 1) : null,
            borderId > 0 ? new XAttribute("applyBorder", 1) : null,
            numberFormatId > 0 ? new XAttribute("applyNumberFormat", 1) : null,
            usesAlignment ? new XAttribute("applyAlignment", 1) : null,
            usesAlignment
                ? new XElement(Main + "alignment",
                    horizontal is null ? null : new XAttribute("horizontal", horizontal),
                    vertical is null ? null : new XAttribute("vertical", vertical),
                    wrapText ? new XAttribute("wrapText", 1) : null)
                : null);
    }

    private static void WriteXml(ZipArchive archive, string path, XElement document)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }
}
