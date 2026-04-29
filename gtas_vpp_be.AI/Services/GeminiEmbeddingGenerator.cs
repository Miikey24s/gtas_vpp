using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace gtas_vpp_be.AI.Services;

/// <summary>
/// Calls Gemini native embedding endpoint:
/// POST https://generativelanguage.googleapis.com/v1beta/models/{model}:embedContent?key={apiKey}
/// </summary>
public class GeminiEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _apiKey;

    public GeminiEmbeddingGenerator(HttpClient httpClient, string model, string apiKey)
    {
        _httpClient = httpClient;
        _model = model;
        _apiKey = apiKey;
    }

    public EmbeddingGeneratorMetadata Metadata => new("Gemini", new Uri("https://generativelanguage.googleapis.com"), _model);

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Embedding<float>>();
        var valuesList = values.ToList();
        if (valuesList.Count == 0) return new GeneratedEmbeddings<Embedding<float>>(results);

        var modelPath = _model.StartsWith("models/") ? _model : $"models/{_model}";
        var url = $"https://generativelanguage.googleapis.com/v1beta/{modelPath}:batchEmbedContents";
        Console.WriteLine($"[GeminiEmbed] POST {url} | Batch Size={valuesList.Count} | keyEmpty={string.IsNullOrEmpty(_apiKey)}");

        var requests = valuesList.Select(text => new
        {
            model = modelPath,
            content = new { parts = new[] { new { text } } }
        }).ToArray();

        var body = new { requests };

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (!string.IsNullOrEmpty(_apiKey))
        {
            request.Headers.Add("x-goog-api-key", _apiKey);
        }
        request.Headers.Add("x-target-model", _model); // Tell Rotation Handler which model
        request.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Respect retry-after or default 30s
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(30);
            Console.WriteLine($"[GeminiEmbed] 429 rate limit, waiting {retryAfter.TotalSeconds}s...");
            await Task.Delay(retryAfter, cancellationToken);

            // Retry once
            request = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("x-goog-api-key", _apiKey);
            }
            request.Headers.Add("x-target-model", _model);
            request.Content = JsonContent.Create(body);
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[GeminiEmbed] ERROR {(int)response.StatusCode}: {errorBody}");
        }
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiBatchEmbedResponse>(cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException("Empty batch embedding response from Gemini.");

        foreach (var embed in result.Embeddings)
        {
            results.Add(new Embedding<float>(embed.Values));
        }

        return new GeneratedEmbeddings<Embedding<float>>(results);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    private sealed class GeminiBatchEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<GeminiEmbedValues> Embeddings { get; set; } = new();
    }

    private sealed class GeminiEmbedValues
    {
        [JsonPropertyName("values")]
        public float[] Values { get; set; } = [];
    }
}
