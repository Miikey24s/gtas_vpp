using System.Collections.Concurrent;
using gtas_vpp_be.AI.KeyManagement.Interfaces;
using gtas_vpp_shared.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.AI.KeyManagement.Services;

public class GeminiKeyManager : IGeminiKeyManager
{
    private readonly ConcurrentDictionary<string, GeminiKeyInfo> _keyStats = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GeminiKeyManager> _logger;

    public GeminiKeyManager(IHttpClientFactory httpClientFactory, ILogger<GeminiKeyManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public IEnumerable<GeminiKeyInfo> GetAllKeyInfos()
    {
        return _keyStats.Values.ToList();
    }

    public void InitializeKeys(IEnumerable<string> keys, IEnumerable<string>? accounts = null)
    {
        var keyList = keys.ToList();
        var accountList = accounts?.ToList() ?? new List<string>();

        for (int i = 0; i < keyList.Count; i++)
        {
            var key = keyList[i];
            var email = (i < accountList.Count) ? accountList[i] : string.Empty;
            _keyStats.GetOrAdd(key, k => new GeminiKeyInfo { Key = k, OwnerEmail = email });
        }
    }

    public void UpdateKeyStatsFromHeaders(string key, HttpResponseMessage response)
    {
        var info = _keyStats.GetOrAdd(key, k => new GeminiKeyInfo { Key = k });
        info.LastUsed = DateTime.UtcNow;

        // Cập nhật trạng thái
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            info.Status = KeyStatus.RateLimited;
        }
        else if (response.IsSuccessStatusCode)
        {
            info.Status = KeyStatus.Live;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                 response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            info.Status = KeyStatus.Dead;
        }

        // Bóc tách Header Rate Limit của Gemini API
        if (response.Headers.TryGetValues("x-ratelimit-remaining-requests-per-minute", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out int remaining))
            {
                info.RemainingRequests = remaining;
            }
        }

        if (response.Headers.TryGetValues("x-ratelimit-limit-requests-per-minute", out var limitValues))
        {
            if (int.TryParse(limitValues.FirstOrDefault(), out int limit))
            {
                info.LimitRequests = limit;
            }
        }
    }

    public async Task<KeyStatus> CheckKeyHealthAsync(string key)
    {
        try
        {
            // Dùng HttpClient cơ bản để ping thẳng tới Google API
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={key}");

            var info = _keyStats.GetOrAdd(key, k => new GeminiKeyInfo { Key = k });
            
            if (response.IsSuccessStatusCode)
            {
                info.Status = KeyStatus.Live;
                return KeyStatus.Live;
            }
            
            info.Status = KeyStatus.Dead;
            _logger.LogWarning($"API Key {key[..5]}... is DEAD. Status: {response.StatusCode}");
            return KeyStatus.Dead;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking key health");
            return KeyStatus.Dead;
        }
    }

    public void MarkKeyExhausted(string key, string model, TimeSpan cooldown)
    {
        if (_keyStats.TryGetValue(key, out var info))
        {
            info.Cooldowns[model] = DateTime.UtcNow.Add(cooldown);
        }
    }

    public bool IsKeyExhausted(string key, string model)
    {
        if (_keyStats.TryGetValue(key, out var info))
        {
            return info.IsExhaustedForModel(model);
        }
        return false;
    }
}
