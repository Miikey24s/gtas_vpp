namespace gtas_vpp_shared.DTOs.Req.VPP;

public class SettlementConfirmReqDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public Guid PrimarySupplierId { get; set; }
    public Guid PriceListId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public List<SettlementExceptionReqDTO> Exceptions { get; set; } = [];
}

public sealed class SettlementCorrectionReqDTO : SettlementConfirmReqDTO
{
    public string? Reason { get; set; }
}
