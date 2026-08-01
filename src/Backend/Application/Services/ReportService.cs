using System.Globalization;
using System.Text;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface IReportService
{
    Task<ReportSummaryResDTO> GetSummaryAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportCsvAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportPdfAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportWorkbookAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);
}

public sealed class ReportService(VPPContext context) : IReportService
{
    private const int MaxExportRows = 50_000;
    private readonly VPPContext _context = context;

    public async Task<ReportSummaryResDTO> GetSummaryAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        Validate(scope, Code, memberCompanyCode, year, month);

        var scopedHeaders = ApplyScope(
            _context.Set<VppRequest>().AsNoTracking().Where(order => !order.IsDeleted),
            scope,
            userId,
            Code,
            memberCompanyCode);

        var availableYears = await scopedHeaders
            .Select(order => order.Year)
            .Distinct()
            .OrderByDescending(value => value)
            .ToListAsync(cancellationToken);

        var filteredHeaders = scopedHeaders
            .Where(order => !year.HasValue || order.Year == year.Value)
            .Where(order => !month.HasValue || order.Month == month.Value);
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

        Settlement? settlement = null;
        var scopedSettlementAllocations = new List<SettlementAllocation>();
        if (year.HasValue && month.HasValue)
        {
            settlement = await _context.Set<Settlement>()
                .AsNoTracking()
                .Include(item => item.Items)
                .Include(item => item.Allocations)
                .FirstOrDefaultAsync(item => !item.IsDeleted
                    && item.IsCurrentRevision
                    && item.MemberCompanyCode == memberCompanyCode
                    && item.Year == year.Value
                    && item.Month == month.Value, cancellationToken);
            if (settlement is not null)
            {
                scopedSettlementAllocations = settlement.Allocations
                    .Where(allocation => scope switch
                    {
                        ReportScopes.Own => allocation.RequesterUserId == userId,
                        ReportScopes.Department => allocation.DepartmentCode == Code,
                        _ => true
                    })
                    .ToList();
            }
        }

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
            Scope = scope,
            Year = year,
            Month = month,
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
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        Validate(scope, Code, memberCompanyCode, year, month);

        var headers = ApplyScope(
                _context.Set<VppRequest>().AsNoTracking().Where(order => !order.IsDeleted),
                scope,
                userId,
                Code,
                memberCompanyCode)
            .Where(order => !year.HasValue || order.Year == year.Value)
            .Where(order => !month.HasValue || order.Month == month.Value);
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
            .Select(detail => new
            {
                detail.Request.Year,
                detail.Request.Month,
                detail.Request.DepartmentCode,
                OrderCode = detail.Request.VppCode,
                detail.Request.Status,
                detail.Request.IsAdditionalOrder,
                ProductCode = detail.VppItem.VppCode,
                ProductName = detail.VppItem.VppName,
                detail.Qty,
                detail.CurrentSinglePrice
            })
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder(Math.Max(1024, rows.Count * 120));
        csv.AppendLine("sep=,");
        csv.AppendLine("Kỳ,Phòng ban,Mã đơn,Trạng thái,Đơn bổ sung,Mã mặt hàng,Tên mặt hàng,Số lượng,Đơn giá,Thành tiền");
        foreach (var row in rows)
        {
            var amount = row.Qty * row.CurrentSinglePrice;
            csv.AppendLine(string.Join(",",
            [
                EscapeCsvCell($"{row.Month:00}/{row.Year}"),
                EscapeCsvCell(row.DepartmentCode),
                EscapeCsvCell(row.OrderCode),
                EscapeCsvCell(VppStatusContract.GetText(row.Status, culture: CultureInfo.GetCultureInfo("vi-VN"))),
                EscapeCsvCell(row.IsAdditionalOrder ? "Có" : "Không"),
                EscapeCsvCell(row.ProductCode),
                EscapeCsvCell(row.ProductName),
                row.Qty.ToString(CultureInfo.InvariantCulture),
                row.CurrentSinglePrice.ToString(CultureInfo.InvariantCulture),
                amount.ToString(CultureInfo.InvariantCulture)
            ]));
        }

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var content = encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
        return new ReportExportResult(
            content,
            ExportFileContract.Report(scope, year, month, "csv"),
            ExportFileContract.CsvContentType);
    }

    public async Task<ReportExportResult> ExportPdfAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetSummaryAsync(
            scope, userId, Code, memberCompanyCode, year, month, cancellationToken);
        return new ReportExportResult(
            ReportPdfBuilder.Build(summary),
            ExportFileContract.Report(scope, year, month, "pdf"),
            ExportFileContract.PdfContentType);
    }

    public async Task<ReportExportResult> ExportWorkbookAsync(
        string scope,
        int userId,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        Validate(scope, Code, memberCompanyCode, year, month);
        var summary = await GetSummaryAsync(
            scope, userId, Code, memberCompanyCode, year, month, cancellationToken);
        var items = new List<ReportWorkbookItem>();

        if (year.HasValue && month.HasValue)
        {
            var settlement = await _context.Set<Settlement>()
                .AsNoTracking()
                .Include(item => item.Items)
                .Include(item => item.Allocations)
                .FirstOrDefaultAsync(item => !item.IsDeleted
                    && item.IsCurrentRevision
                    && item.MemberCompanyCode == memberCompanyCode
                    && item.Year == year.Value
                    && item.Month == month.Value, cancellationToken);
            if (settlement is not null)
            {
                var itemById = settlement.Items.ToDictionary(item => item.Id);
                var scoped = settlement.Allocations.Where(allocation => scope switch
                {
                    ReportScopes.Own => allocation.RequesterUserId == userId,
                    ReportScopes.Department => allocation.DepartmentCode == Code,
                    _ => true
                });
                items = scoped
                    .Where(allocation => itemById.ContainsKey(allocation.SettlementItemId))
                    .Select(allocation =>
                    {
                        var item = itemById[allocation.SettlementItemId];
                        return new ReportWorkbookItem(
                            $"{month:00}/{year}",
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
        }

        return new ReportExportResult(
            ReportWorkbookBuilder.Build(summary, items),
            ExportFileContract.Report(scope, year, month, "xlsx"),
            ExportFileContract.ExcelContentType);
    }

    public static string EscapeCsvCell(string? value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@')
        {
            safe = $"'{safe}";
        }

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }

    private static IQueryable<VppRequest> ApplyScope(
        IQueryable<VppRequest> query,
        string scope,
        int userId,
        string Code,
        string memberCompanyCode)
    {
        query = query.Where(order => order.MemberCompanyCode == memberCompanyCode);
        return scope switch
        {
            ReportScopes.Own => query.Where(order => order.CreatedByUserId == userId),
            ReportScopes.Department => query.Where(order => order.DepartmentCode == Code),
            ReportScopes.All => query,
            _ => query.Where(_ => false)
        };
    }

    private static void Validate(
        string scope,
        string Code,
        string memberCompanyCode,
        int? year,
        int? month)
    {
        if (!ReportScopes.IsValid(scope))
        {
            throw new ArgumentException("Report scope is invalid.", nameof(scope));
        }

        if (string.IsNullOrWhiteSpace(memberCompanyCode))
        {
            throw new ArgumentException("Member company is required.", nameof(memberCompanyCode));
        }

        if (scope == ReportScopes.Department && string.IsNullOrWhiteSpace(Code))
        {
            throw new ArgumentException("Department is required for department reports.", nameof(Code));
        }

        if (year is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }
    }
}
