namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class SettlementPreviewReqDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime? PriceAsOfUtc { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? PrimarySupplierId { get; set; }
    public List<SettlementExceptionReqDTO> Exceptions { get; set; } = [];
}

public sealed class SettlementExceptionReqDTO
{
    public Guid VppId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PriceListId { get; set; }
    public string? Reason { get; set; }
}
