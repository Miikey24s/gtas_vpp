using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.Service.Services;

public static class PriceListColumnMappingServiceCollectionExtensions
{
    public static IServiceCollection AddPriceListColumnMapping(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PriceListColumnMappingOptions>()
            .Bind(configuration.GetSection(PriceListColumnMappingOptions.SectionName))
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.Model),
                "PriceListImport:ColumnMapping:Model is required when AI mapping is enabled.")
            .Validate(options => !options.Enabled
                || Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "PriceListImport:ColumnMapping:BaseUrl must be an absolute URI.")
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 60,
                "PriceListImport:ColumnMapping:TimeoutSeconds must be between 5 and 60.")
            .Validate(options => options.MaxOutputTokens is >= 200 and <= 1_500,
                "PriceListImport:ColumnMapping:MaxOutputTokens must be between 200 and 1500.")
            .ValidateOnStart();

        services.AddSingleton<IPriceListColumnMappingSecretResolver,
            ConfigurationPriceListColumnMappingSecretResolver>();
        services.AddHttpClient<GeminiPriceListColumnMappingSuggester>(client =>
            client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddTransient<IPriceListColumnMappingSuggester>(provider =>
            provider.GetRequiredService<GeminiPriceListColumnMappingSuggester>());

        return services;
    }
}
