namespace gtas_vpp_be.Service.AI;

public interface IVPPEmbeddingStore
{
    Task<List<VPPEmbeddingSearchResult>> SearchSimilarAsync(float[] queryVector, int topK, CancellationToken ct = default);
    Task UpsertEmbeddingAsync(Guid vppId, string text, float[] vector, string modelName, CancellationToken ct = default);
    Task RebuildAllAsync(CancellationToken ct = default);
    Task<bool> HasEmbeddingsAsync(CancellationToken ct = default);
}
