namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VPP_DashboardResDTO
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalQty { get; set; }
        public List<VPP_MonthlyStatResDTO> MonthlyStats { get; set; } = new();
        public List<VPP_CategoryStatResDTO> CategoryStats { get; set; } = new();
        public List<VPP_TopProductResDTO> TopProducts { get; set; } = new();
    }

    public class VPP_MonthlyStatResDTO
    {
        public int Y { get; set; }
        public int M { get; set; }
        public int OrderCount { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }

    public class VPP_CategoryStatResDTO
    {
        public string? CategoryName { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }

    public class VPP_TopProductResDTO
    {
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? UOMName { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }
}
