using System.Globalization;
using System.Text;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Chuẩn hóa tìm kiếm cục bộ để người dùng có thể gõ có dấu hoặc không dấu.
/// Query được chuẩn hóa một lần rồi dùng lại cho các cột trong cùng một lần lọc.
/// </summary>
public static class VppSearchText
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var source = value.Trim()
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);
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

    public static bool MatchesAny(string normalizedSearch, params string?[] values) =>
        normalizedSearch.Length == 0
        || values.Any(value => Contains(value, normalizedSearch));
}
