using System.Globalization;
using System.Text;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.DTOs.Res.Reports;
using gtas_vpp_shared.UI;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface IReportService
{
    Task<ReportSummaryResDTO> GetSummaryAsync(
        string scope,
        int userId,
        string departmentCode,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);

    Task<ReportExportResult> ExportCsvAsync(
        string scope,
        int userId,
        string departmentCode,
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
        string departmentCode,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        Validate(scope, departmentCode, memberCompanyCode, year, month);

        var scopedHeaders = ApplyScope(
            _context.Set<VPP01_RequestHeader>().AsNoTracking().Where(order => !order.IsDeleted),
            scope,
            userId,
            departmentCode,
            memberCompanyCode);

        var availableYears = await scopedHeaders
            .Select(order => order.Y)
            .Distinct()
            .OrderByDescending(value => value)
            .ToListAsync(cancellationToken);

        var filteredHeaders = scopedHeaders
            .Where(order => !year.HasValue || order.Y == year.Value)
            .Where(order => !month.HasValue || order.M == month.Value);
        var filteredHeaderIds = filteredHeaders.Select(order => order.Id);
        var filteredDetails = _context.Set<VPP02_RequestDetail>()
            .AsNoTracking()
            .Where(detail => !detail.IsDeleted && filteredHeaderIds.Contains(detail.VPP01_RequestHeaderId));

        var totalOrders = await filteredHeaders.CountAsync(cancellationToken);
        var totalDepartments = await filteredHeaders
            .Where(order => order.DepartmentCode != null && order.DepartmentCode != string.Empty)
            .Select(order => order.DepartmentCode)
            .Distinct()
            .CountAsync(cancellationToken);
        var totalRequesters = await filteredHeaders
            .Select(order => order.CreateUserId)
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

        var periodRaw = await filteredHeaders
            .GroupBy(order => new { order.Y, order.M })
            .OrderBy(group => group.Key.Y)
            .ThenBy(group => group.Key.M)
            .Select(group => new
            {
                Year = group.Key.Y,
                Month = group.Key.M,
                OrderCount = group.Count(),
                TotalQuantity = group.Sum(order => order.VPP02_RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (int?)detail.Qty) ?? 0),
                TotalAmount = group.Sum(order => order.VPP02_RequestDetails
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
                DepartmentCode = group.Key,
                OrderCount = group.Count(),
                TotalQuantity = group.Sum(order => order.VPP02_RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (int?)detail.Qty) ?? 0),
                TotalAmount = group.Sum(order => order.VPP02_RequestDetails
                    .Where(detail => !detail.IsDeleted)
                    .Sum(detail => (long?)(detail.Qty * detail.CurrentSinglePrice)) ?? 0)
            })
            .OrderByDescending(item => item.TotalAmount)
            .ThenBy(item => item.DepartmentCode)
            .Take(12)
            .ToListAsync(cancellationToken);

        var topProducts = await filteredDetails
            .GroupBy(detail => new
            {
                Code = detail.VPP.VPPCode ?? "-",
                Name = detail.VPP.VPPName ?? "-"
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

        return new ReportSummaryResDTO
        {
            Scope = scope,
            Year = year,
            Month = month,
            GeneratedAt = DateTime.UtcNow,
            AvailableYears = availableYears,
            TotalOrders = totalOrders,
            TotalDepartments = totalDepartments,
            TotalRequesters = totalRequesters,
            TotalLines = detailStats?.Lines ?? 0,
            TotalQuantity = detailStats?.Quantity ?? 0,
            TotalAmount = detailStats?.Amount ?? 0,
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
                ResourceKey = StatusDisplay.GetResourceKey(item.Status),
                OrderCount = item.OrderCount
            }).ToList(),
            DepartmentBreakdown = departmentRaw,
            TopProducts = topProducts
        };
    }

    public async Task<ReportExportResult> ExportCsvAsync(
        string scope,
        int userId,
        string departmentCode,
        string memberCompanyCode,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        Validate(scope, departmentCode, memberCompanyCode, year, month);

        var headers = ApplyScope(
                _context.Set<VPP01_RequestHeader>().AsNoTracking().Where(order => !order.IsDeleted),
                scope,
                userId,
                departmentCode,
                memberCompanyCode)
            .Where(order => !year.HasValue || order.Y == year.Value)
            .Where(order => !month.HasValue || order.M == month.Value);
        var headerIds = headers.Select(order => order.Id);

        var rowCount = await _context.Set<VPP02_RequestDetail>()
            .AsNoTracking()
            .CountAsync(detail => !detail.IsDeleted && headerIds.Contains(detail.VPP01_RequestHeaderId), cancellationToken);
        if (rowCount > MaxExportRows)
        {
            throw new InvalidOperationException(
                $"Report contains {rowCount:N0} rows. Narrow the year or month before exporting (maximum {MaxExportRows:N0}).");
        }

        var rows = await _context.Set<VPP02_RequestDetail>()
            .AsNoTracking()
            .Where(detail => !detail.IsDeleted && headerIds.Contains(detail.VPP01_RequestHeaderId))
            .OrderByDescending(detail => detail.VPP01_RequestHeader.Y)
            .ThenByDescending(detail => detail.VPP01_RequestHeader.M)
            .ThenBy(detail => detail.VPP01_RequestHeader.DepartmentCode)
            .ThenBy(detail => detail.VPP01_RequestHeader.VPPCode)
            .ThenBy(detail => detail.VPP.VPPCode)
            .Select(detail => new
            {
                detail.VPP01_RequestHeader.Y,
                detail.VPP01_RequestHeader.M,
                detail.VPP01_RequestHeader.DepartmentCode,
                OrderCode = detail.VPP01_RequestHeader.VPPCode,
                detail.VPP01_RequestHeader.Status,
                detail.VPP01_RequestHeader.IsAdditionalOrder,
                ProductCode = detail.VPP.VPPCode,
                ProductName = detail.VPP.VPPName,
                detail.Qty,
                detail.CurrentSinglePrice
            })
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder(Math.Max(1024, rows.Count * 120));
        csv.AppendLine("sep=,");
        csv.AppendLine("Kỳ,Phòng ban,Mã đơn,Trạng thái,Đơn bổ sung,Mã vật tư,Tên vật tư,Số lượng,Đơn giá,Thành tiền");
        foreach (var row in rows)
        {
            var amount = row.Qty * row.CurrentSinglePrice;
            csv.AppendLine(string.Join(",",
            [
                EscapeCsvCell($"{row.M:00}/{row.Y}"),
                EscapeCsvCell(row.DepartmentCode),
                EscapeCsvCell(row.OrderCode),
                EscapeCsvCell(StatusDisplay.GetText(row.Status, culture: CultureInfo.GetCultureInfo("vi-VN"))),
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
        var periodPart = year.HasValue
            ? month.HasValue ? $"{year}-{month:00}" : year.Value.ToString(CultureInfo.InvariantCulture)
            : "all";

        return new ReportExportResult(
            content,
            $"GTAS-VPP-{scope}-{periodPart}.csv",
            "text/csv; charset=utf-8");
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

    private static IQueryable<VPP01_RequestHeader> ApplyScope(
        IQueryable<VPP01_RequestHeader> query,
        string scope,
        int userId,
        string departmentCode,
        string memberCompanyCode)
    {
        query = query.Where(order => order.MemberCompanyCode == memberCompanyCode);
        return scope switch
        {
            ReportScopes.Own => query.Where(order => order.CreateUserId == userId),
            ReportScopes.Department => query.Where(order => order.DepartmentCode == departmentCode),
            ReportScopes.All => query,
            _ => query.Where(_ => false)
        };
    }

    private static void Validate(
        string scope,
        string departmentCode,
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

        if (scope == ReportScopes.Department && string.IsNullOrWhiteSpace(departmentCode))
        {
            throw new ArgumentException("Department is required for department reports.", nameof(departmentCode));
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
