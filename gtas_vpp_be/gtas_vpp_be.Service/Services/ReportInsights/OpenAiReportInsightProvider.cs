using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class OpenAiReportInsightProvider(
    HttpClient httpClient,
    IOptions<ReportInsightsOptions> options,
    IReportInsightSecretResolver secrets,
    ILogger<OpenAiReportInsightProvider> logger)
    : ReportInsightProviderBase(httpClient, options, secrets, logger)
{
    public override string Name => AiProviderNames.OpenAI;

    protected override bool RequiresApiKey => true;

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
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("v1/responses"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                store = false,
                max_output_tokens = prompt.MaxOutputTokens,
                instructions = prompt.Instructions,
                input = prompt.AggregateJson,
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "report_insight",
                        strict = true,
                        schema = prompt.OutputSchema
                    }
                }
            }, options: JsonOptions);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Failure(Name, model, response, body);
            }

            return await HandleResponseAsync(response, model, ExtractOutputText, cancellationToken);
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

    private static string? ExtractOutputText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("output", out var output)
            || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type)
                    && type.GetString() == "output_text"
                    && contentItem.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}
