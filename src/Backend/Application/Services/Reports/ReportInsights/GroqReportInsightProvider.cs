using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class GroqReportInsightProvider(
    HttpClient httpClient,
    IOptions<ReportInsightsOptions> options,
    IReportInsightSecretResolver secrets,
    ILogger<GroqReportInsightProvider> logger)
    : ReportInsightProviderBase(httpClient, options, secrets, logger)
{
    public override string Name => AiProviderNames.Groq;

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
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("chat/completions"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            request.Content = JsonContent.Create(new
            {
                model,
                temperature = 0.2,
                max_completion_tokens = prompt.MaxOutputTokens,
                messages = new[]
                {
                    new { role = "system", content = prompt.Instructions },
                    new { role = "user", content = prompt.AggregateJson }
                },
                response_format = BuildResponseFormat(settings.StructuredOutputMode, prompt.OutputSchema)
            }, options: JsonOptions);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Failure(Name, model, response, body);
            }

            return await HandleResponseAsync(response, model, ExtractChatText, cancellationToken);
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

    private static object BuildResponseFormat(string mode, object schema) =>
        string.Equals(mode, "json_schema", StringComparison.OrdinalIgnoreCase)
            ? new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "report_insight",
                    strict = true,
                    schema
                }
            }
            : new { type = "json_object" };

    private static string? ExtractChatText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var first = choices.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object
            || !first.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind == JsonValueKind.String ? content.GetString() : null;
    }
}
