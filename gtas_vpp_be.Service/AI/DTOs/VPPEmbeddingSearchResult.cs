namespace gtas_vpp_be.Service.AI;

public class VPPEmbeddingSearchResult
{
    public Guid VPPId { get; set; }
    public string EmbeddingText { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
