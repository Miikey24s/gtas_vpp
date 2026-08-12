using System.Net;
using System.Text;
using System.Text.Json;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public sealed class GeminiPriceListColumnMappingSuggesterTests
{
    [Fact]
    public void SecretResolver_UsesGeminiEnvironmentConfigurationWithoutExposingItInOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GEMINI_API_KEY"] = "environment-key"
            })
            .Build();

        var resolver = new ConfigurationPriceListColumnMappingSecretResolver(configuration);

        Assert.Equal("environment-key", resolver.ResolveGeminiApiKey());
    }

    [Fact]
    public async Task SuggestAsync_SendsOnlyUnknownColumnsAndKeepsHighConfidenceUniqueTargets()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal("test-key", request.Headers.GetValues("x-goog-api-key").Single());
            Assert.Contains("gemini-2.5-flash-lite:generateContent", request.RequestUri!.AbsoluteUri);

            var body = await request.Content!.ReadAsStringAsync();
            Assert.Contains("responseJsonSchema", body, StringComparison.Ordinal);
            Assert.Contains("Ma noi bo", body, StringComparison.Ordinal);
            Assert.Contains("Gia ban", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Ghi chu", body, StringComparison.Ordinal);
            return JsonResponse(new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = JsonSerializer.Serialize(new
                                    {
                                        suggestions = new object[]
                                        {
                                            new { columnIndex = 0, targetField = "ItemCode", confidence = 0.98m },
                                            new { columnIndex = 1, targetField = "UnitPrice", confidence = 0.93m },
                                            new { columnIndex = 2, targetField = "Note", confidence = 0.99m },
                                            new { columnIndex = 3, targetField = "VatRate", confidence = 0.50m },
                                            new { columnIndex = 4, targetField = "ItemCode", confidence = 0.96m }
                                        }
                                    })
                                }
                            }
                        }
                    }
                }
            });
        });
        var suggester = CreateSuggester(handler, "test-key");

        var suggestions = await suggester.SuggestAsync(
        [
            Column(0, "Ma noi bo", null, "VPP_001"),
            Column(1, "Gia ban", null, "25000"),
            Column(2, "Ghi chu", "Note", "Hang moi"),
            Column(3, "Thue suat", null, "8"),
            Column(4, "Ma khac", null, "VPP_002")
        ]);

        Assert.True(suggester.IsAvailable);
        Assert.Collection(
            suggestions.OrderBy(suggestion => suggestion.ColumnIndex),
            suggestion =>
            {
                Assert.Equal(0, suggestion.ColumnIndex);
                Assert.Equal("ItemCode", suggestion.TargetField);
            },
            suggestion =>
            {
                Assert.Equal(1, suggestion.ColumnIndex);
                Assert.Equal("UnitPrice", suggestion.TargetField);
            });
    }

    [Fact]
    public async Task SuggestAsync_WhenKeyMissing_DoesNotCallProvider()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("Provider must not be called."));
        var suggester = CreateSuggester(handler, null);

        var suggestions = await suggester.SuggestAsync([Column(0, "Gia ban", null, "25000")]);

        Assert.False(suggester.IsAvailable);
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task SuggestAsync_WhenProviderFails_ReturnsManualFallback()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var suggester = CreateSuggester(handler, "test-key");

        var suggestions = await suggester.SuggestAsync([Column(0, "Gia ban", null, "25000")]);

        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task SuggestAsync_WhenResponseIsInvalid_ReturnsManualFallback()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-json", Encoding.UTF8, "application/json")
        }));
        var suggester = CreateSuggester(handler, "test-key");

        var suggestions = await suggester.SuggestAsync([Column(0, "Gia ban", null, "25000")]);

        Assert.Empty(suggestions);
    }

    private static GeminiPriceListColumnMappingSuggester CreateSuggester(
        HttpMessageHandler handler,
        string? apiKey) => new(
            new HttpClient(handler),
            Options.Create(new PriceListColumnMappingOptions
            {
                Enabled = true,
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
                Model = "gemini-2.5-flash-lite",
                TimeoutSeconds = 15,
                MaxOutputTokens = 600
            }),
            new FakeSecrets(apiKey),
            NullLogger<GeminiPriceListColumnMappingSuggester>.Instance);

    private static PriceListImportSourceColumnResDTO Column(
        int index,
        string source,
        string? suggested,
        params string[] samples) => new()
        {
            ColumnIndex = index,
            SourceColumn = source,
            SuggestedTargetField = suggested,
            SampleValues = samples.ToList()
        };

    private static HttpResponseMessage JsonResponse(object payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
    };

    private sealed class FakeSecrets(string? apiKey) : IPriceListColumnMappingSecretResolver
    {
        public string? ResolveGeminiApiKey() => apiKey;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => handler(request);
    }
}
