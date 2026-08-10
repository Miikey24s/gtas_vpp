using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class GeminiReportInsightProvider(
    HttpClient httpClient,
    IOptions<ReportInsightsOptions> options,
    IReportInsightSecretResolver secrets,
    ILogger<GeminiReportInsightProvider> logger)
    : ReportInsightProviderBase(httpClient, options, secrets, logger)
{
    public override string Name => AiProviderNames.Gemini;

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
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildUri($"models/{Uri.EscapeDataString(model)}:generateContent"));
            request.Headers.Add("x-goog-api-key", ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = JsonContent.Create(new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = prompt.Instructions } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = prompt.AggregateJson } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseJsonSchema = prompt.OutputSchema,
                    maxOutputTokens = prompt.MaxOutputTokens,
                    temperature = 0.2
                }
            }, options: JsonOptions);

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return Failure(Name, model, response, body);
            }

            return await HandleResponseAsync(response, model, ExtractCandidateText, cancellationToken);
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

    private static string? ExtractCandidateText(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var first = candidates.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object
            || !first.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text))
            {
                return text.GetString();
            }
        }

        return null;
    }
}
