using System.Net;
using System.Text.Json;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportInsightServiceTests
{
    [Fact]
    public async Task GenerateAsync_UsesDeterministicFallbackWhenFeatureIsDisabled()
    {
        var provider = new FakeProvider(ReportInsightSources.Groq, configured: true);
        var service = CreateService([provider], enabled: false);

        var result = await service.GenerateAsync(CreateSummary(), "en");

        Assert.False(result.IsAiGenerated);
        Assert.Equal(ReportInsightSources.Rules, result.Source);
        Assert.NotEmpty(result.Highlights);
        Assert.NotEmpty(result.Recommendations);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_UsesPriorityProviderAndOnlySendsAggregateJson()
    {
        var provider = new FakeProvider(ReportInsightSources.Groq, configured: true)
        {
            ResponseJson = CreateAiJson()
        };
        var service = CreateService([provider], enabled: true, priority: [ReportInsightSources.Groq]);

        var result = await service.GenerateAsync(CreateSummary(), "en");

        Assert.True(result.IsAiGenerated);
        Assert.Equal(ReportInsightSources.Groq, result.Source);
        Assert.Equal("qwen/qwen3-32b", result.Model);
        Assert.Equal("Aggregate demand increased.", result.Summary);
        Assert.Equal(4, result.Highlights.Count);
        Assert.NotNull(provider.LastPrompt);
        Assert.Contains("totalOrders", provider.LastPrompt!.AggregateJson, StringComparison.Ordinal);
        Assert.DoesNotContain("UserLogin", provider.LastPrompt.AggregateJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_ContinuesToNextProviderWhenFirstProviderIsRateLimited()
    {
        var groq = new FakeProvider(ReportInsightSources.Groq, configured: true)
        {
            FailureKind = ReportInsightFailureKind.RateLimited
        };
        var gemini = new FakeProvider(ReportInsightSources.Gemini, configured: true)
        {
            ResponseJson = CreateAiJson()
        };
        var service = CreateService(
            [groq, gemini],
            enabled: true,
            priority: [ReportInsightSources.Groq, ReportInsightSources.Gemini]);

        var result = await service.GenerateAsync(CreateSummary(), "vi");

        Assert.Equal(ReportInsightSources.Gemini, result.Source);
        Assert.Equal(1, groq.CallCount);
        Assert.Equal(1, gemini.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_StillSupportsLegacyOpenAiConfiguration()
    {
        var provider = new FakeProvider(ReportInsightSources.OpenAI, configured: true)
        {
            ResponseJson = CreateAiJson()
        };
        var service = CreateService(
            [provider],
            enabled: true,
            priority: [ReportInsightSources.Groq, ReportInsightSources.Gemini, ReportInsightSources.Ollama, ReportInsightSources.OpenAI]);

        var result = await service.GenerateAsync(CreateSummary(), "vi");

        Assert.Equal(ReportInsightSources.OpenAI, result.Source);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_UsesRulesWhenProviderReturnsInvalidStructuredOutput()
    {
        var provider = new FakeProvider(ReportInsightSources.Groq, configured: true)
        {
            ResponseJson = "not-json"
        };
        var service = CreateService([provider], enabled: true, priority: [ReportInsightSources.Groq]);

        var result = await service.GenerateAsync(CreateSummary(), "vi");

        Assert.False(result.IsAiGenerated);
        Assert.Equal(ReportInsightSources.Rules, result.Source);
    }

    [Fact]
    public async Task GenerateAsync_EnforcesConfiguredDailyProviderLimit()
    {
        var provider = new FakeProvider(ReportInsightSources.Groq, configured: true)
        {
            ResponseJson = CreateAiJson()
        };
        var service = CreateService(
            [provider],
            enabled: true,
            priority: [ReportInsightSources.Groq],
            dailyLimit: 1);

        var first = await service.GenerateAsync(CreateSummary(), "vi");
        var second = await service.GenerateAsync(CreateSummary(), "vi");

        Assert.True(first.IsAiGenerated);
        Assert.False(second.IsAiGenerated);
        Assert.Equal(1, provider.CallCount);
    }

    private static ReportInsightService CreateService(
        IEnumerable<IReportInsightProvider> providers,
        bool enabled,
        IReadOnlyList<string>? priority = null,
        int dailyLimit = 0)
    {
        var providerOptions = new Dictionary<string, ReportInsightProviderOptions>(StringComparer.OrdinalIgnoreCase)
        {
            [ReportInsightSources.Groq] = new()
            {
                Enabled = true,
                Model = "qwen/qwen3-32b",
                DailyRequestLimit = dailyLimit
            },
            [ReportInsightSources.Gemini] = new() { Enabled = true, Model = "gemini-2.5-flash-lite" },
            [ReportInsightSources.OpenAI] = new() { Enabled = true, Model = "gpt-5.6-luna" }
        };
        var options = Options.Create(new ReportInsightsOptions
        {
            Enabled = enabled,
            ProviderPriority = priority?.ToList() ?? [ReportInsightSources.Groq],
            Providers = providerOptions,
            TimeoutSeconds = 20,
            MaxOutputTokens = 700,
            MaxProvidersPerRequest = 3
        });

        return new ReportInsightService(
            providers,
            options,
            new ReportInsightQuotaGate(),
            NullLogger<ReportInsightService>.Instance);
    }

    private static string CreateAiJson() => JsonSerializer.Serialize(new
    {
        summary = "Aggregate demand increased.",
        highlights = new[] { "One", "Two", "Three", "Four", "Five" },
        risks = new[] { "Concentration" },
        recommendations = new[] { "Review stock" }
    });

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

    private sealed class FakeProvider(string name, bool configured) : IReportInsightProvider
    {
        public string Name { get; } = name;
        public bool IsConfigured { get; } = configured;
        public int CallCount { get; private set; }
        public ReportInsightPrompt? LastPrompt { get; private set; }
        public string? ResponseJson { get; set; }
        public ReportInsightFailureKind FailureKind { get; set; }

        public Task<ReportInsightProviderResult> GenerateAsync(
            ReportInsightPrompt prompt,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastPrompt = prompt;
            return Task.FromResult(new ReportInsightProviderResult(
                Name,
                Name == ReportInsightSources.Groq ? "qwen/qwen3-32b" : "gemini-2.5-flash-lite",
                ResponseJson,
                FailureKind));
        }
    }
}
