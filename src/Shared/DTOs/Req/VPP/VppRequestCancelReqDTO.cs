namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public sealed class VppRequestCancelReqDTO
    {
        public byte[]? RowVersion { get; set; }
        public string? Reason { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
