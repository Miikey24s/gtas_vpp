namespace gtas_vpp_shared.Constants;

public static class BusinessLanguages
{
    public const string Vietnamese = "vi";
    public const string English = "en";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(
        [Vietnamese, English],
        StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string? languageCode, string fallback = Vietnamese)
    {
        var normalized = languageCode?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized)) return fallback;

        var separator = normalized.IndexOfAny(['-', '_']);
        if (separator > 0) normalized = normalized[..separator];

        return Supported.Contains(normalized) ? normalized : fallback;
    }
}

public static class BusinessDataEntityTypes
{
    public const string LookupCategory = "lookup-category";
    public const string LookupValue = "lookup-value";
    public const string VppCategory = "vpp-category";
    public const string VppItem = "vpp-item";
    public const string Supplier = "supplier";
    public const string PriceList = "price-list";
    public const string Department = "department";

    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(
        [LookupCategory, LookupValue, VppCategory, VppItem, Supplier, PriceList, Department],
        StringComparer.OrdinalIgnoreCase);
}
