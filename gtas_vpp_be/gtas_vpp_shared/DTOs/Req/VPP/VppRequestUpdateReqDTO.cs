namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VppRequestUpdateReqDTO
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? Description { get; set; }
        public bool IsAdditionalOrder { get; set; } = false;
        public string? SupplementReason { get; set; }
        public byte[]? RowVersion { get; set; }
        public string? IdempotencyKey { get; set; }
        public List<VppRequestDetailItemReqDTO> Items { get; set; } = new();
        public int UpdatedByUserId { get; set; }
    }
}
