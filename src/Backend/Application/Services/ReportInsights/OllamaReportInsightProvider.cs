using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class OllamaReportInsightProvider(
    HttpClient httpClient,
    IOptions<ReportInsightsOptions> options,
    IReportInsightSecretResolver secrets,
    ILogger<OllamaReportInsightProvider> logger)
    : ReportInsightProviderBase(httpClient, options, secrets, logger)
{
    public override string Name => AiProviderNames.Ollama;

    protected override bool RequiresApiKey => false;

    public override async Task<ReportInsightProviderResult> GenerateAsync(
        ReportInsightPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        var settings = ProviderOptions;
        var model = settings.Model;
        if (!IsConfigured)
        {
            return new(Name, model, null, ReportInsightFailureKind.NotConfigured);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("api/chat"));
            request.Content = JsonContent.Create(new
            {
                model,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = prompt.Instructions },
                    new { role = "user", content = prompt.AggregateJson }
                },
                format = prompt.OutputSchema,
                options = new
                {
                    temperature = 0.2,
                    num_predict = prompt.MaxOutputTokens
                }
            }, options: JsonOptions);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Failure(Name, model, response, body);
            }

            return await HandleResponseAsync(response, model, ExtractMessageText, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error(Name, model, ReportInsightFailureKind.Timeout, new TimeoutException("Provider request timed out."));
        }
        catch (HttpRequestException exception)
        {
            return Error(Name, model, ReportInsightFailureKind.Unavailable, exception);
        }
        catch (JsonException exception)
        {
            return Error(Name, model, ReportInsightFailureKind.InvalidResponse, exception);
        }
    }

    private static string? ExtractMessageText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.GetString();
    }
}
