namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class PriceBookStatusReqDTO
{
    public byte[]? RowVersion { get; set; }
    public string? Reason { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
}
