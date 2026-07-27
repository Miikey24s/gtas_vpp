namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VppRequestCreateReqDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Status { get; set; }
        public string? Description { get; set; }
        public bool IsAdditionalOrder { get; set; } = false;
        public Guid? BaseRequestId { get; set; }
        public string? SupplementReason { get; set; }
        public string? IdempotencyKey { get; set; }
        public List<VppRequestDetailItemReqDTO> Items { get; set; } = new();
    }
}
