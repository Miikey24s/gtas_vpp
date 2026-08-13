using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface ICurrentSettlementReportReader
{
    Task<CurrentSettlementReportSnapshot?> ReadAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default);
}

public sealed record CurrentSettlementReportSnapshot(
    Settlement Settlement,
    IReadOnlyList<SettlementAllocation> ScopedAllocations);

public sealed class CurrentSettlementReportReader(VPPContext context) : ICurrentSettlementReportReader
{
    private readonly VPPContext _context = context;

    public async Task<CurrentSettlementReportSnapshot?> ReadAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default)
    {
        if (!query.Year.HasValue || !query.Month.HasValue)
        {
            return null;
        }

        var settlement = await _context.Set<Settlement>()
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Allocations)
            .FirstOrDefaultAsync(item => !item.IsDeleted
                && item.IsCurrentRevision
                && item.MemberCompanyCode == query.MemberCompanyCode
                && item.Year == query.Year.Value
                && item.Month == query.Month.Value, cancellationToken);
        if (settlement is null)
        {
            return null;
        }

        // Scope luôn lấy từ claims đã được controller đóng gói, không nhận phòng ban/công ty tùy ý từ client.
        var scopedAllocations = settlement.Allocations
            .Where(allocation => query.Scope switch
            {
                ReportScopes.Own => allocation.RequesterUserId == query.UserId,
                ReportScopes.Department => allocation.DepartmentCode == query.DepartmentCode,
                _ => true
            })
            .ToList();

        return new CurrentSettlementReportSnapshot(settlement, scopedAllocations);
    }
}
