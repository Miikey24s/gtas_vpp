namespace gtas_vpp_shared.DTOs.Res.VPP
{
    public class PeriodSettlementResDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public bool IsSettled { get; set; }
        public DateTime? SettledAt { get; set; }
        public int? SettledByUserId { get; set; }
        public string? SettledByUserName { get; set; }
        public Guid? PriceListId { get; set; }
        public string? PriceListName { get; set; }
        public int OrderCount { get; set; }
        public int PendingAdditionalCount { get; set; }
        public Guid? SettlementId { get; set; }
        public int? RevisionNumber { get; set; }
        public decimal? GrandTotal { get; set; }
        public Guid? PrimarySupplierId { get; set; }
        public string? PrimarySupplierName { get; set; }
    }
}
