using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VppRequestDetailResDTO : BaseResDTO
    {
        public Guid VppId { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public string? CategoryName { get; set; }
        public int Qty { get; set; }
        public long CurrentSinglePrice { get; set; }
        public long TotalPrice => (long)Qty * CurrentSinglePrice;
    }
}
