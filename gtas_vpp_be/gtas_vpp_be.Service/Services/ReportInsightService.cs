using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class ReportInsightsOptions
{
    public const string SectionName = "ReportInsights";

    public bool Enabled { get; set; }
    public string Model { get; set; } = "gpt-5.6-luna";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxOutputTokens { get; set; } = 700;
}

public interface IReportInsightService
{
    Task<ReportInsightResDTO> GenerateAsync(
        ReportSummaryResDTO report,
        string language,
        CancellationToken cancellationToken = default);
}

public sealed class ReportInsightService(
    HttpClient httpClient,
    IOptions<ReportInsightsOptions> options,
    IConfiguration configuration,
    ILogger<ReportInsightService> logger) : IReportInsightService
{
    private const string ResponsesPath = "v1/responses";
    private const int MaxListItems = 4;
    private const int MaxTextLength = 500;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient = httpClient;
    private readonly ReportInsightsOptions _options = options.Value;
    private readonly string? _apiKey = configuration["OPENAI_API_KEY"];
    private readonly ILogger<ReportInsightService> _logger = logger;

    public async Task<ReportInsightResDTO> GenerateAsync(
        ReportSummaryResDTO report,
        string language,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var normalizedLanguage = NormalizeLanguage(language);
        var fallback = BuildRuleBasedInsight(report, normalizedLanguage);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_apiKey))
        {
            return fallback;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60)));

            using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesPath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = JsonContent.Create(BuildRequest(report, normalizedLanguage), options: JsonOptions);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI report insight request returned HTTP {StatusCode}; using rule-based fallback.",
                    (int)response.StatusCode);
                return fallback;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var responseDocument = await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken: timeout.Token);
            var outputText = ExtractOutputText(responseDocument.RootElement);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return fallback;
            }

            var generated = JsonSerializer.Deserialize<InsightPayload>(outputText, JsonOptions);
            if (generated is null || string.IsNullOrWhiteSpace(generated.Summary))
            {
                return fallback;
            }

            return new ReportInsightResDTO
            {
                Summary = ClampText(generated.Summary),
                Highlights = NormalizeItems(generated.Highlights),
                Risks = NormalizeItems(generated.Risks),
                Recommendations = NormalizeItems(generated.Recommendations),
                Source = ReportInsightSources.OpenAI,
                Model = _options.Model,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("OpenAI report insight request timed out; using rule-based fallback.");
            return fallback;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex, "OpenAI report insight generation failed; using rule-based fallback.");
            return fallback;
        }
    }

    private object BuildRequest(ReportSummaryResDTO report, string language)
    {
        var languageName = language == "en" ? "English" : "Vietnamese";
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
            StatusBreakdown = report.StatusBreakdown.Take(12),
            DepartmentBreakdown = report.DepartmentBreakdown.Take(12),
            TopProducts = report.TopProducts.Take(10)
        };

        return new
        {
            model = _options.Model,
            store = false,
            reasoning = new { effort = "low" },
            max_output_tokens = Math.Clamp(_options.MaxOutputTokens, 300, 1_500),
            instructions = $"""
                You are a procurement reporting analyst for an office-supply request system.
                Analyze only the aggregate metrics supplied by the application. Do not invent causes,
                users, departments, prices, or events that are absent from the data. Distinguish facts
                from cautious recommendations. Write concise {languageName} suitable for a business dashboard.
                """,
            input = JsonSerializer.Serialize(aggregateInput, JsonOptions),
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "report_insight",
                    strict = true,
                    schema = new
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
                    }
                }
            }
        };
    }

    private static ReportInsightResDTO BuildRuleBasedInsight(ReportSummaryResDTO report, string language)
    {
        var english = language == "en";
        if (report.TotalOrders == 0)
        {
            return new ReportInsightResDTO
            {
                Summary = english
                    ? "There is not enough data in the selected scope and period to derive insights."
                    : "Chưa có đủ dữ liệu trong phạm vi và kỳ đã chọn để tạo nhận định.",
                Recommendations =
                [
                    english
                        ? "Expand the reporting period or select another permitted scope."
                        : "Mở rộng kỳ báo cáo hoặc chọn phạm vi dữ liệu khác mà bạn được phép xem."
                ],
                GeneratedAt = DateTime.UtcNow
            };
        }

        var averageAmount = report.TotalAmount / Math.Max(report.TotalOrders, 1);
        var topProduct = report.TopProducts.FirstOrDefault();
        var topDepartment = report.DepartmentBreakdown.FirstOrDefault();
        var highlights = new List<string>
        {
            english
                ? $"{report.TotalOrders:N0} orders contain {report.TotalQuantity:N0} requested units, with a total value of {report.TotalAmount:N0}."
                : $"{report.TotalOrders:N0} đơn gồm {report.TotalQuantity:N0} sản phẩm được yêu cầu, tổng giá trị {report.TotalAmount:N0} đồng.",
            english
                ? $"Average value per order is {averageAmount:N0}."
                : $"Giá trị trung bình mỗi đơn là {averageAmount:N0} đồng."
        };

        if (topProduct is not null)
        {
            highlights.Add(english
                ? $"The most requested product is {topProduct.ProductName} ({topProduct.TotalQuantity:N0} units)."
                : $"Vật tư được yêu cầu nhiều nhất là {topProduct.ProductName} ({topProduct.TotalQuantity:N0} sản phẩm)."
            );
        }

        var risks = new List<string>();
        var recommendations = new List<string>();
        if (topProduct is not null && report.TotalQuantity > 0 && topProduct.TotalQuantity * 100L / report.TotalQuantity >= 50)
        {
            risks.Add(english
                ? "Demand is concentrated in one product, which can increase supply disruption impact."
                : "Nhu cầu tập trung vào một vật tư, có thể làm tăng ảnh hưởng khi nguồn cung gián đoạn.");
            recommendations.Add(english
                ? "Review safety stock and alternative suppliers for the leading product."
                : "Rà soát tồn kho an toàn và nhà cung cấp thay thế cho vật tư đứng đầu.");
        }

        if (topDepartment is not null && report.TotalOrders > 0 && topDepartment.OrderCount * 100L / report.TotalOrders >= 60)
        {
            risks.Add(english
                ? "A single department accounts for most orders in the selected scope."
                : "Một phòng ban chiếm phần lớn số đơn trong phạm vi đã chọn.");
        }

        if (report.PeriodTrend.Count >= 2)
        {
            var first = report.PeriodTrend.First().TotalQuantity;
            var last = report.PeriodTrend.Last().TotalQuantity;
            if (first > 0 && last >= first * 1.2)
            {
                recommendations.Add(english
                    ? "Demand has increased across the selected periods; validate the next purchasing plan against this trend."
                    : "Nhu cầu tăng qua các kỳ đã chọn; nên đối chiếu xu hướng này khi lập kế hoạch mua kỳ tiếp theo.");
            }
        }

        if (risks.Count == 0)
        {
            risks.Add(english
                ? "No material concentration risk is visible from the available aggregate metrics."
                : "Chưa thấy rủi ro tập trung đáng kể từ các chỉ số tổng hợp hiện có.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add(english
                ? "Continue monitoring product and department concentration when new periods are added."
                : "Tiếp tục theo dõi mức tập trung theo vật tư và phòng ban khi có thêm kỳ dữ liệu.");
        }

        return new ReportInsightResDTO
        {
            Summary = english
                ? "The selected report has been summarized from deterministic aggregate rules."
                : "Báo cáo đã chọn được tóm tắt bằng các quy tắc tổng hợp xác định.",
            Highlights = highlights.Take(MaxListItems).ToList(),
            Risks = risks.Take(MaxListItems).ToList(),
            Recommendations = recommendations.Take(MaxListItems).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static string? ExtractOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type)
                    && type.GetString() == "output_text"
                    && contentItem.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static List<string> NormalizeItems(IEnumerable<string>? items) =>
        items?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(ClampText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxListItems)
            .ToList() ?? [];

    private static string ClampText(string value) =>
        value.Trim().Length <= MaxTextLength ? value.Trim() : value.Trim()[..MaxTextLength];

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";

    private sealed class InsightPayload
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> Highlights { get; set; } = [];
        public List<string> Risks { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];
    }
}
