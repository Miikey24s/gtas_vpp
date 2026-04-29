using System.Text.Json.Nodes;
using gtas_vpp_be.AI.KeyManagement.Interfaces;

namespace gtas_vpp_be.AI.KeyManagement.DelegatingHandlers;

public class GeminiKeyRotationHandler : DelegatingHandler
{
    private readonly IApiKeyRotationService _keyRotationService;
    private readonly IGeminiKeyManager _keyManager;

    public GeminiKeyRotationHandler(
        IApiKeyRotationService keyRotationService,
        IGeminiKeyManager keyManager)
    {
        _keyRotationService = keyRotationService;
        _keyManager = keyManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Rút Key và Model phù hợp (Luôn là gemini-3.1-flash-lite-preview từ Service)
        var (apiKey, model) = _keyRotationService.GetNextKeyAndModel(false, "gemini-3.1-flash-lite-preview");

        // 2. Viết lại Body (Luôn ép thành model Gemini 3.1 Flash Lite)
        if (request.Content != null)
        {
            var jsonString = await request.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                var jsonNode = JsonNode.Parse(jsonString);
                if (jsonNode != null && jsonNode["model"] != null)
                {
                    jsonNode["model"] = model;
                    request.Content = new StringContent(jsonNode.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
                }
            }
            catch {}
        }

        // 3. Inject key mới
        request.Headers.Remove("x-goog-api-key");
        request.Headers.Add("x-goog-api-key", apiKey);

        // 4. Gửi Request
        var response = await base.SendAsync(request, cancellationToken);
        
        // 5. Nếu bị 429, khóa Key cho model này trong 1 phút
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _keyManager.MarkKeyExhausted(apiKey, model, TimeSpan.FromMinutes(1));
        }
        
        // 6. Cập nhật thống kê
        _keyManager.UpdateKeyStatsFromHeaders(apiKey, response);
        
        return response;
    }
}
