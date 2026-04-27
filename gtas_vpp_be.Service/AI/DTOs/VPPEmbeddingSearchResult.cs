namespace gtas_vpp_be.Service.AI;

public class VPPEmbeddingSearchResult
{
    public Guid VPPId { get; set; }
    public string VPPCode { get; set; } = string.Empty;
    public string VPPName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UOMName { get; set; } = string.Empty;
    public string EmbeddingText { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
