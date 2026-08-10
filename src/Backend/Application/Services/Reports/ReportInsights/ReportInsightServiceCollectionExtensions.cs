using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.Service.Services;

public static class ReportInsightServiceCollectionExtensions
{
    public static IServiceCollection AddReportInsights(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ReportInsightsOptions>()
            .Bind(configuration.GetSection(ReportInsightsOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 60,
                "ReportInsights:TimeoutSeconds must be between 5 and 60.")
            .Validate(options => options.MaxOutputTokens is >= 300 and <= 1_500,
                "ReportInsights:MaxOutputTokens must be between 300 and 1500.")
            .Validate(options => options.MaxProvidersPerRequest is >= 1 and <= 8,
                "ReportInsights:MaxProvidersPerRequest must be between 1 and 8.")
            .Validate(options => options.Providers.Values.All(provider => provider.DailyRequestLimit >= 0),
                "ReportInsights provider DailyRequestLimit cannot be negative.")
            .Validate(options => options.Providers.Values.All(provider =>
                    string.IsNullOrWhiteSpace(provider.BaseUrl)
                    || Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out _)),
                "ReportInsights provider BaseUrl must be an absolute URI.")
            .Validate(options => options.Providers.Values.All(provider =>
                    string.Equals(provider.StructuredOutputMode, "json_schema", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(provider.StructuredOutputMode, "json_object", StringComparison.OrdinalIgnoreCase)),
                "ReportInsights provider StructuredOutputMode must be json_schema or json_object.")
            .ValidateOnStart();

        services.AddSingleton<IReportInsightSecretResolver, ConfigurationReportInsightSecretResolver>();
        services.AddSingleton<ReportInsightQuotaGate>();

        services.AddHttpClient<OpenAiReportInsightProvider>(ConfigureHttpClient);
        services.AddHttpClient<GroqReportInsightProvider>(ConfigureHttpClient);
        services.AddHttpClient<GeminiReportInsightProvider>(ConfigureHttpClient);
        services.AddHttpClient<OllamaReportInsightProvider>(ConfigureHttpClient);

        services.AddTransient<IReportInsightProvider>(provider =>
            provider.GetRequiredService<OpenAiReportInsightProvider>());
        services.AddTransient<IReportInsightProvider>(provider =>
            provider.GetRequiredService<GroqReportInsightProvider>());
        services.AddTransient<IReportInsightProvider>(provider =>
            provider.GetRequiredService<GeminiReportInsightProvider>());
        services.AddTransient<IReportInsightProvider>(provider =>
            provider.GetRequiredService<OllamaReportInsightProvider>());
        services.AddScoped<IReportInsightService, ReportInsightService>();

        return services;
    }

    private static void ConfigureHttpClient(HttpClient client) =>
        client.Timeout = Timeout.InfiniteTimeSpan;
}
