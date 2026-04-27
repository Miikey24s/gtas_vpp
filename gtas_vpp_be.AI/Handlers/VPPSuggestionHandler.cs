using System.Diagnostics;
using System.Text;
using gtas_vpp_be.Service.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.AI.Handlers;

public class VPPSuggestionHandler : IAIUseCaseHandler
{
    private const int TopK = 5;
    private readonly IVPPEmbeddingStore _embeddingStore;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IChatClient _chatClient;
    private readonly ILogger<VPPSuggestionHandler> _logger;

    public VPPSuggestionHandler(
        IVPPEmbeddingStore embeddingStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChatClient chatClient,
        ILogger<VPPSuggestionHandler> logger)
    {
        _embeddingStore = embeddingStore;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
        _logger = logger;
    }

    public string UseCaseId => "vpp_suggestion";

    public string DisplayName => "Đề xuất Văn phòng phẩm";

    public bool CanHandle(string userMessage) => true;

    public async Task<AIChatResponse> HandleAsync(AIChatRequest request, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!await _embeddingStore.HasEmbeddingsAsync(ct))
            {
                return AIChatResponse.Error("Chưa có dữ liệu embedding. Vui lòng chạy rebuild embeddings trước khi sử dụng AI.");
            }

            var queryEmbedding = await _embeddingGenerator.GenerateAsync(request.Message, options: null, cancellationToken: ct);
            var searchResults = await _embeddingStore.SearchSimilarAsync(queryEmbedding.Vector.ToArray(), TopK, ct);

            var messages = BuildMessages(request, searchResults);
            var chatResponse = await _chatClient.GetResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = 2048,
                    Temperature = 0.3f
                },
                ct);

            var suggestedItems = searchResults
                .Select(x => new AISuggestedItem
                {
                    VPPId = x.VPPId,
                    VPPCode = x.VPPCode,
                    VPPName = x.VPPName,
                    CategoryName = x.CategoryName,
                    UOMName = x.UOMName,
                    SimilarityScore = x.SimilarityScore
                })
                .ToList();

            stopwatch.Stop();
            _logger.LogInformation("AI {UseCaseId} processed in {ElapsedMs}ms", UseCaseId, stopwatch.ElapsedMilliseconds);

            return AIChatResponse.Success(chatResponse.Text, UseCaseId, suggestedItems);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI {UseCaseId} timed out or was cancelled", UseCaseId);
            return AIChatResponse.Error("Dịch vụ AI phản hồi quá thời gian chờ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {UseCaseId} failed", UseCaseId);
            return AIChatResponse.Error("Dịch vụ AI tạm thời không khả dụng");
        }
    }

    private static List<ChatMessage> BuildMessages(AIChatRequest request, IReadOnlyList<VPPEmbeddingSearchResult> searchResults)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt(searchResults))
        };

        if (request.ConversationHistory is not null)
        {
            messages.AddRange(request.ConversationHistory
                .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                .TakeLast(10)
                .Select(x => new ChatMessage(MapRole(x.Role), x.Content)));
        }

        messages.Add(new ChatMessage(ChatRole.User, request.Message));

        return messages;
    }

    private static string BuildSystemPrompt(IReadOnlyList<VPPEmbeddingSearchResult> searchResults)
    {
        var context = new StringBuilder();

        foreach (var item in searchResults)
        {
            context.AppendLine($"- {item.VPPCode} | {item.VPPName} | Loại: {item.CategoryName} | Đơn vị: {item.UOMName} | Điểm: {item.SimilarityScore:0.000}");
            context.AppendLine($"  Nội dung embed: {item.EmbeddingText}");
        }

        return $"""
Bạn là trợ lý AI chuyên về văn phòng phẩm (VPP) trong hệ thống GTAS VPP.
Nhiệm vụ: Dựa vào danh sách VPP bên dưới, đề xuất sản phẩm phù hợp nhất cho yêu cầu người dùng.
Trả lời bằng tiếng Việt, ngắn gọn, chuyên nghiệp.
Nếu không tìm thấy VPP phù hợp, hãy nói rõ.

Danh sách VPP liên quan:
{context}
""";
    }

    private static ChatRole MapRole(string role)
    {
        return role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
            ? ChatRole.Assistant
            : ChatRole.User;
    }
}
