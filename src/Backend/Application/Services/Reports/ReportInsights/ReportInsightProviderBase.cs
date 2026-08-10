using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public abstract class ReportInsightProviderBase : IReportInsightProvider
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ReportInsightsOptions _options;

    protected ReportInsightProviderBase(
        HttpClient httpClient,
        IOptions<ReportInsightsOptions> options,
        IReportInsightSecretResolver secrets,
        ILogger logger)
    {
        HttpClient = httpClient;
        _options = options.Value;
        Secrets = secrets;
        Logger = logger;
    }

    protected HttpClient HttpClient { get; }

    protected IReportInsightSecretResolver Secrets { get; }

    protected ILogger Logger { get; }

    protected ReportInsightsOptions RootOptions => _options;

    public abstract string Name { get; }

    public virtual bool IsConfigured => ProviderOptions.Enabled &&
        (RequiresApiKey ? !string.IsNullOrWhiteSpace(ApiKey) : ProviderOptions.Enabled);

    protected abstract bool RequiresApiKey { get; }

    public abstract Task<ReportInsightProviderResult> GenerateAsync(
        ReportInsightPrompt prompt,
        CancellationToken cancellationToken = default);

    protected ReportInsightProviderOptions ProviderOptions
    {
        get
        {
            var configured = _options.GetProvider(Name);
            ApplyDefaults(configured);
            return configured;
        }
    }

    protected string? ApiKey => Secrets.Resolve(Name);

    protected Uri BuildUri(string path)
    {
        var baseUrl = ProviderOptions.BaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/{path.TrimStart('/')}", UriKind.Absolute);
    }

    protected static ReportInsightProviderResult Failure(
        string provider,
        string? model,
        HttpResponseMessage response,
        string? body = null)
    {
        var kind = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ReportInsightFailureKind.Unauthorized,
            HttpStatusCode.TooManyRequests => ReportInsightFailureKind.RateLimited,
            HttpStatusCode.RequestTimeout or >= HttpStatusCode.InternalServerError => ReportInsightFailureKind.Unavailable,
            _ => ReportInsightFailureKind.Error
        };

        return new ReportInsightProviderResult(
            provider,
            model,
            null,
            kind,
            (int)response.StatusCode,
            ClampError(body));
    }

    protected static ReportInsightProviderResult Error(
        string provider,
        string? model,
        ReportInsightFailureKind kind,
        Exception exception) => new(
        provider,
        model,
        null,
        kind,
        null,
        ClampError(exception.Message));

    protected static string? ClampError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        return text.Length <= 400 ? text : text[..400];
    }

    protected async Task<ReportInsightProviderResult> HandleResponseAsync(
        HttpResponseMessage response,
        string model,
        Func<JsonDocument, string?> extract,
        CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var output = extract(document);
        return string.IsNullOrWhiteSpace(output)
            ? new ReportInsightProviderResult(
                Name,
                model,
                null,
                ReportInsightFailureKind.InvalidResponse,
                (int)response.StatusCode,
                "Provider response did not contain JSON output.")
            : new ReportInsightProviderResult(Name, model, output);
    }

    private void ApplyDefaults(ReportInsightProviderOptions providerOptions)
    {
        if (string.Equals(Name, AiProviderNames.OpenAI, StringComparison.OrdinalIgnoreCase))
        {
            providerOptions.BaseUrl = string.IsNullOrWhiteSpace(providerOptions.BaseUrl)
                ? "https://api.openai.com"
                : providerOptions.BaseUrl;
            providerOptions.Model = string.IsNullOrWhiteSpace(providerOptions.Model)
                ? _options.Model
                : providerOptions.Model;
        }
        else if (string.Equals(Name, AiProviderNames.Groq, StringComparison.OrdinalIgnoreCase))
        {
            providerOptions.BaseUrl = string.IsNullOrWhiteSpace(providerOptions.BaseUrl)
                ? "https://api.groq.com/openai/v1"
                : providerOptions.BaseUrl;
            providerOptions.Model = string.IsNullOrWhiteSpace(providerOptions.Model)
                ? "qwen/qwen3-32b"
                : providerOptions.Model;
        }
        else if (string.Equals(Name, AiProviderNames.Gemini, StringComparison.OrdinalIgnoreCase))
        {
            providerOptions.BaseUrl = string.IsNullOrWhiteSpace(providerOptions.BaseUrl)
                ? "https://generativelanguage.googleapis.com/v1beta"
                : providerOptions.BaseUrl;
            providerOptions.Model = string.IsNullOrWhiteSpace(providerOptions.Model)
                ? "gemini-2.5-flash-lite"
                : providerOptions.Model;
        }
        else if (string.Equals(Name, AiProviderNames.Ollama, StringComparison.OrdinalIgnoreCase))
        {
            providerOptions.BaseUrl = string.IsNullOrWhiteSpace(providerOptions.BaseUrl)
                ? "http://localhost:11434"
                : providerOptions.BaseUrl;
            providerOptions.Model = string.IsNullOrWhiteSpace(providerOptions.Model)
                ? "qwen3:8b"
                : providerOptions.Model;
        }
    }
}
