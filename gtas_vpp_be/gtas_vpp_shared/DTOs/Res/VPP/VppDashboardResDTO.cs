namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class VppDashboardResDTO
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalQty { get; set; }
        public List<VppMonthlyStatResDTO> MonthlyStats { get; set; } = new();
        public List<VppCategoryStatResDTO> CategoryStats { get; set; } = new();
        public List<VppTopProductResDTO> TopProducts { get; set; } = new();
    }

    public class VppMonthlyStatResDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OrderCount { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }

    public class VppCategoryStatResDTO
    {
        public string? CategoryName { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }

    public class VppTopProductResDTO
    {
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? UomName { get; set; }
        public int TotalQty { get; set; }
        public long TotalAmount { get; set; }
    }
}
