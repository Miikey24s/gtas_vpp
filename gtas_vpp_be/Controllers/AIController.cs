using gtas_vpp_be.AI.KeyManagement.Interfaces;
using gtas_vpp_be.Service.AI;
using gtas_vpp_shared.DTOs.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIOrchestrator _orchestrator;
    private readonly IVPPEmbeddingStore _embeddingStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIController> _logger;
    private readonly IGeminiKeyManager _keyManager;

    public AIController(
        IAIOrchestrator orchestrator,
        IVPPEmbeddingStore embeddingStore,
        HttpClient httpClient,
        ILogger<AIController> logger,
        IGeminiKeyManager keyManager)
    {
        _orchestrator = orchestrator;
        _embeddingStore = embeddingStore;
        _httpClient = httpClient;
        _logger = logger;
        _keyManager = keyManager;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> ChatAsync([FromBody] AIChatRequestDTO request, CancellationToken ct)
    {
        var internalRequest = new AIChatRequest
        {
            Message = request.Message,
            ConversationHistory = request.History?
                .Select(x => new AIChatMessage(x.Role, x.Content))
                .ToList()
        };

        var response = await _orchestrator.ChatAsync(internalRequest, ct);

        return Ok(new AIChatResponseDTO
        {
            Message = response.Message,
            SuggestedItems = response.SuggestedItems?
                .Select(x => new AISuggestedItemDTO
                {
                    VPPId = x.VPPId,
                    VPPCode = x.VPPCode,
                    VPPName = x.VPPName,
                    CategoryName = x.CategoryName,
                    UOMName = x.UOMName,
                    SimilarityScore = x.SimilarityScore
                })
                .ToList(),
            UseCaseId = response.UseCaseId,
            IsSuccess = response.IsSuccess,
            ErrorMessage = response.ErrorMessage
        });
    }

    [HttpPost("rebuild-embeddings")]
    public async Task<IActionResult> RebuildEmbeddingsAsync(CancellationToken ct)
    {
        try
        {
            await _embeddingStore.RebuildAllAsync(ct);
            return Ok(new { IsSuccess = true, Message = "Đã rebuild embeddings VPP thành công" });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI {Action} timed out or was cancelled", "RebuildEmbeddings");
            return Ok(new { IsSuccess = false, Message = "Rebuild embeddings bị hủy hoặc quá thời gian chờ" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {Action} failed", "RebuildEmbeddings");
            return Ok(new { IsSuccess = false, Message = "Không thể rebuild embeddings lúc này" });
        }
    }

    [HttpGet("use-cases")]
    public IActionResult GetUseCases()
    {
        return Ok(_orchestrator.GetAvailableUseCases());
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> HealthCheck(CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/tags", ct);
            return Ok(new
            {
                IsAvailable = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "AI {Action} failed", "HealthCheck");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                IsAvailable = false,
                Message = "Dịch vụ AI tạm thời không khả dụng"
            });
        }
    }

    [HttpGet("keys")]
    public IActionResult GetAIKeys()
    {
        return Ok(_keyManager.GetAllKeyInfos());
    }
}
