using System.Text.Json;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public interface IReportInsightService
{
    Task<ReportInsightResDTO> GenerateAsync(
        ReportSummaryResDTO report,
        string language,
        CancellationToken cancellationToken = default);
}

public sealed class ReportInsightService(
    IEnumerable<IReportInsightProvider> providers,
    IOptions<ReportInsightsOptions> options,
    ReportInsightQuotaGate quotaGate,
    ILogger<ReportInsightService> logger) : IReportInsightService
{
    private const int MaxListItems = 4;
    private const int MaxTextLength = 500;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, IReportInsightProvider> _providers = providers
        .GroupBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    private readonly ReportInsightsOptions _options = options.Value;
    private readonly ReportInsightQuotaGate _quotaGate = quotaGate;
    private readonly ILogger<ReportInsightService> _logger = logger;

    public async Task<ReportInsightResDTO> GenerateAsync(
        ReportSummaryResDTO report,
        string language,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        var normalizedLanguage = NormalizeLanguage(language);
        var fallback = ReportInsightRules.Build(report, normalizedLanguage);

        if (!_options.Enabled)
        {
            return fallback;
        }

        var prompt = ReportInsightPromptFactory.Create(report, normalizedLanguage, _options.MaxOutputTokens);
        var providerNames = (_options.ProviderPriority ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => _providers.TryGetValue(name, out var provider) && provider.IsConfigured)
            .Take(Math.Clamp(_options.MaxProvidersPerRequest, 1, 8));

        foreach (var providerName in providerNames)
        {
            if (!_providers.TryGetValue(providerName, out var provider))
            {
                continue;
            }

            var providerOptions = _options.GetProvider(provider.Name);
            if (!_quotaGate.TryAcquire(provider.Name, providerOptions.DailyRequestLimit))
            {
                _logger.LogWarning(
                    "AI report insight provider {Provider} reached the configured daily request limit; trying fallback provider.",
                    provider.Name);
                continue;
            }

            ReportInsightProviderResult providerResult;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 60)));
                providerResult = await provider.GenerateAsync(prompt, timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "AI report insight provider {Provider} timed out; trying fallback provider.",
                    provider.Name);
                continue;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or JsonException or NotSupportedException or UriFormatException)
            {
                _logger.LogWarning(
                    exception,
                    "AI report insight provider {Provider} failed; trying fallback provider.",
                    provider.Name);
                continue;
            }

            if (!providerResult.IsSuccess)
            {
                _logger.LogWarning(
                    "AI report insight provider {Provider} failed with {FailureKind} and HTTP {StatusCode}; trying fallback provider.",
                    provider.Name,
                    providerResult.FailureKind,
                    providerResult.StatusCode);
                continue;
            }

            if (!TryParsePayload(providerResult.ResponseJson, out var payload))
            {
                _logger.LogWarning(
                    "AI report insight provider {Provider} returned invalid structured output; trying fallback provider.",
                    provider.Name);
                continue;
            }

            return new ReportInsightResDTO
            {
                Summary = ClampText(payload.Summary),
                Highlights = NormalizeItems(payload.Highlights),
                Risks = NormalizeItems(payload.Risks),
                Recommendations = NormalizeItems(payload.Recommendations),
                Source = providerResult.Provider,
                Model = providerResult.Model,
                GeneratedAt = DateTime.UtcNow
            };
        }

        return fallback;
    }

    private static bool TryParsePayload(string? json, out InsightPayload payload)
    {
        payload = new InsightPayload();
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<InsightPayload>(json, JsonOptions);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Summary))
            {
                return false;
            }

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static List<string> NormalizeItems(IEnumerable<string>? items) =>
        items?
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(ClampText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxListItems)
            .ToList() ?? [];

    private static string ClampText(string value)
    {
        var text = value.Trim();
        return text.Length <= MaxTextLength ? text : text[..MaxTextLength];
    }

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

public static class ReportInsightRules
{
    private const int MaxListItems = 4;

    public static ReportInsightResDTO Build(ReportSummaryResDTO report, string language)
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
                : $"{report.TotalOrders:N0} đơn có tổng số lượng yêu cầu {report.TotalQuantity:N0}, tổng giá trị {report.TotalAmount:N0} đồng.",
            english
                ? $"Average value per order is {averageAmount:N0}."
                : $"Giá trị trung bình mỗi đơn là {averageAmount:N0} đồng."
        };

        if (topProduct is not null)
        {
            highlights.Add(english
                ? $"The most requested item is {topProduct.ProductName} ({topProduct.TotalQuantity:N0} units)."
                : $"Mặt hàng được yêu cầu nhiều nhất là {topProduct.ProductName} (số lượng {topProduct.TotalQuantity:N0})."
            );
        }

        var risks = new List<string>();
        var recommendations = new List<string>();
        if (topProduct is not null
            && report.TotalQuantity > 0
            && topProduct.TotalQuantity * 100L / report.TotalQuantity >= 50)
        {
            risks.Add(english
                ? "Demand is concentrated in one item, which can increase supply disruption impact."
                : "Nhu cầu tập trung vào một mặt hàng, có thể làm tăng ảnh hưởng khi nguồn cung gián đoạn.");
            recommendations.Add(english
                ? "Review safety stock and alternative suppliers for the leading item."
                : "Rà soát tồn kho an toàn và nhà cung cấp thay thế cho mặt hàng đứng đầu.");
        }

        if (topDepartment is not null
            && report.TotalOrders > 0
            && topDepartment.OrderCount * 100L / report.TotalOrders >= 60)
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
                ? "Continue monitoring item and department concentration when new periods are added."
                : "Tiếp tục theo dõi mức tập trung theo mặt hàng và phòng ban khi có thêm kỳ dữ liệu.");
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
}
