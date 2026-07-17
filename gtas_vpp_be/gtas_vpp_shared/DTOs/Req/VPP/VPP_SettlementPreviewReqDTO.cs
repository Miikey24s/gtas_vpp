namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class VPP_SettlementPreviewReqDTO
{
    public int Y { get; set; }
    public int M { get; set; }
    public DateTime? PriceAsOfUtc { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? PrimarySupplierId { get; set; }
    public List<VPP_SettlementExceptionReqDTO> Exceptions { get; set; } = [];
}

public sealed class VPP_SettlementExceptionReqDTO
{
    public Guid VppId { get; set; }
    public Guid SupplierId { get; set; }
    public string? Reason { get; set; }
}
