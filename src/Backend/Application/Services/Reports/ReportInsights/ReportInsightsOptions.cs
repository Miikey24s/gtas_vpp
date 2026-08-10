namespace gtas_vpp_be.Service.Services;

public sealed class ReportInsightsOptions
{
    public const string SectionName = "ReportInsights";

    public bool Enabled { get; set; }

    // Provider được cấu hình đầu tiên chạy thành công sẽ được dùng; provider thiếu key bị bỏ qua.
    public List<string> ProviderPriority { get; set; } =
    [
        AiProviderNames.Groq,
        AiProviderNames.Gemini,
        AiProviderNames.Ollama,
        AiProviderNames.OpenAI
    ];

    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 700;
    public int MaxProvidersPerRequest { get; set; } = 3;

    public Dictionary<string, ReportInsightProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Giữ để tương thích ngược với cấu hình cũ chỉ dùng OpenAI.
    public string Model { get; set; } = "gpt-5.6-luna";

    public ReportInsightProviderOptions GetProvider(string providerName)
    {
        var pair = Providers.FirstOrDefault(pair =>
            string.Equals(pair.Key, providerName, StringComparison.OrdinalIgnoreCase));
        return pair.Value ?? new ReportInsightProviderOptions();
    }
}

public sealed class ReportInsightProviderOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int DailyRequestLimit { get; set; }
    public string StructuredOutputMode { get; set; } = "json_schema";
}
