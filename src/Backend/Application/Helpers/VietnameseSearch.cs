using System.Globalization;
using System.Text;

namespace gtas_vpp_be.Service.Helpers;

/// <summary>
/// Chuẩn hóa từ khóa tiếng Việt dùng chung cho tìm kiếm server và các danh sách đã tải về bộ nhớ.
/// </summary>
public static class VietnameseSearch
{
    public const string SqlServerCollation = "Vietnamese_100_CI_AI";
    private const int MaxTerms = 10;
    private const int MaxTermLength = 64;

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
