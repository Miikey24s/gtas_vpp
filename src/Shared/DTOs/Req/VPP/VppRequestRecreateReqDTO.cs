namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class VppRequestRecreateReqDTO
{
    public string? Description { get; set; }
    public string? SupplementReason { get; set; }
    public byte[]? RowVersion { get; set; }
    public string? IdempotencyKey { get; set; }
    public List<VppRequestDetailItemReqDTO> Items { get; set; } = new();
}
