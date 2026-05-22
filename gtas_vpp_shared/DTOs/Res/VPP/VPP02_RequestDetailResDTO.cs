using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP02_RequestDetailResDTO : BaseResDTO
    {
        public Guid VPPId { get; set; }
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? UOMCode { get; set; }
        public string? UOMName { get; set; }
        public string? CategoryName { get; set; }
        public int Qty { get; set; }
        public long CurrentSinglePrice { get; set; }
        public long TotalPrice => (long)Qty * CurrentSinglePrice;
    }
}
