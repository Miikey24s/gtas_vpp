namespace gtas_vpp_shared.DTOs.AI;

public class GeminiKeyInfo
{
    public string Key { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public KeyStatus Status { get; set; } = KeyStatus.Live;
    public int RemainingRequests { get; set; } = 15; // Mặc định Free Tier là 15 RPM
    public int LimitRequests { get; set; } = 15;
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    
    // Tracking cooldowns per actual model name
    public Dictionary<string, DateTime> Cooldowns { get; set; } = new();
    
    public bool IsExhaustedForModel(string modelName)
    {
        if (Cooldowns.TryGetValue(modelName, out var cooldown))
        {
            return cooldown > DateTime.UtcNow;
        }
        return false;
    }
}
