using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_shared.DTOs.Res.Library;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace gtas_vpp_be.Service.Services;

public sealed class PriceListImportParsedFile
{
    public string FileHash { get; init; } = string.Empty;
    public string FileFormat { get; init; } = string.Empty;
    public List<PriceListImportColumnMappingResDTO> ColumnMappings { get; init; } = [];
    public List<PriceListImportParsedRow> Rows { get; init; } = [];
    public List<PriceListImportIssueResDTO> GlobalIssues { get; init; } = [];
}

public sealed class PriceListImportParsedRow
{
    public int RowNumber { get; init; }
    public string? ItemCode { get; init; }
    public string? SupplierSku { get; init; }
    public string? ItemName { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? VatRate { get; init; }
    public decimal? MinimumOrderQuantity { get; init; }
    public int? LeadTimeDays { get; init; }
    public bool? IsDefault { get; init; }
    public string? Note { get; init; }
    public List<PriceListImportIssueResDTO> Issues { get; init; } = [];
}

public sealed class PriceListImportFileParser
{
    public const long MaximumFileSizeBytes = 5 * 1024 * 1024;
    public const int MaximumRows = 5_000;
    private const long MaximumExpandedSizeBytes = 20 * 1024 * 1024;
    private const int MaximumArchiveEntries = 200;
    private static readonly string[] RequiredFields = ["ItemCode", "UnitPrice"];
    private static readonly HashSet<string> SupportedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "ItemCode",
        "SupplierSku",
        "ItemName",
        "UnitPrice",
        "VatRate",
        "MinimumOrderQuantity",
        "LeadTimeDays",
        "IsDefault",
        "Note"
    };

