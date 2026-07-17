namespace gtas_vpp_shared.DTOs.Req.VPP
{
    /// <summary>
    /// Optimistic-concurrency envelope for an approval decision. The UI sends
    /// the rowversion it displayed; the server rejects a stale decision.
    /// </summary>
    public sealed class ApproveOrderReqDTO
    {
        public byte[]? RowVersion { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
