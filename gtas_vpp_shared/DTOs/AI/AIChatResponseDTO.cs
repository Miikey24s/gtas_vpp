namespace gtas_vpp_shared.DTOs.AI;

public class AIChatResponseDTO
{
    public string Message { get; set; } = string.Empty;
    public List<AISuggestedItemDTO>? SuggestedItems { get; set; }
    public string UseCaseId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
