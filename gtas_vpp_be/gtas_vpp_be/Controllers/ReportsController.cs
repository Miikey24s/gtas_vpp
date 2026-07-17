using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(
    IReportService reportService,
    IReportInsightService reportInsightService,
    IPermissionService permissionService) : ControllerBase
{
    private readonly IReportService _reportService = reportService;
    private readonly IReportInsightService _reportInsightService = reportInsightService;
    private readonly IPermissionService _permissionService = permissionService;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string scope = ReportScopes.Own,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentityScope(out var userId, out var departmentCode, out var companyCode))
        {
            return Unauthorized();
        }

        if (!await CanUseScopeAsync(scope, cancellationToken))
        {
            return Forbid();
        }

        return Ok(await _reportService.GetSummaryAsync(
            scope, userId, departmentCode, companyCode, year, month, cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Policy = Permissions.ReportExport)]
    public async Task<IActionResult> Export(
        [FromQuery] string scope = ReportScopes.Own,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentityScope(out var userId, out var departmentCode, out var companyCode))
        {
            return Unauthorized();
        }

        if (!await CanUseScopeAsync(scope, cancellationToken))
        {
            return Forbid();
        }

        var export = await _reportService.ExportCsvAsync(
            scope, userId, departmentCode, companyCode, year, month, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }

    [HttpGet("export.xlsx")]
    [Authorize(Policy = Permissions.ReportExport)]
    public async Task<IActionResult> ExportWorkbook(
        [FromQuery] string scope = ReportScopes.Own,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentityScope(out var userId, out var departmentCode, out var companyCode))
        {
            return Unauthorized();
        }

        if (!await CanUseScopeAsync(scope, cancellationToken))
        {
            return Forbid();
        }

        var export = await _reportService.ExportWorkbookAsync(
            scope, userId, departmentCode, companyCode, year, month, cancellationToken);
        return File(export.Content, export.ContentType, export.FileName);
    }

    [HttpGet("insights")]
    [EnableRateLimiting("report-insights")]
    public async Task<IActionResult> GetInsights(
        [FromQuery] string scope = ReportScopes.Own,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] string language = "vi",
        CancellationToken cancellationToken = default)
    {
        if (!TryGetIdentityScope(out var userId, out var departmentCode, out var companyCode))
        {
            return Unauthorized();
        }

        if (!await CanUseScopeAsync(scope, cancellationToken))
        {
            return Forbid();
        }

        var summary = await _reportService.GetSummaryAsync(
            scope, userId, departmentCode, companyCode, year, month, cancellationToken);
        return Ok(await _reportInsightService.GenerateAsync(summary, language, cancellationToken));
    }

    private Task<bool> CanUseScopeAsync(string scope, CancellationToken cancellationToken)
    {
        var permission = scope switch
        {
            ReportScopes.Own => Permissions.ReportViewOwn,
            ReportScopes.Department => Permissions.ReportViewDepartment,
            ReportScopes.All => Permissions.ReportViewAll,
            _ => string.Empty
        };

        return string.IsNullOrWhiteSpace(permission)
            ? Task.FromResult(false)
            : _permissionService.HasPermissionAsync(User, permission, cancellationToken);
    }

    private bool TryGetIdentityScope(
        out int userId,
        out string departmentCode,
        out string companyCode)
    {
        departmentCode = User.FindFirstValue(AppClaimTypes.DepartmentCode) ?? string.Empty;
        companyCode = User.FindFirstValue("MemberCompanyCode") ?? string.Empty;
        return int.TryParse(User.FindFirstValue("UserID"), out userId)
            && !string.IsNullOrWhiteSpace(companyCode);
    }
}
