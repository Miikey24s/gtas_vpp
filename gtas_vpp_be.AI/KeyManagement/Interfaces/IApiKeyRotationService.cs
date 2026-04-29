namespace gtas_vpp_be.AI.KeyManagement.Interfaces;

public interface IApiKeyRotationService
{
    /// <summary>
    /// Rút một key ra khỏi hàng đợi và nhét lại vào cuối hàng (Round-Robin).
    /// </summary>
    string GetNextKey();
    (string Key, string Model) GetNextKeyAndModel(bool autoMode, string fixedModel);
    void InitializeKeys(IEnumerable<string> keys);
    void UpdateActiveKeys(IEnumerable<string> activeKeys);
}
