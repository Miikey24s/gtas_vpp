namespace gtas_vpp_be.Service.AI;

public record AIChatMessage(string Role, string Content);

public class AIChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<AIChatMessage>? ConversationHistory { get; set; }
    public string? UseCaseHint { get; set; }
}