    private static readonly IReadOnlyDictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["itemcode"] = "ItemCode",
            ["mavattu"] = "ItemCode",
            ["mamathang"] = "ItemCode",
            ["mahang"] = "ItemCode",
            ["suppliersku"] = "SupplierSku",
            ["mancc"] = "SupplierSku",
            ["mahangncc"] = "SupplierSku",
            ["itemname"] = "ItemName",
            ["tenmathang"] = "ItemName",
            ["tenhang"] = "ItemName",
            ["unitprice"] = "UnitPrice",
            ["dongia"] = "UnitPrice",
            ["gia"] = "UnitPrice",
            ["vatrate"] = "VatRate",
            ["vat"] = "VatRate",
            ["thuevat"] = "VatRate",
            ["minimumorderquantity"] = "MinimumOrderQuantity",
            ["moq"] = "MinimumOrderQuantity",
            ["soluongtoithieu"] = "MinimumOrderQuantity",
            ["leadtimedays"] = "LeadTimeDays",
            ["songaygiao"] = "LeadTimeDays",
            ["thoigiangiao"] = "LeadTimeDays",
            ["isdefault"] = "IsDefault",
            ["macdinh"] = "IsDefault",
            ["note"] = "Note",
            ["ghichu"] = "Note"
        };

    public async Task<PriceListImportParsedFile> ParseAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken = default)
        => await ParseAsync(source, fileName, null, cancellationToken);

    public async Task<PriceListImportParsedFile> ParseAsync(
        Stream source,
        string fileName,
        IReadOnlyDictionary<int, string>? columnMappings,
        CancellationToken cancellationToken = default)
    {
        var sourceFile = await ReadSourceAsync(source, fileName, cancellationToken);
        return BuildParsedFile(
            sourceFile.Rows,
            sourceFile.FileFormat,
            sourceFile.FileHash,
            columnMappings);
    }

    public async Task<PriceListImportAnalysisResDTO> AnalyzeAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var sourceFile = await ReadSourceAsync(source, fileName, cancellationToken);
        var table = ResolveTable(sourceFile.Rows);
        var columns = new List<PriceListImportSourceColumnResDTO>();
        var issues = new List<PriceListImportIssueResDTO>();
        var suggestedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < table.Header.Cells.Count; index++)
        {
            var sourceHeader = table.Header.Cells[index]?.Trim();
            if (string.IsNullOrWhiteSpace(sourceHeader))
            {
                continue;
            }

            HeaderAliases.TryGetValue(NormalizeToken(sourceHeader), out var suggestedTarget);
            if (!string.IsNullOrWhiteSpace(suggestedTarget) && !suggestedTargets.Add(suggestedTarget))
            {
                issues.Add(Issue(
                    table.Header.RowNumber,
                    "Warning",
                    "DUPLICATE_SUGGESTED_COLUMN",
                    $"Có nhiều cột cùng được gợi ý là {FieldLabel(suggestedTarget)}. Vui lòng chọn lại.",
                    sourceHeader));
                suggestedTarget = null;
            }

            columns.Add(new PriceListImportSourceColumnResDTO
            {
                ColumnIndex = index,
                SourceColumn = sourceHeader,
                SuggestedTargetField = suggestedTarget,
                SampleValues = table.DataRows
                    .Select(row => index < row.Cells.Count ? row.Cells[index]?.Trim() : null)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .Select(value => value!)
                    .ToList()
            });
        }

        foreach (var required in RequiredFields)
        {
            if (!columns.Any(column => string.Equals(
                    column.SuggestedTargetField,
                    required,
                    StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(Issue(
                    table.Header.RowNumber,
                    "Warning",
                    "MAPPING_REQUIRED",
                    $"Chưa nhận diện được cột {FieldLabel(required)}. Vui lòng chọn cột tương ứng.",
                    required));
            }
        }

        return new PriceListImportAnalysisResDTO
        {
            FileName = sourceFile.SafeFileName,
            FileHash = sourceFile.FileHash,
            FileFormat = sourceFile.FileFormat,
            TotalRows = table.DataRows.Count,
            CanPreviewAutomatically = RequiredFields.All(required => columns.Any(column => string.Equals(
                column.SuggestedTargetField,
                required,
                StringComparison.OrdinalIgnoreCase))),
            AiSuggestionsAvailable = false,
            Columns = columns,
            Issues = issues
        };
    }

    private static PriceListImportParsedFile BuildParsedFile(
        IReadOnlyList<SourceTableRow> sourceRows,
        string format,
        string hash,
        IReadOnlyDictionary<int, string>? columnMappings)
    {
        var table = ResolveTable(sourceRows);
        var mappedColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mappings = new List<PriceListImportColumnMappingResDTO>();
        var globalIssues = new List<PriceListImportIssueResDTO>();
        for (var index = 0; index < table.Header.Cells.Count; index++)
        {
            var sourceHeader = table.Header.Cells[index]?.Trim();
            if (string.IsNullOrWhiteSpace(sourceHeader))
            {
                continue;
            }

            string? customTarget = null;
            var isCustom = columnMappings is not null && columnMappings.TryGetValue(index, out customTarget);
            var targetField = isCustom
                ? customTarget?.Trim()
                : HeaderAliases.GetValueOrDefault(NormalizeToken(sourceHeader));
            if (string.IsNullOrWhiteSpace(targetField))
            {
                continue;
            }
            if (!SupportedFields.Contains(targetField))
            {
                globalIssues.Add(Issue(
                    table.Header.RowNumber,
                    "Error",
                    "UNSUPPORTED_MAPPING",
                    $"Cột {sourceHeader} đang ánh xạ đến trường không được hỗ trợ.",
                    sourceHeader));
                continue;
            }

            var canonicalTarget = SupportedFields.First(field => string.Equals(
                field,
                targetField,
                StringComparison.OrdinalIgnoreCase));
            if (mappedColumns.ContainsKey(canonicalTarget))
            {
                globalIssues.Add(Issue(
                    table.Header.RowNumber,
                    "Error",
                    "DUPLICATE_COLUMN",
                    $"Có nhiều cột cùng ánh xạ vào {FieldLabel(canonicalTarget)}.",
                    sourceHeader));
                continue;
            }

            mappedColumns[canonicalTarget] = index;
            mappings.Add(new PriceListImportColumnMappingResDTO
            {
                SourceColumnIndex = index,
                SourceColumn = sourceHeader,
                TargetField = canonicalTarget,
                IsRequired = RequiredFields.Contains(canonicalTarget, StringComparer.OrdinalIgnoreCase),
                IsCustom = isCustom
            });
        }

        foreach (var required in RequiredFields)
        {
            if (!mappedColumns.ContainsKey(required))
            {
                globalIssues.Add(Issue(
                    table.Header.RowNumber,
                    "Error",
                    "MISSING_REQUIRED_COLUMN",
                    $"Thiếu cột bắt buộc {FieldLabel(required)}.",
                    required));
            }
        }

        var rows = table.DataRows.Select(row => ParseRow(row, mappedColumns)).ToList();
        return new PriceListImportParsedFile
        {
            FileHash = hash,
            FileFormat = format,
            ColumnMappings = mappings,
            Rows = rows,
            GlobalIssues = globalIssues
        };
    }

    private static TableRows ResolveTable(IReadOnlyList<SourceTableRow> sourceRows)
    {
        var headerRow = sourceRows.FirstOrDefault(row => row.Cells.Any(cell => !string.IsNullOrWhiteSpace(cell)));
        if (headerRow is null)
        {
            throw new BusinessException("File không có dòng tiêu đề.");
        }

        var dataRows = sourceRows
            .Where(row => row.RowNumber > headerRow.RowNumber)
            .Where(row => row.Cells.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList();
        if (dataRows.Count > MaximumRows)
        {
            throw new BusinessException($"File có quá {MaximumRows:N0} dòng dữ liệu.");
        }

        return new TableRows(headerRow, dataRows);
    }

    private static async Task<BufferedSourceFile> ReadSourceAsync(
        Stream source,
        string fileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        var safeFileName = Path.GetFileName(fileName ?? string.Empty);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();
        if (extension is not ".xlsx" and not ".csv")
        {
            throw new BusinessException("Chỉ hỗ trợ file Excel (.xlsx) hoặc CSV (.csv).");
        }

        await using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > MaximumFileSizeBytes)
            {
                throw new BusinessException("File vượt quá giới hạn 5 MB.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        if (buffer.Length == 0)
        {
            throw new BusinessException("File không có dữ liệu.");
        }

        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        IReadOnlyList<SourceTableRow> sourceRows = extension == ".csv"
            ? ParseCsv(bytes)
            : ParseXlsx(bytes);

        return new BufferedSourceFile(safeFileName, extension[1..], hash, sourceRows);
    }

    private static PriceListImportParsedRow ParseRow(
        SourceTableRow source,
        IReadOnlyDictionary<string, int> mappedColumns)
    {
        var issues = new List<PriceListImportIssueResDTO>();
        var itemCode = Cell(source, mappedColumns, "ItemCode")?.Trim();
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            issues.Add(Issue(source.RowNumber, "Error", "ITEM_CODE_REQUIRED", "Mã mặt hàng không được để trống.", "ItemCode"));
        }

        var priceText = Cell(source, mappedColumns, "UnitPrice");
        var unitPrice = ParseDecimal(priceText, preferThousands: true);
        if (!unitPrice.HasValue)
        {
            issues.Add(Issue(source.RowNumber, "Error", "UNIT_PRICE_INVALID", "Đơn giá không hợp lệ.", "UnitPrice"));
        }
        else if (unitPrice.Value < 0)
        {
            issues.Add(Issue(source.RowNumber, "Error", "UNIT_PRICE_NEGATIVE", "Đơn giá phải lớn hơn hoặc bằng 0.", "UnitPrice"));
        }

        var vatRate = ParseOptionalDecimal(source, mappedColumns, "VatRate", issues, 0, 100, false);
        var moq = ParseOptionalDecimal(source, mappedColumns, "MinimumOrderQuantity", issues, 0, null, false);
        var leadTimeDays = ParseOptionalInt(source, mappedColumns, "LeadTimeDays", issues, 0);
        var isDefault = ParseOptionalBoolean(source, mappedColumns, issues);

        return new PriceListImportParsedRow
        {
            RowNumber = source.RowNumber,
            ItemCode = NormalizeOptional(itemCode),
            SupplierSku = NormalizeOptional(Cell(source, mappedColumns, "SupplierSku")),
            ItemName = NormalizeOptional(Cell(source, mappedColumns, "ItemName")),
            UnitPrice = unitPrice,
            VatRate = vatRate,
            MinimumOrderQuantity = moq,
            LeadTimeDays = leadTimeDays,
            IsDefault = isDefault,
            Note = NormalizeOptional(Cell(source, mappedColumns, "Note")),
            Issues = issues
        };
    }

    private static decimal? ParseOptionalDecimal(
        SourceTableRow source,
        IReadOnlyDictionary<string, int> columns,
        string field,
        ICollection<PriceListImportIssueResDTO> issues,
        decimal minimum,
        decimal? maximum,
        bool preferThousands)
    {
        var raw = Cell(source, columns, field);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var value = ParseDecimal(raw, preferThousands);
        if (!value.HasValue || value.Value < minimum || (maximum.HasValue && value.Value > maximum.Value))
        {
            var range = maximum.HasValue ? $"từ {minimum} đến {maximum}" : $"lớn hơn hoặc bằng {minimum}";
            issues.Add(Issue(
                source.RowNumber,
                "Error",
                $"{field.ToUpperInvariant()}_INVALID",
                $"{FieldLabel(field)} phải {range}.",
                field));
            return null;
        }

        return value;
    }

    private static int? ParseOptionalInt(
        SourceTableRow source,
        IReadOnlyDictionary<string, int> columns,
        string field,
        ICollection<PriceListImportIssueResDTO> issues,
        int minimum)
    {
        var raw = Cell(source, columns, field);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var number = ParseDecimal(raw, preferThousands: false);
        if (!number.HasValue || decimal.Truncate(number.Value) != number.Value || number.Value < minimum || number.Value > int.MaxValue)
        {
            issues.Add(Issue(
                source.RowNumber,
                "Error",
                $"{field.ToUpperInvariant()}_INVALID",
                $"{FieldLabel(field)} phải là số nguyên lớn hơn hoặc bằng {minimum}.",
                field));
            return null;
        }

        return decimal.ToInt32(number.Value);
    }

    private static bool? ParseOptionalBoolean(
        SourceTableRow source,
        IReadOnlyDictionary<string, int> columns,
        ICollection<PriceListImportIssueResDTO> issues)
    {
        var raw = Cell(source, columns, "IsDefault");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return NormalizeToken(raw) switch
        {
            "true" or "1" or "yes" or "y" or "co" => true,
            "false" or "0" or "no" or "n" or "khong" => false,
            _ => AddInvalidBooleanIssue()
        };

        bool? AddInvalidBooleanIssue()
        {
            issues.Add(Issue(source.RowNumber, "Error", "IS_DEFAULT_INVALID", "Mặc định chỉ nhận Có/Không, Yes/No, True/False hoặc 1/0.", "IsDefault"));
            return null;
        }
    }

    private static decimal? ParseDecimal(string? raw, bool preferThousands)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var text = raw.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var hasDot = text.Contains('.', StringComparison.Ordinal);
        var hasComma = text.Contains(',', StringComparison.Ordinal);
        if (hasDot && hasComma)
        {
            var culture = text.LastIndexOf(',') > text.LastIndexOf('.')
                ? CultureInfo.GetCultureInfo("vi-VN")
                : CultureInfo.GetCultureInfo("en-US");
            return decimal.TryParse(text, NumberStyles.Number, culture, out var mixed) ? mixed : null;
        }

        if (hasComma)
        {
            var culture = preferThousands && LooksLikeThousands(text, ',')
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("vi-VN");
            return decimal.TryParse(text, NumberStyles.Number, culture, out var commaValue) ? commaValue : null;
        }

        if (hasDot)
        {
            var culture = preferThousands && LooksLikeThousands(text, '.')
                ? CultureInfo.GetCultureInfo("vi-VN")
                : CultureInfo.InvariantCulture;
            return decimal.TryParse(text, NumberStyles.Number, culture, out var dotValue) ? dotValue : null;
        }

        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static bool LooksLikeThousands(string value, char separator)
    {
        var groups = value.TrimStart('-', '+').Split(separator);
        return groups.Length > 1 && groups.Skip(1).All(group => group.Length == 3 && group.All(char.IsDigit));
    }

    private static string? Cell(SourceTableRow row, IReadOnlyDictionary<string, int> columns, string field)
        => columns.TryGetValue(field, out var index) && index < row.Cells.Count ? row.Cells[index] : null;

    private static PriceListImportIssueResDTO Issue(int row, string severity, string code, string message, string? column = null)
        => new() { RowNumber = row, Severity = severity, Code = code, Message = message, Column = column };

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FieldLabel(string field) => field switch
    {
        "ItemCode" => "Mã mặt hàng",
        "SupplierSku" => "Mã hàng nhà cung cấp",
        "ItemName" => "Tên mặt hàng",
        "UnitPrice" => "Đơn giá",
        "VatRate" => "Thuế VAT",
        "MinimumOrderQuantity" => "Số lượng tối thiểu",
        "LeadTimeDays" => "Số ngày giao",
        "IsDefault" => "Giá mặc định",
        "Note" => "Ghi chú",
        _ => field
    };

    private static string NormalizeToken(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is 'đ' or 'Đ')
            {
                builder.Append('d');
            }
            else if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IReadOnlyList<SourceTableRow> ParseCsv(byte[] bytes)
    {
        string content;
        try
        {
            content = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        }
        catch (DecoderFallbackException ex)
        {
            throw new BusinessException("File CSV phải dùng mã hóa UTF-8.", ex);
        }

        var delimiter = DetectDelimiter(content);
        var rows = new List<SourceTableRow>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        var inQuotes = false;
        var rowNumber = 1;

        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (character == '"')
            {
                if (inQuotes && index + 1 < content.Length && content[index + 1] == '"')
                {
                    currentCell.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (!inQuotes && character == delimiter)
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            if (!inQuotes && (character == '\r' || character == '\n'))
            {
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                {
                    index++;
                }
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                rows.Add(new SourceTableRow(rowNumber++, currentRow));
                currentRow = [];
                continue;
            }

            currentCell.Append(character);
        }

        if (inQuotes)
        {
            throw new BusinessException("File CSV có dấu ngoặc kép chưa đóng.");
        }

        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString());
            rows.Add(new SourceTableRow(rowNumber, currentRow));
        }

        return rows;
    }

    private static char DetectDelimiter(string content)
    {
        var firstLine = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var candidates = new[] { ',', ';', '\t' };
        return candidates
            .Select(candidate => (Candidate: candidate, Count: CountOutsideQuotes(firstLine, candidate)))
            .OrderByDescending(result => result.Count)
            .First().Candidate;
    }

    private static int CountOutsideQuotes(string value, char candidate)
    {
        var count = 0;
        var inQuotes = false;
        foreach (var character in value)
        {
            if (character == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (!inQuotes && character == candidate)
            {
                count++;
            }
        }
        return count;
    }

    private static IReadOnlyList<SourceTableRow> ParseXlsx(byte[] bytes)
    {
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count > MaximumArchiveEntries
                || archive.Entries.Sum(entry => entry.Length) > MaximumExpandedSizeBytes)
            {
                throw new BusinessException("File Excel có cấu trúc quá lớn.");
            }

            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = ResolveFirstWorksheet(archive)
                ?? throw new BusinessException("File Excel không có trang dữ liệu.");
            using var sheetStream = sheetEntry.Open();
            var document = XDocument.Load(sheetStream, LoadOptions.None);
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var rows = new List<SourceTableRow>();
            foreach (var rowElement in document.Descendants(main + "row"))
            {
                var rowNumber = (int?)rowElement.Attribute("r") ?? rows.Count + 1;
                var values = new List<string>();
                foreach (var cell in rowElement.Elements(main + "c"))
                {
                    var reference = (string?)cell.Attribute("r") ?? string.Empty;
                    var columnIndex = ColumnIndex(reference);
                    while (values.Count <= columnIndex)
                    {
                        values.Add(string.Empty);
                    }
                    values[columnIndex] = ReadCellValue(cell, main, sharedStrings);
                }
                rows.Add(new SourceTableRow(rowNumber, values));
            }
            return rows;
        }
        catch (InvalidDataException ex)
        {
            throw new BusinessException("File Excel không hợp lệ hoặc đã bị hỏng.", ex);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new BusinessException("File Excel không hợp lệ hoặc đã bị hỏng.", ex);
        }
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream, LoadOptions.None);
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(main + "si")
            .Select(item => string.Concat(item.Descendants(main + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static ZipArchiveEntry? ResolveFirstWorksheet(ZipArchive archive)
    {
        var workbook = archive.GetEntry("xl/workbook.xml");
        var relationships = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbook is not null && relationships is not null)
        {
            using var workbookStream = workbook.Open();
            using var relationshipsStream = relationships.Open();
            var workbookDocument = XDocument.Load(workbookStream, LoadOptions.None);
            var relationshipDocument = XDocument.Load(relationshipsStream, LoadOptions.None);
            XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
            var relationshipId = (string?)workbookDocument.Descendants(main + "sheet").FirstOrDefault()?.Attribute(rel + "id");
            var target = relationshipDocument.Descendants(packageRel + "Relationship")
                .FirstOrDefault(item => string.Equals((string?)item.Attribute("Id"), relationshipId, StringComparison.Ordinal))?
                .Attribute("Target")?.Value;
            if (!string.IsNullOrWhiteSpace(target) && !target.Contains("..", StringComparison.Ordinal))
            {
                var path = target.StartsWith("/", StringComparison.Ordinal)
                    ? target.TrimStart('/')
                    : $"xl/{target.TrimStart('/')}";
                var resolved = archive.GetEntry(path.Replace('\\', '/'));
                if (resolved is not null)
                {
                    return resolved;
                }
            }
        }

        return archive.Entries
            .Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                            && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string ReadCellValue(XElement cell, XNamespace main, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        if (string.Equals(type, "inlineStr", StringComparison.Ordinal))
        {
            return string.Concat(cell.Descendants(main + "t").Select(text => text.Value));
        }

        var raw = cell.Element(main + "v")?.Value ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.Ordinal)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0
            && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return raw;
    }

    private static int ColumnIndex(string reference)
    {
        var index = 0;
        foreach (var character in reference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }
            index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }
        return Math.Max(0, index - 1);
    }

    private sealed record BufferedSourceFile(
        string SafeFileName,
        string FileFormat,
        string FileHash,
        IReadOnlyList<SourceTableRow> Rows);

    private sealed record TableRows(SourceTableRow Header, IReadOnlyList<SourceTableRow> DataRows);

    private sealed record SourceTableRow(int RowNumber, IReadOnlyList<string> Cells);
}
