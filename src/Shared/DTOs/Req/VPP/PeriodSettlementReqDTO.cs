namespace gtas_vpp_shared.DTOs.Req.VPP
{
    public class PeriodSettlementReqDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public Guid? PriceListId { get; set; }
    }
}
