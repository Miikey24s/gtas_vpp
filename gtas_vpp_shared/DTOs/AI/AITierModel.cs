namespace gtas_vpp_shared.DTOs.AI;

public class AITierModel
{
    public int TierLevel { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ActualModelName { get; set; } = string.Empty;
    public string Category { get; set; } = "Text-to-text / Vision";
    public string Role { get; set; } = string.Empty;
    public int MaxRpd { get; set; }
    public int RpmLimit { get; set; }
    public string ContextWindow { get; set; } = string.Empty;
    public string Pricing { get; set; } = "Free Tier ($0.00)";
    public string Features { get; set; } = string.Empty;
}
