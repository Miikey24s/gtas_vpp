using System.Text.Json;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Services;

public static class AiProviderNames
{
    public const string OpenAI = ReportInsightSources.OpenAI;
    public const string Groq = ReportInsightSources.Groq;
    public const string Gemini = ReportInsightSources.Gemini;
    public const string Ollama = ReportInsightSources.Ollama;
}

public enum ReportInsightFailureKind
{
    None,
    NotConfigured,
    RateLimited,
    Unauthorized,
    Unavailable,
    InvalidResponse,
    Timeout,
    Error
}

public sealed record ReportInsightPrompt(
    string Language,
    string Instructions,
    string AggregateJson,
    object OutputSchema,
    int MaxOutputTokens);

public sealed record ReportInsightProviderResult(
    string Provider,
    string? Model,
    string? ResponseJson,
    ReportInsightFailureKind FailureKind = ReportInsightFailureKind.None,
    int? StatusCode = null,
    string? ErrorMessage = null)
{
    public bool IsSuccess => FailureKind == ReportInsightFailureKind.None
        && !string.IsNullOrWhiteSpace(ResponseJson);
}

public interface IReportInsightProvider
{
    string Name { get; }

    bool IsConfigured { get; }

    Task<ReportInsightProviderResult> GenerateAsync(
        ReportInsightPrompt prompt,
        CancellationToken cancellationToken = default);
}

public interface IReportInsightSecretResolver
{
    string? Resolve(string providerName);
}

public sealed class ConfigurationReportInsightSecretResolver(IConfiguration configuration)
    : IReportInsightSecretResolver
{
    private readonly IConfiguration _configuration = configuration;

    public string? Resolve(string providerName)
    {
        var normalized = providerName.Trim().ToUpperInvariant();
        var configured = _configuration[$"ReportInsights:ProviderKeys:{providerName}"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var environmentKey = normalized switch
        {
            "OPENAI" => "OPENAI_API_KEY",
            "GROQ" => "GROQ_API_KEY",
            "GEMINI" => "GEMINI_API_KEY",
            _ => null
        };

        if (environmentKey is not null)
        {
            var environmentValue = _configuration[environmentKey];
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }
        }

        return normalized == "GEMINI"
            ? _configuration["GOOGLE_API_KEY"]
            : null;
    }
}

public sealed class ReportInsightQuotaGate
{
    private readonly Dictionary<string, RequestCounter> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public bool TryAcquire(string providerName, int dailyLimit)
    {
        if (dailyLimit <= 0)
        {
            return true;
        }

        var key = $"{DateTime.UtcNow:yyyyMMdd}:{providerName}";
        RequestCounter counter;
        lock (_sync)
        {
            counter = _counters.TryGetValue(key, out var existing)
                ? existing
                : _counters[key] = new RequestCounter();
        }

        var count = Interlocked.Increment(ref counter.Count);
        if (count <= dailyLimit)
        {
            return true;
        }

        Interlocked.Decrement(ref counter.Count);
        return false;
    }

    private sealed class RequestCounter
    {
        public int Count;
    }
}

public static class ReportInsightPromptFactory
{
    private const int MaxListItems = 12;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ReportInsightPrompt Create(
        ReportSummaryResDTO report,
        string language,
        int maxOutputTokens)
    {
        ArgumentNullException.ThrowIfNull(report);
        var normalizedLanguage = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
        var languageName = normalizedLanguage == "en" ? "English" : "Vietnamese";
        var aggregateInput = new
        {
            report.Scope,
            report.Year,
            report.Month,
            report.TotalOrders,
            report.TotalDepartments,
            report.TotalRequesters,
            report.TotalLines,
            report.TotalQuantity,
            report.TotalAmount,
            PeriodTrend = report.PeriodTrend.Take(24),
            StatusBreakdown = report.StatusBreakdown.Take(MaxListItems),
            DepartmentBreakdown = report.DepartmentBreakdown.Take(MaxListItems),
            TopProducts = report.TopProducts.Take(10)
        };

        return new ReportInsightPrompt(
            normalizedLanguage,
            $"""
            You are a procurement reporting analyst for an office-supply request system.
            Analyze only the aggregate metrics supplied by the application. Do not invent causes,
            users, departments, prices, or events that are absent from the data. Distinguish facts
            from cautious recommendations. Write concise {languageName} suitable for a business dashboard.
            Return only the requested JSON object. Do not include markdown fences.
            """,
            JsonSerializer.Serialize(aggregateInput, JsonOptions),
            ReportInsightSchema.JsonSchema,
            Math.Clamp(maxOutputTokens, 300, 1_500));
    }
}

public static class ReportInsightSchema
{
    public static readonly object JsonSchema = new
    {
        type = "object",
        properties = new
        {
            summary = new { type = "string" },
            highlights = new { type = "array", items = new { type = "string" } },
            risks = new { type = "array", items = new { type = "string" } },
            recommendations = new { type = "array", items = new { type = "string" } }
        },
        required = new[] { "summary", "highlights", "risks", "recommendations" },
        additionalProperties = false
    };
}
