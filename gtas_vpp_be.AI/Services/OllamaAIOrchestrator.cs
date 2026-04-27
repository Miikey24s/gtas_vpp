using gtas_vpp_be.Service.AI;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.AI.Services;

public class OllamaAIOrchestrator : IAIOrchestrator
{
    private readonly IEnumerable<IAIUseCaseHandler> _handlers;
    private readonly ILogger<OllamaAIOrchestrator> _logger;
    private readonly HttpClient _httpClient;

    public OllamaAIOrchestrator(
        IEnumerable<IAIUseCaseHandler> handlers,
        ILogger<OllamaAIOrchestrator> logger,
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
            if (!await IsOllamaAvailableAsync(ct))
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

    private async Task<bool> IsOllamaAvailableAsync(CancellationToken ct)
    {
        try
        {
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
