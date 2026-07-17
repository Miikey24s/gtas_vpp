namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class RejectOrderReqDTO
    {
        public string? Reason { get; set; }
        public byte[]? RowVersion { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
