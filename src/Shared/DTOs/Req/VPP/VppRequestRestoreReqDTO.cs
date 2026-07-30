namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class VppRequestRestoreReqDTO
{
    public byte[]? RowVersion { get; set; }
    public string? IdempotencyKey { get; set; }
}
