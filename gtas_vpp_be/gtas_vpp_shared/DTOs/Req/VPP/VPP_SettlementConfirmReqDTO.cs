namespace gtas_vpp_shared.DTOs.Req.VPP;

public class VPP_SettlementConfirmReqDTO
{
    public int Y { get; set; }
    public int M { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public Guid PrimarySupplierId { get; set; }
    public Guid PriceListId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<VPP_SettlementExceptionReqDTO> Exceptions { get; set; } = [];
}

public sealed class VPP_SettlementCorrectionReqDTO : VPP_SettlementConfirmReqDTO
{
    public string? Reason { get; set; }
}
