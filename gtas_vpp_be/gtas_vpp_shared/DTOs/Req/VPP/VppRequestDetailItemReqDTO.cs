namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VppRequestDetailItemReqDTO
    {
        public Guid? Id { get; set; }
        public Guid VppId { get; set; }
        public int Qty { get; set; }
        public string? Description { get; set; }
    }
}
