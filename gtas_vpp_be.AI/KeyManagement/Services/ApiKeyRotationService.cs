using System.Collections.Concurrent;
using gtas_vpp_be.AI.KeyManagement.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace gtas_vpp_be.AI.KeyManagement.Services;

public class ApiKeyRotationService : IApiKeyRotationService
{
    private ConcurrentQueue<string> _keys = new();
    private readonly IServiceProvider _serviceProvider;

    public ApiKeyRotationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string GetNextKeyForModel(string targetModel)
    {
        if (_keys.IsEmpty)
            throw new InvalidOperationException("No API keys available in the rotation pool.");

        var keyManager = _serviceProvider.GetRequiredService<IGeminiKeyManager>();
        int totalKeys = _keys.Count;

        for (int i = 0; i < totalKeys; i++)
        {
            if (_keys.TryDequeue(out var key))
            {
                _keys.Enqueue(key); // Round Robin
                if (!keyManager.IsKeyExhausted(key, targetModel))
                {
                    return key;
                }
            }
        }
        
        throw new InvalidOperationException($"All {totalKeys} keys are exhausted for model {targetModel}.");
    }

    public string GetNextKey()
    {
        if (_keys.IsEmpty)
            throw new InvalidOperationException("No API keys available in the rotation pool.");

        if (_keys.TryDequeue(out var key))
        {
            _keys.Enqueue(key);
            return key;
        }

        return _keys.FirstOrDefault() ?? throw new InvalidOperationException("Failed to dequeue API key.");
    }

    public void InitializeKeys(IEnumerable<string> keys)
    {
        _keys = new ConcurrentQueue<string>(keys);
    }

    public void UpdateActiveKeys(IEnumerable<string> activeKeys)
    {
        _keys = new ConcurrentQueue<string>(activeKeys);
    }
}
