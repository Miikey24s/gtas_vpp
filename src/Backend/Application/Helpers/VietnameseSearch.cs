using System.Globalization;
using System.Text;

namespace gtas_vpp_be.Service.Helpers;

/// <summary>
/// Chuẩn hóa từ khóa tiếng Việt dùng chung cho tìm kiếm server và các danh sách đã tải về bộ nhớ.
/// </summary>
public static class VietnameseSearch
{
    public const string SqlServerCollation = "Vietnamese_100_CI_AI";

    public static string PrepareTerm(string? value) =>
        (value ?? string.Empty).Trim().Replace('đ', 'd').Replace('Đ', 'D');

    public static string BuildContainsPattern(string? value)
    {
        var term = PrepareTerm(value)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);
        return $"%{term}%";
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var source = PrepareTerm(value).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(source.Length);
        foreach (var character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool Contains(string? value, string normalizedSearch) =>
        normalizedSearch.Length == 0
        || Normalize(value).Contains(normalizedSearch, StringComparison.Ordinal);
}
