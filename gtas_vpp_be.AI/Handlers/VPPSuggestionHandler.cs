using System.Diagnostics;
using System.Text;
using gtas_vpp_be.Service.AI;
using Microsoft.Extensions.Configuration;
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
    private readonly string _aiProvider;
    private readonly string _chatModel;

    public VPPSuggestionHandler(
        IVPPEmbeddingStore embeddingStore,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IChatClient chatClient,
        ILogger<VPPSuggestionHandler> logger,
        IConfiguration configuration)
    {
        _embeddingStore = embeddingStore;
        _embeddingGenerator = embeddingGenerator;
        _chatClient = chatClient;
        _logger = logger;
        _aiProvider = configuration["AISettings:Provider"] ?? "Ollama";
        _chatModel = configuration["AISettings:ChatModel"] ?? "gemma4:e4b";
    }

    public string UseCaseId => "vpp_suggestion";

    public string DisplayName => "VPP Suggestion";

    public bool CanHandle(string userMessage) => true;

    public async Task<AIChatResponse> HandleAsync(AIChatRequest request, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (!await _embeddingStore.HasEmbeddingsAsync(ct))
            {
                return AIChatResponse.Error("No embedding data found. Please rebuild embeddings before using AI.");
            }

            var queryEmbedding = await _embeddingGenerator.GenerateAsync(request.Message, options: null, cancellationToken: ct);
            var searchResults = await _embeddingStore.SearchSimilarAsync(queryEmbedding.Vector.ToArray(), TopK, ct);

            var messages = BuildMessages(request, searchResults);

            var options = new ChatOptions
            {
                ModelId = _chatModel,
                MaxOutputTokens = 2048,
                Temperature = 0.3f
            };

            ChatResponse chatResponse;
            if (!string.Equals(_aiProvider, "Google", StringComparison.OrdinalIgnoreCase))
            {
                chatResponse = await _chatClient.GetResponseAsync(messages, options, ct);
            }
            else
            {
                // STEP 1: ROUTING (Determine primary model)
                string primaryModel = "gemini-3.1-flash-lite-preview";
                if (request.Message.Length > 8000 || request.Message.Contains("phân tích code", StringComparison.OrdinalIgnoreCase) || request.Message.Contains("analyze code", StringComparison.OrdinalIgnoreCase))
                {
                    primaryModel = "gemma-4-31b";
                }

                options.ModelId = primaryModel;
                options.AdditionalProperties = new AdditionalPropertiesDictionary
                {
                    ["HttpRequestHeaders"] = new Dictionary<string, string> { { "x-target-model", primaryModel } }
                };

                try
                {
                    chatResponse = await _chatClient.GetResponseAsync(messages, options, ct);
                }
                catch (Exception ex) when (ex.Message.Contains("ALL_KEYS_EXHAUSTED_FOR_MODEL"))
                {
                    _logger.LogWarning("All 8 keys exhausted (429 Rate Limit) for primary model {Model}. ACTIVATING FALLBACK!", primaryModel);

                    string fallbackModel = "gemma-3-27b";
                    options.ModelId = fallbackModel;

                    if (options.AdditionalProperties?["HttpRequestHeaders"] is Dictionary<string, string> headers)
                    {
                        headers["x-target-model"] = fallbackModel;
                    }

                    try
                    {
                        chatResponse = await _chatClient.GetResponseAsync(messages, options, ct);
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Total failure! Fallback model has also collapsed!");
                        return AIChatResponse.Error("AI Service is currently overloaded. Please try again later.");
                    }
                }
            }

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
            _logger.LogInformation("AI {UseCaseId} processed in {ElapsedMs}ms using model {ModelId}", UseCaseId, stopwatch.ElapsedMilliseconds, options.ModelId);

            return AIChatResponse.Success(chatResponse.Text, UseCaseId, suggestedItems);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "AI {UseCaseId} timed out or was cancelled", UseCaseId);
            return AIChatResponse.Error("AI Service response timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI {UseCaseId} failed", UseCaseId);
            return AIChatResponse.Error("AI Service is temporarily unavailable.");
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
            context.AppendLine($"- {item.VPPCode} | {item.VPPName} | Category: {item.CategoryName} | Unit: {item.UOMName} | Score: {item.SimilarityScore:0.000}");
            context.AppendLine($"  Embedding text: {item.EmbeddingText}");
        }

        return $"""
You are an AI assistant specialized in office supplies (VPP) within the GTAS VPP system.
Task: Based on the relevant VPP list below, suggest the most suitable product for the user's request.
Reply in English, concisely and professionally.
If no suitable VPP is found, state it clearly.

Relevant VPP list:
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
