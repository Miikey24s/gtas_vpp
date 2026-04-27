namespace gtas_vpp_be.Service.AI;

public interface IAIOrchestrator
{
    Task<AIChatResponse> ChatAsync(AIChatRequest request, CancellationToken ct = default);
    IReadOnlyList<AIUseCaseInfo> GetAvailableUseCases();
}
