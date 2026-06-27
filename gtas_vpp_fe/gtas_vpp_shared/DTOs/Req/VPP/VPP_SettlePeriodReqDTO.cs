namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class VPP_SettlePeriodReqDTO
    {
        public int Y { get; set; }
        public int M { get; set; }
        public Guid? PriceListId { get; set; }
    }
}
