namespace gtas_vpp_be.Service.AI;

public class AISuggestedItem
{
    public Guid VPPId { get; set; }
    public string VPPCode { get; set; } = string.Empty;
    public string VPPName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UOMName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}

public class AIChatResponse
{
    public string Message { get; set; } = string.Empty;
    public List<AISuggestedItem>? SuggestedItems { get; set; }
    public string UseCaseId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static AIChatResponse Error(string message) =>
        new() { IsSuccess = false, ErrorMessage = message, Message = message };

    public static AIChatResponse Success(string message, string useCaseId, List<AISuggestedItem>? items = null) =>
        new() { IsSuccess = true, Message = message, UseCaseId = useCaseId, SuggestedItems = items };
}
