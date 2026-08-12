namespace gtas_vpp_be.Service.Services;

public sealed class PriceListColumnMappingOptions
{
    public const string SectionName = "PriceListImport:ColumnMapping";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";

    public string Model { get; set; } = "gemini-2.5-flash-lite";

    public int TimeoutSeconds { get; set; } = 15;

    public int MaxOutputTokens { get; set; } = 600;
}

public interface IPriceListColumnMappingSecretResolver
{
    string? ResolveGeminiApiKey();
}

public sealed class ConfigurationPriceListColumnMappingSecretResolver(
    Microsoft.Extensions.Configuration.IConfiguration configuration)
    : IPriceListColumnMappingSecretResolver
{
    public string? ResolveGeminiApiKey()
    {
        var configured = configuration[$"{PriceListColumnMappingOptions.SectionName}:ApiKey"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var geminiKey = configuration["GEMINI_API_KEY"];
        return !string.IsNullOrWhiteSpace(geminiKey)
            ? geminiKey
            : configuration["GOOGLE_API_KEY"];
    }
}
