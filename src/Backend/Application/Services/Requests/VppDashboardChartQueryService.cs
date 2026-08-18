using gtas_vpp_be.Model.VPP;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Tổng hợp dữ liệu biểu đồ ngay tại database để dashboard không phải tải toàn bộ đơn về memory.
/// </summary>
public sealed class VppDashboardChartQueryService : IVppDashboardChartQueryService
{
    private readonly IUnitOfWork _unitOfWork;

    public VppDashboardChartQueryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VppDashboardChartResult> GetAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _unitOfWork.VPPContext.Set<VppRequest>()
            .AsNoTracking()
            .Where(request => !request.IsDeleted && request.CreatedByUserId == userId);

        var monthlyRaw = await baseQuery
            .Where(request => request.SubmittedDate.HasValue)
            .GroupBy(request => new
            {
                request.SubmittedDate!.Value.Year,
                request.SubmittedDate.Value.Month
            })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                OrderCount = group.Count(),
                TotalQty = group.Sum(request => request.RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (int?)detail.Qty) ?? 0),
                TotalLines = group.Sum(request => request.RequestDetails.Count(detail => !detail.IsDeleted))
            })
            .ToListAsync(cancellationToken);

        // Nhãn kỳ được định dạng sau truy vấn vì phép định dạng này không dịch ổn định sang SQL.
        var monthly = monthlyRaw
            .Select(group => new VppDashboardMonthlyPoint(
                $"{group.Year}-{group.Month:D2}",
                group.OrderCount,
                group.TotalQty,
                group.TotalLines))
            .ToList();

        var statusRaw = await baseQuery
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var statuses = statusRaw
            .Select(group => new VppDashboardStatusPoint(
                VppStatusContract.GetText(group.Status),
                group.Count))
            .ToList();

        var totalOrders = await baseQuery.CountAsync(cancellationToken);
        return new VppDashboardChartResult(monthly, statuses, totalOrders);
    }
}
