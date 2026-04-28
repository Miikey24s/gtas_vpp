namespace gtas_vpp_shared.DTOs.AI;

public class AIChatRequestDTO
{
    public string Message { get; set; } = string.Empty;
    public List<AIChatMessageDTO>? History { get; set; }
}
