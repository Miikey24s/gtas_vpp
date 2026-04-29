namespace gtas_vpp_be.AI.KeyManagement.Interfaces;

public interface IApiKeyRotationService
{
    string GetNextKey();
    string GetNextKeyForModel(string targetModel);
    void InitializeKeys(IEnumerable<string> keys);
    void UpdateActiveKeys(IEnumerable<string> activeKeys);
}
