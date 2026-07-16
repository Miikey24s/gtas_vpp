namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VPP01_UpdateReqDTO
    {
        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? Description { get; set; }
        public bool IsAdditionalOrder { get; set; } = false;
        public string? SupplementReason { get; set; }
        public byte[]? RowVersion { get; set; }
        public string? IdempotencyKey { get; set; }
        public List<VPP02_ItemReqDTO> Items { get; set; } = new();
        public int UpdateUserId { get; set; }
    }
}
