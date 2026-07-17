namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class AggregatedVppItemResDTO
    {
        public Guid VppId { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? UomName { get; set; }
        public string? CategoryName { get; set; }
        public int TotalQty { get; set; }
        public long UnitPrice { get; set; }
        public long TotalPrice => (long)TotalQty * UnitPrice;
        public List<AggregatedVppItemBreakdownResDTO> Breakdown { get; set; } = new();
    }

    public class AggregatedVppItemBreakdownResDTO
    {
        public int UserId { get; set; }
        public string? RequesterName { get; set; }
        public string? Code { get; set; }
        public int Qty { get; set; }
        public string? Note { get; set; }
    }

    public class AggregatedVppResDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalOrders { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
        public List<AggregatedVppItemResDTO> Items { get; set; } = new();
    }
}
