namespace gtas_vpp_shared.DTOs.AI;

public class AISuggestedItemDTO
{
    public Guid VPPId { get; set; }
    public string VPPCode { get; set; } = string.Empty;
    public string VPPName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UOMName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
