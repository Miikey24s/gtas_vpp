using System.Text;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Quy ước tên file và MIME dùng chung cho mọi export. Tên file chỉ chứa
/// segment đã làm sạch để Content-Disposition không bị sai khi dữ liệu có ký tự lạ.
/// </summary>
public static class ExportFileContract
{
    private const string PortableInvalidFileNameCharacters = "<>:\"/\\|?*";

    public const string PdfContentType = "application/pdf";
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string CsvContentType = "text/csv; charset=utf-8";

    public static string Order(string? orderCode, Guid orderId, string extension)
        => BuildWithFallback("GTAS-VPP-Don", orderCode, orderId.ToString("N"), extension);

    public static string Settlement(int year, int month, int revision, string extension)
        => BuildSegments("GTAS-VPP-Chot-ky", $"{year:0000}-{month:00}", $"R{revision}", extension);

    public static string Report(string scope, int? year, int? month, string extension)
    {
        var period = year.HasValue
            ? month.HasValue ? $"{year:0000}-{month:00}" : year.Value.ToString("0000")
            : "Tat-ca-ky";
        return BuildSegments("GTAS-VPP-Bao-cao", scope, period, extension);
    }

    private static string BuildWithFallback(string prefix, string? segment, string fallback, string extension)
        => $"{prefix}-{SanitizeSegment(segment, fallback)}.{NormalizeExtension(extension)}";

    private static string BuildSegments(string prefix, string first, string second, string extension)
        => $"{prefix}-{SanitizeSegment(first, "Du-lieu")}-{SanitizeSegment(second, "1")}.{NormalizeExtension(extension)}";

    internal static string SanitizeSegment(string? value, string fallback)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        var builder = new StringBuilder(source.Length);
        var lastWasSeparator = false;

        foreach (var character in source.Normalize(NormalizationForm.FormKC))
        {
            var isSeparator = char.IsWhiteSpace(character) || character is '-' or '_';
            if (isSeparator)
            {
                if (!lastWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                lastWasSeparator = true;
                continue;
            }

            // Linux cho phép một số ký tự mà Windows cấm. Export phải cho cùng
            // một tên file an toàn ở local, CI Linux và trình duyệt production.
            if (char.IsControl(character)
                || PortableInvalidFileNameCharacters.Contains(character))
            {
                if (!lastWasSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                lastWasSeparator = true;
                continue;
            }

            builder.Append(character);
            lastWasSeparator = false;
            if (builder.Length >= 80)
            {
                break;
            }
        }

        var result = builder.ToString().Trim('-', '.', ' ');
        return string.IsNullOrWhiteSpace(result) ? fallback : result;
    }

    private static string NormalizeExtension(string extension)
    {
        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        return normalized is "pdf" or "xlsx" or "csv"
            ? normalized
            : throw new ArgumentOutOfRangeException(nameof(extension), extension, "Unsupported export extension.");
    }
}
