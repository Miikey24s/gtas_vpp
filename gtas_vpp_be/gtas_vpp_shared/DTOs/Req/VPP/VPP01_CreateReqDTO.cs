namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VPP01_CreateReqDTO
    {
        public int Y { get; set; }
        public int M { get; set; }
        public int Status { get; set; }
        public string? Description { get; set; }
        public bool IsAdditionalOrder { get; set; } = false;
        public Guid? BaseRequestId { get; set; }
        public string? SupplementReason { get; set; }
        public string? IdempotencyKey { get; set; }
        public List<VPP02_ItemReqDTO> Items { get; set; } = new();
    }
}
