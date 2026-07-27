namespace gtas_vpp_shared.DTOs.Req.VPP
{
    /// <summary>
    /// Envelope optimistic concurrency cho quyết định duyệt. UI gửi rowversion
    /// đang hiển thị; server từ chối quyết định dựa trên dữ liệu cũ.
    /// </summary>
    public sealed class ApproveOrderReqDTO
    {
        public byte[]? RowVersion { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
