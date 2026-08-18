namespace gtas_vpp_be.Service.Services;

public interface IVppDashboardChartQueryService
{
    Task<VppDashboardChartResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public sealed record VppDashboardChartResult(
    IReadOnlyList<VppDashboardMonthlyPoint> Monthly,
    IReadOnlyList<VppDashboardStatusPoint> StatusDistribution,
    int TotalOrders);

public sealed record VppDashboardMonthlyPoint(
    string Month,
    int OrderCount,
    int TotalQty,
    int TotalLines);

public sealed record VppDashboardStatusPoint(
    string Status,
    int Count);
