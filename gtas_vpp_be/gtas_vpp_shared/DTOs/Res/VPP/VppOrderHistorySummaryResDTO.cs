namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class VppOrderHistorySummaryResDTO
{
    public int PeriodCount { get; set; }
    public int TotalOrders { get; set; }
    public int TotalLines { get; set; }
    public int TotalQuantity { get; set; }
    public int? LatestPeriod { get; set; }
    public List<VppOrderHistoryPeriodResDTO> Periods { get; set; } = [];
}

public sealed class VppOrderHistoryPeriodResDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int OrderCount { get; set; }
    public int RegularQuantity { get; set; }
    public int AdditionalQuantity { get; set; }
    public int TotalQuantity => RegularQuantity + AdditionalQuantity;
    public int PeriodKey => (Year * 100) + Month;
    public string Period => $"{Month:00}/{Year}";
}
