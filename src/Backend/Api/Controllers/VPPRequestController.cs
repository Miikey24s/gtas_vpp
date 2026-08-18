using gtas_vpp_be.Authorization;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Globalization;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VPPRequestController : ControllerBase
    {
        private readonly IVPPRequestService _vppService;
        private readonly IPermissionService _permissionService;
        private readonly IAppNotificationService _notificationService;
        private readonly IVppCatalogService _catalogService;
        private readonly IVppDashboardChartQueryService _dashboardChartQueryService;

        public VPPRequestController(
            IVPPRequestService vppService,
            IPermissionService permissionService,
            IAppNotificationService notificationService,
            IVppCatalogService catalogService,
            IVppDashboardChartQueryService dashboardChartQueryService)
        {
            _vppService = vppService;
            _permissionService = permissionService;
            _notificationService = notificationService;
            _catalogService = catalogService;
            _dashboardChartQueryService = dashboardChartQueryService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;
        private string CurrentDepartmentCode => User.FindFirstValue(AppClaimTypes.DepartmentCode) ?? string.Empty;
        private string CurrentMemberCompanyCode => User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

        [HttpGet("my-orders")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? status,
            [FromQuery] List<int>? years,
            [FromQuery] List<int>? months,
            [FromQuery] List<int>? statuses)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var data = await _vppService.GetMyOrdersAsync(
                CurrentUserId.Value,
                MergeIntFilters(year, years),
                MergeIntFilters(month, months),
                MergeIntFilters(status, statuses));

            return Ok(data);
        }

        [HttpGet("my-orders-summary")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        public async Task<IActionResult> GetMyOrdersSummary(
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? status,
            [FromQuery] List<int>? years,
            [FromQuery] List<int>? months,
            [FromQuery] List<int>? statuses,
            [FromQuery] int? skip,
            [FromQuery] int? top,
            [FromQuery] string? filter,
            [FromQuery] string? orderby)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var mergedYears = MergeIntFilters(year, years);
            var mergedMonths = MergeIntFilters(month, months);
            var mergedStatuses = MergeIntFilters(status, statuses);

            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetMyOrdersSummaryAsync(
                    CurrentUserId.Value,
                    mergedYears,
                    mergedMonths,
                    mergedStatuses);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetMyOrdersSummaryPagedAsync(
                CurrentUserId.Value,
                mergedYears,
                mergedMonths,
                mergedStatuses,
                skip,
                top);

            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        [HttpGet("my-order-history-summary")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        [ProducesResponseType<VppOrderHistorySummaryResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrderHistorySummary(
            [FromQuery] int? fromPeriod,
            [FromQuery] int? toPeriod)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (!HasValidPeriodRange(fromPeriod, toPeriod))
                return BadRequest(new { Message = "The selected period range is invalid." });

            var result = await _vppService.GetMyOrderHistorySummaryAsync(
                CurrentUserId.Value,
                fromPeriod,
                toPeriod);
            return Ok(result);
        }

        [HttpGet("department-order-history-summary")]
        [Authorize(Policy = Permissions.RequestViewDepartment)]
        [ProducesResponseType<VppOrderHistorySummaryResDTO>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartmentOrderHistorySummary(
            [FromQuery] int? fromPeriod,
            [FromQuery] int? toPeriod)
        {
            if (string.IsNullOrWhiteSpace(CurrentDepartmentCode))
                return Forbid();
            if (!HasValidPeriodRange(fromPeriod, toPeriod))
                return BadRequest(new { Message = "The selected period range is invalid." });

            var result = await _vppService.GetDepartmentOrderHistorySummaryAsync(
                CurrentDepartmentCode,
                CurrentMemberCompanyCode,
                fromPeriod,
                toPeriod);
            return Ok(result);
        }

        [HttpGet("my-order-history")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrderHistory(
            [FromQuery] int? fromPeriod,
            [FromQuery] int? toPeriod,
            [FromQuery] int? exactPeriod,
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] bool? isAdditionalOrder,
            [FromQuery] int? skip,
            [FromQuery] int? top)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (!HasValidPeriodRange(fromPeriod, toPeriod)
                || (exactPeriod.HasValue && !IsValidPeriod(exactPeriod.Value)))
            {
                return BadRequest(new { Message = "The selected period is invalid." });
            }

            var (data, totalCount) = await _vppService.GetMyOrderHistoryPageAsync(
                CurrentUserId.Value,
                fromPeriod,
                toPeriod,
                exactPeriod,
                search,
                status,
                isAdditionalOrder,
                skip,
                top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            return Ok(data);
        }

        [HttpGet("department-order-history")]
        [Authorize(Policy = Permissions.RequestViewDepartment)]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartmentOrderHistory(
            [FromQuery] int? fromPeriod,
            [FromQuery] int? toPeriod,
            [FromQuery] int? exactPeriod,
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] bool? isAdditionalOrder,
            [FromQuery] int? skip,
            [FromQuery] int? top)
        {
            if (string.IsNullOrWhiteSpace(CurrentDepartmentCode))
                return Forbid();
            if (!HasValidPeriodRange(fromPeriod, toPeriod)
                || (exactPeriod.HasValue && !IsValidPeriod(exactPeriod.Value)))
            {
                return BadRequest(new { Message = "The selected period is invalid." });
            }

            var (data, totalCount) = await _vppService.GetDepartmentOrderHistoryPageAsync(
                CurrentDepartmentCode,
                CurrentMemberCompanyCode,
                fromPeriod,
                toPeriod,
                exactPeriod,
                search,
                status,
                isAdditionalOrder,
                skip,
                top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            return Ok(data);
        }

        [HttpGet("orders/{id:guid}")]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var data = await _vppService.GetOrderByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewOrderAsync(data)) return Forbid();

            return Ok(data);
        }

        [HttpGet("orders/{id:guid}/export.xlsx")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportOrderWorkbook(Guid id)
        {
            var data = await _vppService.GetOrderByIdAsync(id);
            if (data == null) return NotFound();
            // Cùng quy tắc phạm vi với xem chi tiết đơn: chủ đơn hoặc quyền xem
            // theo phòng ban/toàn công ty. File xuất không chứa giá.
            if (!await CanViewOrderAsync(data)) return Forbid();

            var content = OrderWorkbookBuilder.Build(data);
            return File(
                content,
                ExportFileContract.ExcelContentType,
                ExportFileContract.Order(data.VppCode, data.Id, "xlsx"));
        }

        [HttpGet("orders/{id:guid}/export.pdf")]
        [Produces("application/pdf")]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExportOrderPdf(Guid id)
        {
            var data = await _vppService.GetOrderByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewOrderAsync(data)) return Forbid();

            var content = OrderPdfBuilder.Build(data);
            return File(
                content,
                ExportFileContract.PdfContentType,
                ExportFileContract.Order(data.VppCode, data.Id, "pdf"));
        }

        [HttpGet("orders/{id:guid}/history")]
        [ProducesResponseType<VppRequestHistoryResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderHistory(Guid id)
        {
            var data = await _vppService.GetOrderHistoryAsync(id);
            if (data is null) return NotFound();
            var current = data.Revisions.FirstOrDefault(x => x.Id == id)
                ?? data.Revisions.FirstOrDefault();
            if (current is null || !await CanViewOrderAsync(current)) return Forbid();
            return Ok(data);
        }

        [HttpPost("orders")]
        [Authorize(Policy = Permissions.RequestCreate)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateOrder([FromBody] VppRequestCreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (string.IsNullOrWhiteSpace(CurrentDepartmentCode)
                || string.IsNullOrWhiteSpace(CurrentMemberCompanyCode))
            {
                return Forbid();
            }

            var result = await _vppService.CreateOrderAsync(req, CurrentUserId.Value, CurrentDepartmentCode, CurrentMemberCompanyCode);
            if (result.IsAdditionalOrder)
            {
                await TryPublishAdditionalOrderCreatedAsync(result);
            }
            return Ok(result);
        }

        [HttpPut("orders/{id:guid}")]
        [Authorize(Policy = Permissions.RequestUpdateOwn)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] VppRequestUpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsOwnedByCurrentUser(current) || !IsInCurrentCompany(current)) return Forbid();

            req.Id = id;
            req.UpdatedByUserId = CurrentUserId.Value;
            var result = await _vppService.UpdateOrderAsync(req);
            return Ok(result);
        }

        [HttpPost("orders/{id:guid}/manager-adjustment")]
        [Authorize(Policy = Permissions.PeriodSettle)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AdjustOrderAfterClose(
            Guid id,
            [FromBody] VppManagerOrderAdjustmentReqDTO request,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null || string.IsNullOrWhiteSpace(CurrentMemberCompanyCode))
                return Forbid();

            var result = await _vppService.AdjustOrderAfterCloseAsync(
                id,
                CurrentUserId.Value,
                CurrentMemberCompanyCode,
                request);
            try
            {
                var actionText = string.Equals(request.Action, "Cancel", StringComparison.OrdinalIgnoreCase)
                    ? "đã được hủy"
                    : "đã được cập nhật";
                await _notificationService.PublishAsync(
                    [result.CreatedByUserId],
                    CurrentMemberCompanyCode,
                    "period.order-adjustment.completed",
                    "Đơn đặt hàng đã được điều chỉnh",
                    $"Đơn {result.VppCode} {actionText} trước khi chốt kỳ. {request.EmployeeNote.Trim()}",
                    $"/dashboard?tab=1&orderId={result.Id}",
                    result.Id.ToString("N"),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Could not publish manager adjustment notification for order {OrderId}", result.Id);
            }

            return Ok(result);
        }

        [HttpPost("orders/{id:guid}/cancel")]
        [Authorize(Policy = Permissions.RequestCancelOwn)]
        public async Task<IActionResult> CancelOrder(
            Guid id,
            [FromBody] VppRequestCancelReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsOwnedByCurrentUser(current) || !IsInCurrentCompany(current)) return Forbid();

            await _vppService.CancelOrderAsync(id, CurrentUserId.Value, req);
            return Ok();
        }

        [HttpPost("orders/{id:guid}/restore")]
        [Authorize(Policy = Permissions.RequestUpdateOwn)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RestoreCancelledOrder(
            Guid id,
            [FromBody] VppRequestRestoreReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsOwnedByCurrentUser(current) || !IsInCurrentCompany(current)) return Forbid();

            var result = await _vppService.RestoreCancelledOrderAsync(id, CurrentUserId.Value, req);
            return Ok(result);
        }

        [HttpPost("orders/{id:guid}/recreate")]
        [Authorize(Policy = Permissions.RequestUpdateOwn)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RecreateCancelledOrder(
            Guid id,
            [FromBody] VppRequestRecreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsOwnedByCurrentUser(current) || !IsInCurrentCompany(current)) return Forbid();

            var result = await _vppService.RecreateCancelledOrderAsync(id, CurrentUserId.Value, req);
            return Ok(result);
        }

        [HttpGet("orders/previous-items")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        [ProducesResponseType<VppRequestResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPreviousOrderItems([FromQuery] Guid? periodId)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _vppService.GetPreviousOrderItemsAsync(
                CurrentUserId.Value,
                periodId);
            if (result == null) return NotFound(new { Message = "No previous order found." });
            return Ok(result);
        }

        [HttpGet("period-info")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        [ProducesResponseType<VppPeriodInfoResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPeriodInfo([FromQuery] Guid? periodId)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var info = await _vppService.GetCurrentPeriodInfoAsync(
                CurrentUserId.Value,
                periodId);
            return Ok(info);
        }

        [HttpGet("products/lookup")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
        [ProducesResponseType<List<VppItemResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProductsLookup(
            [FromQuery] string? search,
            [FromQuery] int? top,
            CancellationToken cancellationToken = default)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _catalogService.QueryItemsAsync(
                null, search, null, 0, Math.Clamp(top ?? 100, 1, 100), "VppCode asc",
                null, null, showDeleted: false, cancellationToken);
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            return Ok(ToProductResults(result.Items));
        }

        [HttpGet("products")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
        [ProducesResponseType<List<VppItemResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProducts(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] string? filter,
            [FromQuery] int? skip,
            [FromQuery] int? top,
            [FromQuery] string? orderby,
            [FromQuery] string? distinct,
            [FromQuery] string? distinctFilter)
        {
            var result = await _catalogService.QueryItemsAsync(
                categoryId, search, filter, skip ?? 0, top ?? 20, orderby,
                distinct, distinctFilter, showDeleted: false, HttpContext.RequestAborted);
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            return Ok(ToProductResults(result.Items));
        }

        private static List<VppItemResDTO> ToProductResults(IEnumerable<VppItemResDTO> items)
            => items.Select(x => new VppItemResDTO
            {
                Id = x.Id,
                VppCode = x.VppCode,
                VppName = x.VppName,
                Description = x.Description,
                VppCategoryId = x.VppCategoryId,
                VppCategoryCode = x.VppCategoryCode,
                VppCategoryName = x.VppCategoryName,
                UomId = x.UomId,
                UomCode = x.UomCode,
                UomName = x.UomName,
                SupplierCount = x.SupplierCount,
                DefaultVatRate = x.DefaultVatRate,
                DefaultPrice = x.DefaultPrice,
                DefaultSupplierName = x.DefaultSupplierName
            }).ToList();

        [HttpGet("categories")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
        [ProducesResponseType<List<VppCategoryResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _catalogService.GetCategoriesAsync(HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpGet("all-orders")]
        [Authorize(Policy = Permissions.RequestViewAll)]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby, [FromQuery] bool summaryOnly = false)
        {
            if (summaryOnly)
            {
                var (summaries, summaryCount, summaryLines, summaryQty, summaryAmount) = await _vppService
                    .GetAllOrderSummariesPagedAsync(
                        year,
                        month,
                        status,
                        departmentCode,
                        skip,
                        top,
                        CurrentMemberCompanyCode);
                Response.Headers.Append("X-Total-Count", summaryCount.ToString());
                Response.Headers.Append("X-Total-Lines", summaryLines.ToString());
                Response.Headers.Append("X-Total-Qty", summaryQty.ToString());
                Response.Headers.Append("X-Total-Amount", summaryAmount.ToString());
                return Ok(summaries);
            }

            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetAllOrdersAsync(year, month, status, departmentCode, CurrentMemberCompanyCode);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                var filteredTotalAmount = ApplyOrderQuery(scopedData, filter, orderby).Sum(x => x.TotalAmount);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                Response.Headers.Append("X-Total-Amount", filteredTotalAmount.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty, totalAmount) = await _vppService.GetAllOrdersPagedAsync(year, month, status, departmentCode, skip, top, CurrentMemberCompanyCode);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            Response.Headers.Append("X-Total-Amount", totalAmount.ToString());
            return Ok(data);
        }

        [HttpGet("department-orders")]
        [Authorize(Policy = Permissions.RequestViewDepartment)]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDepartmentOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
            departmentCode = CurrentDepartmentCode;

            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetDepartmentOrdersAsync(year, month, status, departmentCode, CurrentMemberCompanyCode);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetDepartmentOrdersPagedAsync(year, month, status, departmentCode, skip, top, CurrentMemberCompanyCode);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        // Gom nhu cầu kỳ (bước 2 vận hành kỳ, §3.3.3.4 — quyết định D7 trong ATLAS-001):
        // chỉ đọc, cùng policy với chốt kỳ, tổng hợp từ phiên bản đơn hợp lệ hiện hành.
        [HttpGet("period-demand")]
        [Authorize(Policy = Permissions.PeriodSettle)]
        [ProducesResponseType<AggregatedVppResDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPeriodDemand([FromQuery] int year, [FromQuery] int month)
        {
            if (!IsValidPeriod(year * 100 + month))
            {
                return BadRequest("Invalid period.");
            }

            var result = await _vppService.GetPeriodDemandAsync(year, month, CurrentMemberCompanyCode);
            return Ok(result);
        }

        [HttpGet("additional-orders/pending")]
        [Authorize]
        [ProducesResponseType<List<VppRequestResDTO>>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetPendingAdditionalOrders([FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
            if (!await HasSupplementDecisionPermissionAsync()) return Forbid();

            var canViewAllDepartments = await _permissionService
                .HasPermissionAsync(User, Permissions.RequestViewAll);
            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetPendingAdditionalOrdersAsync(
                    CurrentMemberCompanyCode,
                    CurrentDepartmentCode,
                    canViewAllDepartments);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService
                .GetPendingAdditionalOrdersPagedAsync(
                    skip,
                    top,
                    CurrentMemberCompanyCode,
                    CurrentDepartmentCode,
                    canViewAllDepartments);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        /// <summary>
        /// Trả về các giá trị distinct của cột đơn hàng được chỉ định. CheckBoxList filter
        /// của grid dùng dữ liệu này để hiển thị mọi giá trị có thể có trên tất cả page.
        /// </summary>
        [HttpGet("order-filter-values")]
        public async Task<IActionResult> GetOrderFilterValues(
            [FromQuery] string column,
            [FromQuery] string? scope,
            [FromQuery] int? year,
            [FromQuery] int? month,
            [FromQuery] int? status,
            [FromQuery] List<int>? years,
            [FromQuery] List<int>? months,
            [FromQuery] List<int>? statuses,
            [FromQuery] string? departmentCode,
            [FromQuery] string? filter,
            [FromQuery] string? filters,
            [FromQuery] string? distinctFilter)
        {
            if (!await CanAccessScopeAsync(scope)) return Forbid();

            if (string.IsNullOrWhiteSpace(column))
                return BadRequest(new { Message = "Column parameter is required." });

            // Allowlist cột được phép để ngăn SQL injection và rò rỉ thông tin.
            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VppCode", "DepartmentCode", "MemberCompanyCode", "StatusText", "Description", "Period", "RequesterName", "TotalLines", "TotalQty", "SubmittedDate", "SubmittedDateText"
            };

            if (!allowedColumns.Contains(column))
                return BadRequest(new { Message = $"Column '{column}' is not available for filtering." });

            try
            {
                if (string.Equals(scope, "department", StringComparison.OrdinalIgnoreCase))
                {
                    departmentCode = CurrentDepartmentCode;
                }

                var scopedData = await GetScopedOrderDataAsync(scope, year, month, status, departmentCode, years, months, statuses);
                var filteredData = ApplyPopupFilterScope(scopedData, filters);

                if (!string.IsNullOrWhiteSpace(filter) && !filteredData.Any())
                {
                    filteredData = ApplyOrderQuery(scopedData, filter, orderby: null).ToList();
                }

                var results = BuildOrderFilterValues(filteredData, column, distinctFilter);

                return Ok(results);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Order distinct query failed for column {Column}", column);
                return Ok(new List<object>());
            }
        }

        private async Task<List<VppRequestResDTO>> GetScopedOrderDataAsync(
            string? scope,
            int? year,
            int? month,
            int? status,
            string? departmentCode,
            List<int>? years = null,
            List<int>? months = null,
            List<int>? statuses = null)
        {
            return scope?.Trim().ToLowerInvariant() switch
            {
                "department" => await _vppService.GetDepartmentOrdersAsync(year, month, status, CurrentDepartmentCode, CurrentMemberCompanyCode),
                "pending" => await _vppService.GetPendingAdditionalOrdersAsync(
                    CurrentMemberCompanyCode,
                    CurrentDepartmentCode,
                    await _permissionService.HasPermissionAsync(User, Permissions.RequestViewAll)),
                "my-orders" => CurrentUserId.HasValue
                    ? await _vppService.GetMyOrdersAsync(
                        CurrentUserId.Value,
                        MergeIntFilters(year, years),
                        MergeIntFilters(month, months),
                        MergeIntFilters(status, statuses))
                    : new(),
                _ => await _vppService.GetAllOrdersAsync(year, month, status, departmentCode, CurrentMemberCompanyCode)
            };
        }

        private static (List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty) ApplyOrderGridOperations(
            IEnumerable<VppRequestResDTO> scopedData,
            string? filter,
            string? orderby,
            int? skip,
            int? top)
        {
            var filteredData = ApplyOrderQuery(scopedData, filter, orderby).ToList();
            var totalCount = filteredData.Count;
            var totalLines = filteredData.Sum(x => x.TotalLines);
            var totalQty = filteredData.Sum(x => x.TotalQty);

            IEnumerable<VppRequestResDTO> pagedData = filteredData;
            if (skip.HasValue && skip.Value > 0)
            {
                pagedData = pagedData.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                pagedData = pagedData.Take(top.Value);
            }

            return (pagedData.ToList(), totalCount, totalLines, totalQty);
        }

        private static IQueryable<VppRequestResDTO> ApplyOrderQuery(IEnumerable<VppRequestResDTO> scopedData, string? filter, string? orderby)
        {
            var query = scopedData.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Order filter parse failed, using scoped data");
                }
            }

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                try
                {
                    return query.OrderBy(orderby);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Order orderby parse failed, using default order");
                }
            }

            return query
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.UpdatedAtUtc);
        }

        private static List<Dictionary<string, object?>> BuildOrderFilterValues(
            IEnumerable<VppRequestResDTO> data,
            string column,
            string? distinctFilter)
        {
            return column switch
            {
                "Period" => data
                    .Where(x => !string.IsNullOrWhiteSpace(x.Period))
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.Period.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.Period)
                    .OrderBy(g => g.First().Year)
                    .ThenBy(g => g.First().Month)
                    .Select(g => new Dictionary<string, object?>
                    {
                        ["Period"] = g.Key,
                        ["Year"] = g.First().Year,
                        ["Month"] = g.First().Month
                    })
                    .ToList(),
                "StatusText" => data
                    .Where(x => !string.IsNullOrWhiteSpace(x.StatusText))
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.StatusText.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.StatusText)
                    .OrderBy(g => g.First().Status)
                    .Select(g => new Dictionary<string, object?>
                    {
                        ["StatusText"] = g.Key,
                        ["Status"] = g.First().Status,
                        ["IsAdditionalOrder"] = g.First().IsAdditionalOrder,
                        ["IsDeadlinePassed"] = g.First().IsDeadlinePassed
                    })
                    .ToList(),
                "TotalLines" => data
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.TotalLines.ToString().Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.TotalLines)
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new Dictionary<string, object?> { ["TotalLines"] = x })
                    .ToList(),
                "TotalQty" => data
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.TotalQty.ToString().Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.TotalQty)
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new Dictionary<string, object?> { ["TotalQty"] = x })
                    .ToList(),
                "SubmittedDate" or "SubmittedDateText" => data
                    .Where(x => x.SubmittedDate.HasValue)
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.SubmittedDateText.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.SubmittedDateText)
                    .OrderByDescending(g => g.First().SubmittedDate)
                    .Select(g => new Dictionary<string, object?>
                    {
                        ["SubmittedDateText"] = g.Key,
                        ["SubmittedDate"] = g.First().SubmittedDate
                    })
                    .ToList(),
                "RequesterName" => data
                    .Where(x => !string.IsNullOrWhiteSpace(x.RequesterName))
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.RequesterName!.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.RequesterName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .Select(x => new Dictionary<string, object?> { ["RequesterName"] = x })
                    .ToList(),
                _ => data
                    .Select(x => new
                    {
                        Value = GetOrderColumnValue(x, column)
                    })
                    .Where(x => x.Value != null)
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.Value!.ToString()!.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new Dictionary<string, object?> { [column] = x })
                    .ToList()
            };
        }

        private static List<VppRequestResDTO> ApplyPopupFilterScope(
            IEnumerable<VppRequestResDTO> data,
            string? filtersJson)
        {
            if (string.IsNullOrWhiteSpace(filtersJson))
            {
                return data.ToList();
            }

            try
            {
                var filters = JsonSerializer.Deserialize<List<PopupFilterScope>>(filtersJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (filters == null || filters.Count == 0)
                {
                    return data.ToList();
                }

                return data.Where(order => filters.All(filter => MatchesPopupFilter(order, filter))).ToList();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Order popup filter scope parse failed, using unfiltered scoped data");
                return data.ToList();
            }
        }

        private static bool MatchesPopupFilter(VppRequestResDTO order, PopupFilterScope filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Property) || filter.Values == null || filter.Values.Count == 0)
            {
                return true;
            }

            var comparableValue = GetPopupFilterComparableValue(order, filter.Property);
            return comparableValue != null && filter.Values.Contains(comparableValue, StringComparer.OrdinalIgnoreCase);
        }

        private static string? GetPopupFilterComparableValue(VppRequestResDTO order, string property)
        {
            return property switch
            {
                nameof(VppRequestResDTO.VppCode) => order.VppCode,
                nameof(VppRequestResDTO.DepartmentCode) => order.DepartmentCode,
                nameof(VppRequestResDTO.MemberCompanyCode) => order.MemberCompanyCode,
                nameof(VppRequestResDTO.Description) => order.Description,
                nameof(VppRequestResDTO.RequesterName) => order.RequesterName,
                nameof(VppRequestResDTO.Period) => order.Period,
                nameof(VppRequestResDTO.StatusText) => order.StatusText,
                nameof(VppRequestResDTO.SubmittedDateText) or nameof(VppRequestResDTO.SubmittedDate) => order.SubmittedDateText,
                nameof(VppRequestResDTO.TotalLines) => order.TotalLines.ToString(CultureInfo.InvariantCulture),
                nameof(VppRequestResDTO.TotalQty) => order.TotalQty.ToString(CultureInfo.InvariantCulture),
                _ => GetOrderColumnValue(order, property)?.ToString()
            };
        }

        private sealed class PopupFilterScope
        {
            public string Property { get; set; } = string.Empty;
            public List<string> Values { get; set; } = new();
        }

        private static object? GetOrderColumnValue(VppRequestResDTO order, string column)
        {
            return column switch
            {
                "VppCode" => order.VppCode,
                "DepartmentCode" => order.DepartmentCode,
                "MemberCompanyCode" => order.MemberCompanyCode,
                "Description" => order.Description,
                "TotalLines" => order.TotalLines,
                "TotalQty" => order.TotalQty,
                "SubmittedDate" => order.SubmittedDate,
                "SubmittedDateText" => order.SubmittedDateText,
                _ => null
            };
        }

        [HttpGet("dashboard-charts")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        public async Task<IActionResult> GetDashboardCharts()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _dashboardChartQueryService.GetAsync(
                CurrentUserId.Value,
                HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpPost("additional-orders/{id:guid}/approve")]
        [Authorize(Policy = Permissions.RequestApprove)]
        public async Task<IActionResult> ApproveAdditionalOrder(
            Guid id,
            [FromBody] ApproveOrderReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsInCurrentCompany(current)) return Forbid();

            var canApproveCrossDepartment = await _permissionService
                .HasPermissionAsync(User, Permissions.RequestViewAll);
            await _vppService.ApproveAdditionalOrderAsync(
                id, CurrentUserId.Value, req.RowVersion, req.IdempotencyKey,
                CurrentDepartmentCode, canApproveCrossDepartment, CurrentMemberCompanyCode);
            await TryPublishOrderDecisionAsync(current, approved: true, reason: null);
            return Ok();
        }

        [HttpPost("additional-orders/{id:guid}/reject")]
        [Authorize(Policy = Permissions.RequestReject)]
        public async Task<IActionResult> RejectAdditionalOrder(Guid id, [FromBody] RejectOrderReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsInCurrentCompany(current)) return Forbid();

            var canApproveCrossDepartment = await _permissionService
                .HasPermissionAsync(User, Permissions.RequestViewAll);
            await _vppService.RejectAdditionalOrderAsync(
                id, CurrentUserId.Value, req.Reason, req.RowVersion, req.IdempotencyKey,
                CurrentDepartmentCode, canApproveCrossDepartment, CurrentMemberCompanyCode);
            await TryPublishOrderDecisionAsync(current, approved: false, req.Reason);
            return Ok();
        }

        private async Task TryPublishAdditionalOrderCreatedAsync(VppRequestResDTO order)
        {
            try
            {
                var recipients = await _notificationService.GetRecipientsWithPermissionAsync(
                    CurrentMemberCompanyCode,
                    Permissions.RequestApprove,
                    HttpContext.RequestAborted);

                await _notificationService.PublishAsync(
                    recipients.Where(userId => userId != CurrentUserId),
                    CurrentMemberCompanyCode,
                    "additional-order.pending",
                    "ÄÆ¡n bá»• sung chá» duyá»‡t",
                    $"ÄÆ¡n {order.VppCode} cá»§a {order.RequesterName ?? "nhÃ¢n viÃªn"} Ä‘ang chá» xá»­ lÃ½.",
                    "/dashboard?tab=5&periodTab=pending",
                    order.Id.ToString("N"),
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                // Đơn hàng đã commit. Lỗi notification tạm thời không được khiến client
                // retry và tạo dữ liệu trùng.
                Serilog.Log.Warning(ex, "Could not publish pending-order notification for {OrderId}", order.Id);
            }
        }

        private async Task TryPublishOrderDecisionAsync(
            VppRequestResDTO order,
            bool approved,
            string? reason)
        {
            try
            {
                var decision = approved ? "Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t" : "Ä‘Ã£ bá»‹ tá»« chá»‘i";
                var reasonSuffix = !approved && !string.IsNullOrWhiteSpace(reason)
                    ? $" LÃ½ do: {reason.Trim()}"
                    : string.Empty;

                await _notificationService.PublishAsync(
                    [order.CreatedByUserId],
                    CurrentMemberCompanyCode,
                    approved ? "additional-order.approved" : "additional-order.rejected",
                    approved ? "ÄÆ¡n bá»• sung Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t" : "ÄÆ¡n bá»• sung bá»‹ tá»« chá»‘i",
                    $"ÄÆ¡n {order.VppCode} {decision}.{reasonSuffix}",
                    "/dashboard?tab=1",
                    order.Id.ToString("N"),
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Could not publish order-decision notification for {OrderId}", order.Id);
            }
        }

        private async Task<bool> CanViewOrderAsync(VppRequestResDTO order)
        {
            if (!IsInCurrentCompany(order)) return false;

            if (IsOwnedByCurrentUser(order)
                && await _permissionService.HasPermissionAsync(User, Permissions.RequestViewOwn))
            {
                return true;
            }

            if (string.Equals(order.DepartmentCode, CurrentDepartmentCode, StringComparison.OrdinalIgnoreCase)
                && await _permissionService.HasPermissionAsync(User, Permissions.RequestViewDepartment))
            {
                return true;
            }

            return await _permissionService.HasPermissionAsync(User, Permissions.RequestViewAll);
        }

        private async Task<bool> HasSupplementDecisionPermissionAsync() =>
            await _permissionService.HasPermissionAsync(User, Permissions.RequestApprove)
            || await _permissionService.HasPermissionAsync(User, Permissions.RequestReject);

        private Task<bool> CanAccessScopeAsync(string? scope) =>
            scope?.Trim().ToLowerInvariant() switch
            {
                "my-orders" => _permissionService.HasPermissionAsync(User, Permissions.RequestViewOwn),
                "department" => _permissionService.HasPermissionAsync(User, Permissions.RequestViewDepartment),
                "pending" => HasSupplementDecisionPermissionAsync(),
                _ => _permissionService.HasPermissionAsync(User, Permissions.RequestViewAll)
            };

        private bool IsOwnedByCurrentUser(VppRequestResDTO order) =>
            CurrentUserId.HasValue && order.CreatedByUserId == CurrentUserId.Value;

        private bool IsInCurrentCompany(VppRequestResDTO order) =>
            !string.IsNullOrWhiteSpace(CurrentMemberCompanyCode)
            && string.Equals(order.MemberCompanyCode, CurrentMemberCompanyCode, StringComparison.OrdinalIgnoreCase);

        private static bool HasValidPeriodRange(int? fromPeriod, int? toPeriod) =>
            (!fromPeriod.HasValue || IsValidPeriod(fromPeriod.Value))
            && (!toPeriod.HasValue || IsValidPeriod(toPeriod.Value))
            && (!fromPeriod.HasValue || !toPeriod.HasValue || fromPeriod.Value <= toPeriod.Value);

        private static bool IsValidPeriod(int period)
        {
            var year = period / 100;
            var month = period % 100;
            return year is >= 2000 and <= 9999 && month is >= 1 and <= 12;
        }

        private static IEnumerable<int>? MergeIntFilters(int? singleValue, IEnumerable<int>? listValues)
        {
            var values = new HashSet<int>();

            if (singleValue.HasValue)
            {
                values.Add(singleValue.Value);
            }

            if (listValues != null)
            {
                foreach (var value in listValues)
                {
                    values.Add(value);
                }
            }

            return values.Count == 0 ? null : values;
        }
    }
}
