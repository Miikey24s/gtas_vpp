using gtas_vpp_be.AI.KeyManagement.Interfaces;
using gtas_vpp_be.Service.AI;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIOrchestrator _orchestrator;
    private readonly IChatClient _chatClient;
    private readonly IVPPEmbeddingStore _embeddingStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public AIController(
        IAIOrchestrator orchestrator,
        IChatClient chatClient,
        IVPPEmbeddingStore embeddingStore,
        HttpClient httpClient,
        ILogger<AIController> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _orchestrator = orchestrator;
        _chatClient = chatClient;
        _embeddingStore = embeddingStore;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("admin-chat")]
    [Authorize(Policy = Permissions.RequestAIChat)]
    public async Task<IActionResult> AdminChatAsync([FromBody] AIChatRequestDTO request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Ok(new AIChatResponseDTO
            {
                IsSuccess = false,
                Message = "Message is required.",
                ErrorMessage = "Message is required."
            });
        }

        try
        {
            var messages = BuildDirectChatMessages(request);
            var response = await _chatClient.GetResponseAsync(messages, new ChatOptions
            {
                MaxOutputTokens = 4096,
                Temperature = 0.7f
            }, ct);

            return Ok(new AIChatResponseDTO
            {
                IsSuccess = true,
                Message = string.IsNullOrWhiteSpace(response.Text) ? "No response from AI." : response.Text,
                UseCaseId = "admin_chat"
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI {Action} timed out or was cancelled", "AdminChat");
            return StatusCode(504, new AIChatResponseDTO
            {
                IsSuccess = false,
                Message = "AI response timed out.",
                ErrorMessage = "AI response timed out.",
                UseCaseId = "admin_chat"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {Action} failed", "AdminChat");
            return StatusCode(503, new AIChatResponseDTO
            {
                IsSuccess = false,
                Message = "AI service is temporarily unavailable.",
                ErrorMessage = "AI service is temporarily unavailable.",
                UseCaseId = "admin_chat"
            });
        }
    }

    [HttpPost("chat")]
    [Authorize(Policy = Permissions.RequestAIVppChat)]
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
            return StatusCode(504, new { IsSuccess = false, Message = "Rebuild embeddings bị hủy hoặc quá thời gian chờ" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {Action} failed", "RebuildEmbeddings");
            return StatusCode(503, new { IsSuccess = false, Message = "Không thể rebuild embeddings lúc này" });
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
            if (!IsSelfHostedAiEndpoint())
            {
                return Ok(new
                {
                    IsAvailable = true,
                    StatusCode = StatusCodes.Status200OK
                });
            }

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
        if (!string.Equals(_configuration["AISettings:Provider"], "Google", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { Message = "Google API key management is disabled. Configure Ollama instead." });
        }

        var keyManager = _serviceProvider.GetService<IGeminiKeyManager>();
        if (keyManager is null)
        {
            return NotFound(new { Message = "Google API key management is not registered." });
        }

        return Ok(keyManager.GetAllKeyInfos());
    }

    private bool IsSelfHostedAiEndpoint()
    {
        var host = _httpClient.BaseAddress?.Host ?? string.Empty;
        return host.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("100.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("ollama", StringComparison.OrdinalIgnoreCase)
            || host.Contains("annam.id.vn", StringComparison.OrdinalIgnoreCase);
    }

    private static List<Microsoft.Extensions.AI.ChatMessage> BuildDirectChatMessages(AIChatRequestDTO request)
    {
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();

        if (request.History is not null)
        {
            messages.AddRange(request.History
                .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                .TakeLast(20)
                .Select(x => new Microsoft.Extensions.AI.ChatMessage(MapDirectChatRole(x.Role), x.Content)));
        }

        messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, request.Message));
        return messages;
    }

    private static ChatRole MapDirectChatRole(string role)
    {
        return role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? ChatRole.Assistant
            : ChatRole.User;
    }
}
