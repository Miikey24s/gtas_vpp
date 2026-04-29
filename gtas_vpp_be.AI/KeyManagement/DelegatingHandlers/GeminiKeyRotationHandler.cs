using System.Net;
using gtas_vpp_be.AI.KeyManagement.Interfaces;

namespace gtas_vpp_be.AI.KeyManagement.DelegatingHandlers;

public class GeminiKeyRotationHandler : DelegatingHandler
{
    private readonly IApiKeyRotationService _keyRotationService;
    private readonly IGeminiKeyManager _keyManager;
    private readonly int _maxRetries;
    private readonly TimeSpan _totalTimeout;

    public GeminiKeyRotationHandler(
        IApiKeyRotationService keyRotationService,
        IGeminiKeyManager keyManager,
        int maxRetries = 8,
        TimeSpan? totalTimeout = null)
    {
        _keyRotationService = keyRotationService;
        _keyManager = keyManager;
        _maxRetries = maxRetries;
        _totalTimeout = totalTimeout ?? TimeSpan.FromSeconds(15);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get target model from virtual header (passed from Logic layer)
        var targetModel = request.Headers.TryGetValues("x-target-model", out var values) 
            ? values.FirstOrDefault() ?? "gemini-3.1-flash-lite-preview"
            : "gemini-3.1-flash-lite-preview";

        // Remove virtual header before sending to Google API to avoid Bad Request
        request.Headers.Remove("x-target-model");

        // Cache request body once to prevent memory leak on retries
        byte[]? cachedBody = null;
        if (request.Content != null)
        {
            cachedBody = await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        // Create timeout for entire retry loop
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(_totalTimeout);

        int attempts = 0;
        while (attempts < _maxRetries)
        {
            attempts++;
            
            // Get an available key for the target model
            string apiKey;
            try 
            {
                apiKey = _keyRotationService.GetNextKeyForModel(targetModel);
            }
            catch (InvalidOperationException)
            {
                // Break out if rotation service says all keys are exhausted
                break;
            }

            // Inject API Key into Header
            request.Headers.Remove("x-goog-api-key");
            request.Headers.Remove("Authorization");
            
            bool isOpenAiEndpoint = request.RequestUri != null && request.RequestUri.ToString().Contains("/openai/");
            if (isOpenAiEndpoint)
            {
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
            }
            else
            {
                request.Headers.Add("x-goog-api-key", apiKey);
            }

            // Clone request using cached body
            var clonedRequest = CloneRequest(request, cachedBody);

            // Send actual request
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(clonedRequest, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"AI request timed out after {_totalTimeout.TotalSeconds}s");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests) // 429 TOO MANY REQUESTS
            {
                // Lock this key for 1 minute for this specific model
                _keyManager.MarkKeyExhausted(apiKey, targetModel, TimeSpan.FromMinutes(1));
                
                // If we haven't tried all keys, loop will continue and get the next one
                if (attempts < _maxRetries) continue; 
            }

            // Update key stats
            _keyManager.UpdateKeyStatsFromHeaders(apiKey, response);

            // If not 429, return response immediately (Success or other errors)
            if (response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                return response;
            }
        }

        // IF WE REACH HERE, ALL KEYS ARE 429 FOR THIS MODEL
        throw new InvalidOperationException($"ALL_KEYS_EXHAUSTED_FOR_MODEL:{targetModel}");
    }

    // Helper to clone HTTP Request using cached body (no memory leak)
    private HttpRequestMessage CloneRequest(HttpRequestMessage req, byte[]? cachedBody)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);
        if (cachedBody != null)
        {
            clone.Content = new ByteArrayContent(cachedBody);
            foreach (var h in req.Content!.Headers) clone.Content.Headers.Add(h.Key, h.Value);
        }
        foreach (var h in req.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        return clone;
    }
}
