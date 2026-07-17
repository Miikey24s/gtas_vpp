using System.Net;
using System.Text;
using System.Text.Json;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportInsightServiceTests
{
    [Fact]
    public async Task GenerateAsync_UsesDeterministicFallbackWhenFeatureIsDisabled()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var service = CreateService(handler, enabled: false, apiKey: null);

        var result = await service.GenerateAsync(CreateSummary(), "en");

        Assert.False(result.IsAiGenerated);
        Assert.Equal(ReportInsightSources.Rules, result.Source);
        Assert.NotEmpty(result.Highlights);
        Assert.NotEmpty(result.Recommendations);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_UsesStructuredOpenAiOutputWithoutSendingRawOrders()
    {
        string? requestBody = null;
        var generated = JsonSerializer.Serialize(new
        {
            summary = "Aggregate demand increased.",
            highlights = new[] { "One", "Two", "Three", "Four", "Five" },
            risks = new[] { "Concentration" },
            recommendations = new[] { "Review stock" }
        });
        var responseBody = JsonSerializer.Serialize(new
        {
            output = new[]
            {
                new
                {
                    content = new[]
                    {
                        new { type = "output_text", text = generated }
                    }
                }
            }
        });
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-key", request.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(handler, enabled: true, apiKey: "test-key");

        var result = await service.GenerateAsync(CreateSummary(), "en");

        Assert.True(result.IsAiGenerated);
        Assert.Equal("gpt-5.6-luna", result.Model);
        Assert.Equal("Aggregate demand increased.", result.Summary);
        Assert.Equal(4, result.Highlights.Count);
        Assert.NotNull(requestBody);
        Assert.Contains("\"store\":false", requestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("UserLogin", requestBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_FallsBackWhenOpenAiReturnsAnError()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var service = CreateService(handler, enabled: true, apiKey: "test-key");

        var result = await service.GenerateAsync(CreateSummary(), "vi");

        Assert.False(result.IsAiGenerated);
        Assert.Equal(1, handler.CallCount);
        Assert.NotEmpty(result.Summary);
    }

    private static ReportInsightService CreateService(
        HttpMessageHandler handler,
        bool enabled,
        string? apiKey)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        var configurationValues = new Dictionary<string, string?>
        {
            ["OPENAI_API_KEY"] = apiKey
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var options = Options.Create(new ReportInsightsOptions
        {
            Enabled = enabled,
            Model = "gpt-5.6-luna",
            TimeoutSeconds = 20,
            MaxOutputTokens = 700
        });

        return new ReportInsightService(
            httpClient,
            options,
            configuration,
            NullLogger<ReportInsightService>.Instance);
    }

    private static ReportSummaryResDTO CreateSummary() => new()
    {
        Scope = ReportScopes.All,
        Year = 2026,
        TotalOrders = 4,
        TotalDepartments = 2,
        TotalRequesters = 3,
        TotalLines = 5,
        TotalQuantity = 100,
        TotalAmount = 2_000_000,
        PeriodTrend =
        [
            new() { Year = 2026, Month = 1, Period = "01/2026", TotalQuantity = 20, TotalAmount = 400_000 },
            new() { Year = 2026, Month = 2, Period = "02/2026", TotalQuantity = 30, TotalAmount = 600_000 }
        ],
        DepartmentBreakdown =
        [
            new() { Code = "IT", OrderCount = 3, TotalQuantity = 80, TotalAmount = 1_500_000 }
        ],
        TopProducts =
        [
            new() { ProductCode = "P001", ProductName = "Paper", TotalQuantity = 60, TotalAmount = 1_000_000 }
        ]
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler = handler;

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return _handler(request);
        }
    }
}
