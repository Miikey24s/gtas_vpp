using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface IReportService
{
    Task<ReportSummaryResDTO> GetSummaryAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportCsvAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportPdfAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportWorkbookAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default);
}

public sealed class ReportService(
    VPPContext context,
    ICurrentSettlementReportReader settlementReader) : IReportService
{
    private const int MaxExportRows = 50_000;
    private readonly VPPContext _context = context;
    private readonly ICurrentSettlementReportReader _settlementReader = settlementReader;

    public async Task<ReportSummaryResDTO> GetSummaryAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default)
    {
        query.Validate();

        var scopedHeaders = ApplyScope(
            _context.Set<VppRequest>().AsNoTracking().Where(order => !order.IsDeleted),
            query);

        var availableYears = await scopedHeaders
            .Select(order => order.Year)
            .Distinct()
            .OrderByDescending(value => value)
            .ToListAsync(cancellationToken);

        var filteredHeaders = scopedHeaders
            .Where(order => !query.Year.HasValue || order.Year == query.Year.Value)
            .Where(order => !query.Month.HasValue || order.Month == query.Month.Value);
        var filteredHeaderIds = filteredHeaders.Select(order => order.Id);
        var filteredDetails = _context.Set<VppRequestDetail>()
            .AsNoTracking()
            .Where(detail => !detail.IsDeleted && filteredHeaderIds.Contains(detail.RequestId));

        var totalOrders = await filteredHeaders.CountAsync(cancellationToken);
        var totalDepartments = await filteredHeaders
            .Where(order => order.DepartmentCode != null && order.DepartmentCode != string.Empty)
            .Select(order => order.DepartmentCode)
            .Distinct()
            .CountAsync(cancellationToken);
        var totalRequesters = await filteredHeaders
            .Select(order => order.CreatedByUserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var detailStats = await filteredDetails
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Lines = group.Count(),
                Quantity = group.Sum(detail => detail.Qty),
                Amount = group.Sum(detail => detail.Qty * detail.CurrentSinglePrice)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var settlementSnapshot = await _settlementReader.ReadAsync(query, cancellationToken);
        var settlement = settlementSnapshot?.Settlement;
        var scopedSettlementAllocations = settlementSnapshot?.ScopedAllocations ?? [];

        var periodRaw = await filteredHeaders
            .GroupBy(order => new { order.Year, order.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                OrderCount = group.Count(),
                TotalQuantity = group.Sum(order => order.RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (int?)detail.Qty) ?? 0),
                TotalAmount = group.Sum(order => order.RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (long?)(detail.Qty * detail.CurrentSinglePrice)) ?? 0)
            })
            .ToListAsync(cancellationToken);

        var statusRaw = await filteredHeaders
            .GroupBy(order => order.Status)
            .Select(group => new { Status = group.Key, OrderCount = group.Count() })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var departmentRaw = await filteredHeaders
            .GroupBy(order => order.DepartmentCode ?? "-")
            .Select(group => new ReportDepartmentPointResDTO
            {
                Code = group.Key,
                OrderCount = group.Count(),
                TotalQuantity = group.Sum(order => order.RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (int?)detail.Qty) ?? 0),
                TotalAmount = group.Sum(order => order.RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (long?)(detail.Qty * detail.CurrentSinglePrice)) ?? 0)
            })
            .OrderByDescending(item => item.TotalAmount)
            .ThenBy(item => item.Code)
            .Take(12)
            .ToListAsync(cancellationToken);

        var topProducts = await filteredDetails
            .GroupBy(detail => new
            {
                Code = detail.VppItem.VppCode ?? "-",
                Name = detail.VppItem.VppName ?? "-"
            })
            .Select(group => new ReportProductPointResDTO
            {
                ProductCode = group.Key.Code,
                ProductName = group.Key.Name,
                TotalQuantity = group.Sum(detail => detail.Qty),
                TotalAmount = group.Sum(detail => detail.Qty * detail.CurrentSinglePrice)
            })
            .OrderByDescending(item => item.TotalQuantity)
            .ThenBy(item => item.ProductCode)
            .Take(10)
            .ToListAsync(cancellationToken);

        var effectiveTotalOrders = totalOrders;
        var effectiveTotalLines = detailStats?.Lines ?? 0;
        var effectiveTotalQuantity = detailStats?.Quantity ?? 0;
        var effectiveTotalAmount = detailStats?.Amount ?? 0;
        if (settlement is not null)
        {
            effectiveTotalOrders = scopedSettlementAllocations
                .Select(item => item.RequestHeaderId)
                .Distinct()
                .Count();
            effectiveTotalLines = scopedSettlementAllocations
                .Select(item => item.RequestDetailId)
                .Distinct()
                .Count();
            effectiveTotalQuantity = (int)scopedSettlementAllocations.Sum(item => item.Quantity);
            effectiveTotalAmount = (long)decimal.Round(
                scopedSettlementAllocations.Sum(item => item.GrossAmount),
                0,
                MidpointRounding.AwayFromZero);

            var allocationByDepartment = scopedSettlementAllocations
                .GroupBy(item => item.DepartmentCode ?? "-")
                .Select(group => new ReportDepartmentPointResDTO
                {
                    Code = group.Key,
                    OrderCount = group.Select(item => item.RequestHeaderId).Distinct().Count(),
                    TotalQuantity = (int)group.Sum(item => item.Quantity),
                    TotalAmount = (long)group.Sum(item => item.GrossAmount)
                })
                .OrderByDescending(item => item.TotalAmount)
                .ThenBy(item => item.Code)
                .Take(12)
                .ToList();
            departmentRaw = allocationByDepartment;

            var itemById = settlement.Items.ToDictionary(item => item.Id);
            topProducts = scopedSettlementAllocations
                .GroupBy(item => item.SettlementItemId)
                .Where(group => itemById.ContainsKey(group.Key))
                .Select(group => new ReportProductPointResDTO
                {
                    ProductCode = itemById[group.Key].VppCode,
                    ProductName = itemById[group.Key].VppName,
                    TotalQuantity = (int)group.Sum(item => item.Quantity),
                    TotalAmount = (long)group.Sum(item => item.GrossAmount)
                })
                .OrderByDescending(item => item.TotalQuantity)
                .ThenBy(item => item.ProductCode)
                .Take(10)
                .ToList();
        }

        var settlementAllocationTotal = settlement?.Allocations.Sum(item => item.GrossAmount);
        decimal? settlementVariance = settlement is null
            ? null
            : settlement.GrandTotal - settlementAllocationTotal!.Value;

        return new ReportSummaryResDTO
        {
            Scope = query.Scope,
            Year = query.Year,
            Month = query.Month,
            GeneratedAt = DateTime.UtcNow,
            AvailableYears = availableYears,
            TotalOrders = effectiveTotalOrders,
            TotalDepartments = totalDepartments,
            TotalRequesters = totalRequesters,
            TotalLines = effectiveTotalLines,
            TotalQuantity = effectiveTotalQuantity,
            TotalAmount = effectiveTotalAmount,
            IsSettlementReconciled = settlement is not null && settlementVariance == 0m,
            SettlementId = settlement?.Id,
            SettlementRevisionNumber = settlement?.RevisionNumber,
            SettlementPrimarySupplierName = settlement?.PrimarySupplierName,
            SettlementGrandTotal = settlement?.GrandTotal,
            SettlementAllocationTotal = settlementAllocationTotal,
            SettlementVariance = settlementVariance,
            PeriodTrend = periodRaw.Select(item => new ReportPeriodPointResDTO
            {
                Year = item.Year,
                Month = item.Month,
                Period = $"{item.Month:00}/{item.Year}",
                OrderCount = item.OrderCount,
                TotalQuantity = item.TotalQuantity,
                TotalAmount = item.TotalAmount
            }).ToList(),
            StatusBreakdown = statusRaw.Select(item => new ReportStatusPointResDTO
            {
                Status = item.Status,
                ResourceKey = VppStatusContract.GetResourceKey(item.Status),
                OrderCount = item.OrderCount
            }).ToList(),
            DepartmentBreakdown = departmentRaw,
            TopProducts = topProducts
        };
    }

    public async Task<ReportExportResult> ExportCsvAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default)
    {
        query.Validate();

        var headers = ApplyScope(
                _context.Set<VppRequest>().AsNoTracking().Where(order => !order.IsDeleted),
                query)
            .Where(order => !query.Year.HasValue || order.Year == query.Year.Value)
            .Where(order => !query.Month.HasValue || order.Month == query.Month.Value);
        var headerIds = headers.Select(order => order.Id);

        var rowCount = await _context.Set<VppRequestDetail>()
            .AsNoTracking()
            .CountAsync(detail => !detail.IsDeleted && headerIds.Contains(detail.RequestId), cancellationToken);
        if (rowCount > MaxExportRows)
        {
            throw new InvalidOperationException(
                $"Report contains {rowCount:N0} rows. Narrow the year or month before exporting (maximum {MaxExportRows:N0}).");
        }

        var rows = await _context.Set<VppRequestDetail>()
            .AsNoTracking()
            .Where(detail => !detail.IsDeleted && headerIds.Contains(detail.RequestId))
            .OrderByDescending(detail => detail.Request.Year)
            .ThenByDescending(detail => detail.Request.Month)
            .ThenBy(detail => detail.Request.DepartmentCode)
            .ThenBy(detail => detail.Request.VppCode)
            .ThenBy(detail => detail.VppItem.VppCode)
            .Select(detail => new ReportCsvRow(
                detail.Request.Year,
                detail.Request.Month,
                detail.Request.DepartmentCode,
                detail.Request.VppCode,
                detail.Request.Status,
                detail.Request.IsAdditionalOrder,
                detail.VppItem.VppCode,
                detail.VppItem.VppName,
                detail.Qty,
                detail.CurrentSinglePrice))
            .ToListAsync(cancellationToken);
        return new ReportExportResult(
            ReportCsvBuilder.Build(rows),
            ExportFileContract.Report(query.Scope, query.Year, query.Month, "csv"),
            ExportFileContract.CsvContentType);
    }

    public async Task<ReportExportResult> ExportPdfAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAsync(query, cancellationToken);
        return new ReportExportResult(
            ReportPdfBuilder.Build(summary),
            ExportFileContract.Report(query.Scope, query.Year, query.Month, "pdf"),
            ExportFileContract.PdfContentType);
    }

    public async Task<ReportExportResult> ExportWorkbookAsync(
        ReportQueryContext query,
        CancellationToken cancellationToken = default)
    {
        query.Validate();
        var summary = await GetSummaryAsync(query, cancellationToken);
        var items = new List<ReportWorkbookItem>();

        var settlementSnapshot = await _settlementReader.ReadAsync(query, cancellationToken);
        if (settlementSnapshot is not null)
        {
            var settlement = settlementSnapshot.Settlement;
            var itemById = settlement.Items.ToDictionary(item => item.Id);
            items = settlementSnapshot.ScopedAllocations
                .Where(allocation => itemById.ContainsKey(allocation.SettlementItemId))
                .Select(allocation =>
                {
                    var item = itemById[allocation.SettlementItemId];
                    return new ReportWorkbookItem(
                            $"{query.Month:00}/{query.Year}",
                            allocation.DepartmentCode ?? "-",
                            allocation.RequesterUserId,
                            item.VppCode,
                            item.VppName,
                            allocation.Quantity,
                            item.NetUnitPrice,
                            item.VatRate,
                            allocation.NetAmount,
                            allocation.VatAmount,
                            allocation.CommercialAdjustmentAmount + allocation.RoundingAdjustment,
                            allocation.GrossAmount,
                            item.SupplierId == settlement.PrimarySupplierId
                                ? settlement.PrimarySupplierName
                                : $"Exception supplier {item.SupplierId}",
                            item.PriceListId == settlement.PriceListId
                                ? settlement.PriceListName
                                : $"Exception price book {item.PriceListId}",
                            item.IsSupplierException);
                })
                .OrderBy(item => item.Code)
                .ThenBy(item => item.ProductCode)
                .ToList();
        }

        return new ReportExportResult(
            ReportWorkbookBuilder.Build(summary, items),
            ExportFileContract.Report(query.Scope, query.Year, query.Month, "xlsx"),
            ExportFileContract.ExcelContentType);
    }

    public static string EscapeCsvCell(string? value) => ReportCsvBuilder.EscapeCell(value);

    private static IQueryable<VppRequest> ApplyScope(
        IQueryable<VppRequest> query,
        ReportQueryContext context)
    {
        query = query.Where(order => order.MemberCompanyCode == context.MemberCompanyCode);
        return context.Scope switch
        {
            ReportScopes.Own => query.Where(order => order.CreatedByUserId == context.UserId),
            ReportScopes.Department => query.Where(order => order.DepartmentCode == context.DepartmentCode),
            ReportScopes.All => query,
            _ => query.Where(_ => false)
        };
    }

}
