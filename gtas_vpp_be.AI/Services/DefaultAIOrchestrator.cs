using gtas_vpp_be.Service.AI;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.AI.Services;

public class DefaultAIOrchestrator : IAIOrchestrator
{
    private readonly IEnumerable<IAIUseCaseHandler> _handlers;
    private readonly ILogger<DefaultAIOrchestrator> _logger;
    private readonly HttpClient _httpClient;

    public DefaultAIOrchestrator(
        IEnumerable<IAIUseCaseHandler> handlers,
        ILogger<DefaultAIOrchestrator> logger,
        HttpClient httpClient)
    {
        _handlers = handlers;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<AIChatResponse> ChatAsync(AIChatRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!await IsServiceAvailableAsync(ct))
            {
                return AIChatResponse.Error("Dịch vụ AI tạm thời không khả dụng");
            }

            var handler = _handlers.FirstOrDefault(x => x.CanHandle(request.Message));
            if (handler is null)
            {
                return AIChatResponse.Error("Tôi không hiểu yêu cầu của bạn");
            }

            return await handler.HandleAsync(request, ct);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI {Action} timed out or was cancelled", "Chat");
            return AIChatResponse.Error("Dịch vụ AI phản hồi quá thời gian chờ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {Action} failed", "Chat");
            return AIChatResponse.Error("Dịch vụ AI tạm thời không khả dụng");
        }
    }

    public IReadOnlyList<AIUseCaseInfo> GetAvailableUseCases()
    {
        return _handlers
            .Select(x => new AIUseCaseInfo(x.UseCaseId, x.DisplayName, $"Xử lý {x.DisplayName}"))
            .ToList();
    }

    /// <summary>
    /// Checks if the AI backend is reachable.
    /// For Ollama: pings /api/tags. For cloud providers: always returns true (they manage availability).
    /// </summary>
    private async Task<bool> IsServiceAvailableAsync(CancellationToken ct)
    {
        try
        {
            // If the HttpClient points to a cloud endpoint (not local/self-hosted), assume available
            var host = _httpClient.BaseAddress?.Host ?? "";
            var isSelfHosted = host.Contains("localhost")
                || host.Contains("127.0.0.1")
                || host.StartsWith("100.")      // Tailscale
                || host.Contains("ollama");      // Docker service
            if (!isSelfHosted)
            {
                return true;
            }

            // Local Ollama health check
            using var response = await _httpClient.GetAsync("api/tags", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "AI {Action} failed", "HealthCheck");
            return false;
        }
    }
}
