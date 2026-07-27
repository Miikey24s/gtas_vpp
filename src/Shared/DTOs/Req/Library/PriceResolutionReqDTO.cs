namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class PriceResolutionReqDTO
{
    public Guid VppId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? LockedPriceListId { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public decimal Quantity { get; set; } = 1m;
}
