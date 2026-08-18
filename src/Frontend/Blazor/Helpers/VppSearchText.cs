using System.Globalization;
using System.Text;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Chuẩn hóa tìm kiếm cục bộ để người dùng có thể gõ có dấu hoặc không dấu.
/// Query được chuẩn hóa một lần rồi dùng lại cho các cột trong cùng một lần lọc.
/// </summary>
public static class VppSearchText
{
    private const int MaxTerms = 10;
    private const int MaxTermLength = 64;

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
        var pendingSeparator = false;
        foreach (var character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(char.ToLowerInvariant(character));
                pendingSeparator = false;
            }
            else
            {
                pendingSeparator = builder.Length > 0;
            }
        }

        return builder.ToString().Trim().Normalize(NormalizationForm.FormC);
    }

    public static IReadOnlyList<string> Tokenize(string? value) =>
        Normalize(value)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length > 0)
            .Select(term => term.Length <= MaxTermLength ? term : term[..MaxTermLength])
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTerms)
            .ToArray();

    public static string Compact(string? value) => Normalize(value).Replace(" ", string.Empty, StringComparison.Ordinal);

    public static bool Contains(string? value, string normalizedSearch) =>
        MatchesAllTerms(Tokenize(normalizedSearch), value);

    public static bool MatchesAny(string normalizedSearch, params string?[] values) =>
        MatchesAllTerms(Tokenize(normalizedSearch), values);

    public static bool MatchesAllTerms(IReadOnlyList<string> terms, params string?[] values)
    {
        if (terms.Count == 0)
        {
            return true;
        }

        var normalizedValues = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(Normalize)
            .ToArray();

        return terms.All(term => normalizedValues.Any(value =>
            value.Contains(term, StringComparison.Ordinal)
            || value.Replace(" ", string.Empty, StringComparison.Ordinal).Contains(term, StringComparison.Ordinal)));
    }
}
