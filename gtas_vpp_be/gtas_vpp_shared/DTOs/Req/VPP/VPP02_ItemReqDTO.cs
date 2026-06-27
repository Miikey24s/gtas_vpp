namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VPP02_ItemReqDTO
    {
        public Guid? Id { get; set; }
        public Guid VPPId { get; set; }
        public int Qty { get; set; }
        public string? Description { get; set; }
    }
}
