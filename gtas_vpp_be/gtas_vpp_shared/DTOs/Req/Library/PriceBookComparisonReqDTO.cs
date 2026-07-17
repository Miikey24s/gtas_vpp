namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class PriceBookComparisonReqDTO
{
    public DateTime PriceAsOfUtc { get; set; }
    public List<PriceBookComparisonItemReqDTO> Items { get; set; } = [];
    public List<Guid>? SupplierIds { get; set; }
}

public sealed class PriceBookComparisonItemReqDTO
{
    public Guid VppId { get; set; }
    public decimal Quantity { get; set; }
}
