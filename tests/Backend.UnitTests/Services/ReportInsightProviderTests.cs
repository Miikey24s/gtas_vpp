using System.Net;
using System.Text;
using System.Text.Json;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportInsightProviderTests
{
    [Fact]
    public async Task GroqProvider_SendsBearerTokenAndParsesChatResponse()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("groq-test", request.Headers.Authorization?.Parameter);
            Assert.EndsWith("/chat/completions", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("qwen/qwen3-32b", body, StringComparison.Ordinal);
            return JsonResponse(new { choices = new[] { new { message = new { content = "{\"summary\":\"ok\"}" } } } });
        });
        var provider = new GroqReportInsightProvider(
            new HttpClient(handler),
            Options.Create(OptionsFor(ReportInsightSources.Groq, "https://api.groq.com/openai/v1", "qwen/qwen3-32b")),
            new FakeSecrets((ReportInsightSources.Groq, "groq-test")),
            NullLogger<GroqReportInsightProvider>.Instance);

        var result = await provider.GenerateAsync(CreatePrompt());

        Assert.True(result.IsSuccess);
        Assert.Equal("{\"summary\":\"ok\"}", result.ResponseJson);
    }

    [Fact]
    public async Task GeminiProvider_UsesApiKeyHeaderAndJsonSchema()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal("gemini-test", request.Headers.GetValues("x-goog-api-key").Single());
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("responseJsonSchema", body, StringComparison.Ordinal);
            return JsonResponse(new
            {
                candidates = new[]
                {
                    new { content = new { parts = new[] { new { text = "{\"summary\":\"ok\"}" } } } }
                }
            });
        });
        var provider = new GeminiReportInsightProvider(
            new HttpClient(handler),
            Options.Create(OptionsFor(ReportInsightSources.Gemini, "https://generativelanguage.googleapis.com/v1beta", "gemini-2.5-flash-lite")),
            new FakeSecrets((ReportInsightSources.Gemini, "gemini-test")),
            NullLogger<GeminiReportInsightProvider>.Instance);

        var result = await provider.GenerateAsync(CreatePrompt());

        Assert.True(result.IsSuccess);
        Assert.Equal("gemini-2.5-flash-lite", result.Model);
    }

    [Fact]
    public async Task OllamaProvider_UsesLocalStructuredOutputWithoutApiKey()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("\"format\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Authorization", request.Headers.ToString(), StringComparison.OrdinalIgnoreCase);
            return JsonResponse(new { message = new { content = "{\"summary\":\"ok\"}" } });
        });
        var provider = new OllamaReportInsightProvider(
            new HttpClient(handler),
            Options.Create(OptionsFor(ReportInsightSources.Ollama, "http://localhost:11434", "qwen3:8b")),
            new FakeSecrets(),
            NullLogger<OllamaReportInsightProvider>.Instance);

        var result = await provider.GenerateAsync(CreatePrompt());

        Assert.True(result.IsSuccess);
        Assert.Equal("qwen3:8b", result.Model);
    }

    [Fact]
    public async Task Provider_MapsRateLimitToFallbackFriendlyFailure()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var provider = new GroqReportInsightProvider(
            new HttpClient(handler),
            Options.Create(OptionsFor(ReportInsightSources.Groq, "https://api.groq.com/openai/v1", "qwen/qwen3-32b")),
            new FakeSecrets((ReportInsightSources.Groq, "groq-test")),
            NullLogger<GroqReportInsightProvider>.Instance);

        var result = await provider.GenerateAsync(CreatePrompt());

        Assert.False(result.IsSuccess);
        Assert.Equal(ReportInsightFailureKind.RateLimited, result.FailureKind);
        Assert.Equal(429, result.StatusCode);
    }

    private static ReportInsightsOptions OptionsFor(string provider, string baseUrl, string model) => new()
    {
        Enabled = true,
        ProviderPriority = [provider],
        Providers = new Dictionary<string, ReportInsightProviderOptions>(StringComparer.OrdinalIgnoreCase)
        {
            [provider] = new() { Enabled = true, BaseUrl = baseUrl, Model = model }
        }
    };

    private static ReportInsightPrompt CreatePrompt() => new(
        "vi",
        "Return JSON only.",
        "{\"TotalOrders\":4}",
        ReportInsightSchema.JsonSchema,
        700);

    private static HttpResponseMessage JsonResponse(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    private sealed class FakeSecrets(params (string Name, string Value)[] values) : IReportInsightSecretResolver
    {
        private readonly Dictionary<string, string> _values = values
            .ToDictionary(value => value.Name, value => value.Value, StringComparer.OrdinalIgnoreCase);

        public string? Resolve(string providerName) =>
            _values.TryGetValue(providerName, out var value) ? value : null;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => _handler(request);
    }
}
