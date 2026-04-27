namespace gtas_vpp_be.Service.AI;

public interface IAIUseCaseHandler
{
    string UseCaseId { get; }
    string DisplayName { get; }
    bool CanHandle(string userMessage);
    Task<AIChatResponse> HandleAsync(AIChatRequest request, CancellationToken ct = default);
}
