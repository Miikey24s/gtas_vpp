namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP_AggregatedItemResDTO
    {
        public Guid VPPId { get; set; }
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? UOMName { get; set; }
        public string? CategoryName { get; set; }
        public int TotalQty { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice => (long)TotalQty * UnitPrice;
        public List<VPP_AggregatedItemBreakdownResDTO> Breakdown { get; set; } = new();
    }

    public class VPP_AggregatedItemBreakdownResDTO
    {
        public int UserId { get; set; }
        public string? RequesterName { get; set; }
        public string? DepartmentCode { get; set; }
        public int Qty { get; set; }
        public string? Note { get; set; }
    }

    public class VPP_AggregatedResDTO
    {
        public int Y { get; set; }
        public int M { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
        public List<VPP_AggregatedItemResDTO> Items { get; set; } = new();
    }
}
