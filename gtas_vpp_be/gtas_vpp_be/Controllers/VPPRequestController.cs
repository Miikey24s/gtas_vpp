using gtas_vpp_be.Authorization;
using gtas_vpp_be.Model.Library;
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
    public class VPPRequestController : BaseGenericController
    {
        private readonly IVPPRequestService _vppService;
        private readonly IPermissionService _permissionService;
        private readonly IAppNotificationService _notificationService;
        private readonly IVppCatalogService _catalogService;

        public VPPRequestController(
            IServiceProvider serviceProvider,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IVPPRequestService vppService,
            IPermissionService permissionService,
            IAppNotificationService notificationService,
            IVppCatalogService catalogService)
            : base(serviceProvider, userNameResolver, unitOfWork)
        {
            _vppService = vppService;
            _permissionService = permissionService;
            _notificationService = notificationService;
            _catalogService = catalogService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;
        private string CurrentDepartmentCode => User.FindFirstValue("DepartmentCode") ?? string.Empty;
        private string CurrentMemberCompanyCode => User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

        [HttpGet("my-orders")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
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

        [HttpGet("orders/{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var data = await _vppService.GetOrderByIdAsync(id);
            if (data == null) return NotFound();
            if (!await CanViewOrderAsync(data)) return Forbid();

            return Ok(data);
        }

        [HttpGet("orders/{id:guid}/history")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
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
        public async Task<IActionResult> CreateOrder([FromBody] VPP01_CreateReqDTO req)
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
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] VPP01_UpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (req.RowVersion is not { Length: > 0 })
                return BadRequest(new { Message = "RowVersion is required. Refresh the request and try again." });

            var current = await _vppService.GetOrderByIdAsync(id);
            if (current == null) return NotFound();
            if (!IsOwnedByCurrentUser(current) || !IsInCurrentCompany(current)) return Forbid();

            req.Id = id;
            req.UpdateUserId = CurrentUserId.Value;
            var result = await _vppService.UpdateOrderAsync(req);
            return Ok(result);
        }

        [HttpPost("orders/{id:guid}/cancel")]
        [Authorize(Policy = Permissions.RequestCancelOwn)]
        public async Task<IActionResult> CancelOrder(
            Guid id,
            [FromBody] VPP_CancelOrderReqDTO req)
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

        [HttpGet("orders/previous-items")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        public async Task<IActionResult> GetPreviousOrderItems()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _vppService.GetPreviousOrderItemsAsync(CurrentUserId.Value);
            if (result == null) return NotFound(new { Message = "No previous order found." });
            return Ok(result);
        }

        [HttpGet("period-info")]
        [Authorize(Policy = Permissions.RequestViewOwn)]
        public async Task<IActionResult> GetPeriodInfo()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var info = await _vppService.GetCurrentPeriodInfoAsync(CurrentUserId.Value);
            return Ok(info);
        }

        [HttpGet("products/lookup")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
        public async Task<IActionResult> GetProductsLookup(
            [FromQuery] string? search,
            [FromQuery] int? top,
            CancellationToken cancellationToken = default)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _catalogService.QueryItemsAsync(
                null, search, null, 0, Math.Clamp(top ?? 100, 1, 100), "VPPCode asc",
                null, null, showDeleted: false, cancellationToken);
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            return Ok(ToProductResults(result.Items));
        }

        [HttpGet("products")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
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

        private static IEnumerable<object> ToProductResults(IEnumerable<L04_VPPResDTO> items)
            => items.Select(x => new
            {
                x.Id,
                x.VPPCode,
                x.VPPName,
                x.Description,
                x.VPPCategoryId,
                x.VPPCategoryCode,
                x.VPPCategoryName,
                x.UOMId,
                x.UOMCode,
                x.UOMName,
                x.SupplierCount,
                x.DefaultVatRate,
                x.DefaultPrice,
                x.DefaultSupplierName
            });

        [HttpGet("categories")]
        [Authorize(Policy = Permissions.RequestCatalogView)]
        public async Task<IActionResult> GetCategories()
        {
            var data = await ReadEntitiesAsync<L03_VPPCategory>(
                true,
                x => !x.IsDeleted);

            var result = (data ?? new()).Select(x => new
            {
                x.Id,
                x.VPPCategoryCode,
                x.VPPCategoryName
            });

            return Ok(result);
        }

        [HttpGet("all-orders")]
        [Authorize(Policy = Permissions.RequestViewAll)]
        public async Task<IActionResult> GetAllOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
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

        [HttpGet("additional-orders/pending")]
        [Authorize(Policy = Permissions.RequestApprove)]
        public async Task<IActionResult> GetPendingAdditionalOrders([FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
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
        /// Returns distinct values for a specified order column. Used by CheckBoxList
        /// grid filters so they can show all possible values across all pages.
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

            // Whitelist of allowed columns to prevent SQL injection / info leakage
            var allowedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "VPPCode", "DepartmentCode", "MemberCompanyCode", "StatusText", "Description", "Period", "RequesterName", "TotalLines", "TotalQty", "SubmittedDate", "SubmittedDateText"
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

        private async Task<List<VPP01_RequestHeaderResDTO>> GetScopedOrderDataAsync(
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

        private static (List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty) ApplyOrderGridOperations(
            IEnumerable<VPP01_RequestHeaderResDTO> scopedData,
            string? filter,
            string? orderby,
            int? skip,
            int? top)
        {
            var filteredData = ApplyOrderQuery(scopedData, filter, orderby).ToList();
            var totalCount = filteredData.Count;
            var totalLines = filteredData.Sum(x => x.TotalLines);
            var totalQty = filteredData.Sum(x => x.TotalQty);

            IEnumerable<VPP01_RequestHeaderResDTO> pagedData = filteredData;
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

        private static IQueryable<VPP01_RequestHeaderResDTO> ApplyOrderQuery(IEnumerable<VPP01_RequestHeaderResDTO> scopedData, string? filter, string? orderby)
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
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ThenByDescending(x => x.UpdateDate);
        }

        private static List<Dictionary<string, object?>> BuildOrderFilterValues(
            IEnumerable<VPP01_RequestHeaderResDTO> data,
            string column,
            string? distinctFilter)
        {
            return column switch
            {
                "Period" => data
                    .Where(x => !string.IsNullOrWhiteSpace(x.Period))
                    .Where(x => string.IsNullOrWhiteSpace(distinctFilter) || x.Period.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.Period)
                    .OrderBy(g => g.First().Y)
                    .ThenBy(g => g.First().M)
                    .Select(g => new Dictionary<string, object?>
                    {
                        ["Period"] = g.Key,
                        ["Y"] = g.First().Y,
                        ["M"] = g.First().M
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

        private static List<VPP01_RequestHeaderResDTO> ApplyPopupFilterScope(
            IEnumerable<VPP01_RequestHeaderResDTO> data,
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

        private static bool MatchesPopupFilter(VPP01_RequestHeaderResDTO order, PopupFilterScope filter)
        {
            if (string.IsNullOrWhiteSpace(filter.Property) || filter.Values == null || filter.Values.Count == 0)
            {
                return true;
            }

            var comparableValue = GetPopupFilterComparableValue(order, filter.Property);
            return comparableValue != null && filter.Values.Contains(comparableValue, StringComparer.OrdinalIgnoreCase);
        }

        private static string? GetPopupFilterComparableValue(VPP01_RequestHeaderResDTO order, string property)
        {
            return property switch
            {
                nameof(VPP01_RequestHeaderResDTO.VPPCode) => order.VPPCode,
                nameof(VPP01_RequestHeaderResDTO.DepartmentCode) => order.DepartmentCode,
                nameof(VPP01_RequestHeaderResDTO.MemberCompanyCode) => order.MemberCompanyCode,
                nameof(VPP01_RequestHeaderResDTO.Description) => order.Description,
                nameof(VPP01_RequestHeaderResDTO.RequesterName) => order.RequesterName,
                nameof(VPP01_RequestHeaderResDTO.Period) => order.Period,
                nameof(VPP01_RequestHeaderResDTO.StatusText) => order.StatusText,
                nameof(VPP01_RequestHeaderResDTO.SubmittedDateText) or nameof(VPP01_RequestHeaderResDTO.SubmittedDate) => order.SubmittedDateText,
                nameof(VPP01_RequestHeaderResDTO.TotalLines) => order.TotalLines.ToString(CultureInfo.InvariantCulture),
                nameof(VPP01_RequestHeaderResDTO.TotalQty) => order.TotalQty.ToString(CultureInfo.InvariantCulture),
                _ => GetOrderColumnValue(order, property)?.ToString()
            };
        }

        private sealed class PopupFilterScope
        {
            public string Property { get; set; } = string.Empty;
            public List<string> Values { get; set; } = new();
        }

        private static object? GetOrderColumnValue(VPP01_RequestHeaderResDTO order, string column)
        {
            return column switch
            {
                "VPPCode" => order.VPPCode,
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

            // P3.3 (F-13): GroupBy executed on the database side (Monthly + StatusDistribution +
            // TotalOrders) so we never materialize the full order set per dashboard load.
            // Previously: ToListAsync() pulled every order of the user (200/year Ã— 1000 user
            // Ã— n requests/day) and grouped in memory â€” biggest dashboard hot path.
            var baseQuery = _unitOfWork.VPPContext.Set<gtas_vpp_be.Model.VPP.VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.CreateUserId == CurrentUserId.Value);

            var monthlyRaw = await baseQuery
                .Where(x => x.SubmittedDate.HasValue)
                .GroupBy(x => new { x.SubmittedDate!.Value.Year, x.SubmittedDate.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    OrderCount = g.Count(),
                    TotalQty = g.Sum(x => x.VPP02_RequestDetails
                        .Where(d => !d.IsDeleted)
                        .Sum(d => (int?)d.Qty) ?? 0),
                    TotalLines = g.Sum(x => x.VPP02_RequestDetails.Count(d => !d.IsDeleted))
                })
                .ToListAsync();

            // Format the period label in-memory (string interpolation isn't translatable).
            var monthlyData = monthlyRaw
                .Select(g => new
                {
                    Month = $"{g.Year}-{g.Month:D2}",
                    g.OrderCount,
                    g.TotalQty,
                    g.TotalLines
                })
                .ToList();

            var statusRaw = await baseQuery
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusData = statusRaw
                .Select(g => new
                {
                    // P4/F-16: Shared label table instead of a local switch copy.
                    Status = VppStatusContract.GetText(g.Status),
                    g.Count
                })
                .ToList();

            var totalOrders = await baseQuery.CountAsync();

            return Ok(new
            {
                Monthly = monthlyData,
                StatusDistribution = statusData,
                TotalOrders = totalOrders
            });
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

        private async Task TryPublishAdditionalOrderCreatedAsync(VPP01_RequestHeaderResDTO order)
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
                    $"ÄÆ¡n {order.VPPCode} cá»§a {order.RequesterName ?? "nhÃ¢n viÃªn"} Ä‘ang chá» xá»­ lÃ½.",
                    "/dashboard?tab=5&periodTab=pending",
                    order.Id.ToString("N"),
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                // The order has already committed. A temporary notification
                // outage must never make the client retry and create a duplicate.
                Serilog.Log.Warning(ex, "Could not publish pending-order notification for {OrderId}", order.Id);
            }
        }

        private async Task TryPublishOrderDecisionAsync(
            VPP01_RequestHeaderResDTO order,
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
                    [order.CreateUserId],
                    CurrentMemberCompanyCode,
                    approved ? "additional-order.approved" : "additional-order.rejected",
                    approved ? "ÄÆ¡n bá»• sung Ä‘Ã£ Ä‘Æ°á»£c duyá»‡t" : "ÄÆ¡n bá»• sung bá»‹ tá»« chá»‘i",
                    $"ÄÆ¡n {order.VPPCode} {decision}.{reasonSuffix}",
                    "/dashboard?tab=1",
                    order.Id.ToString("N"),
                    HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Could not publish order-decision notification for {OrderId}", order.Id);
            }
        }

        private async Task<bool> CanViewOrderAsync(VPP01_RequestHeaderResDTO order)
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

        private Task<bool> CanAccessScopeAsync(string? scope)
        {
            var permission = scope?.Trim().ToLowerInvariant() switch
            {
                "my-orders" => Permissions.RequestViewOwn,
                "department" => Permissions.RequestViewDepartment,
                "pending" => Permissions.RequestApprove,
                _ => Permissions.RequestViewAll
            };

            return _permissionService.HasPermissionAsync(User, permission);
        }

        private bool IsOwnedByCurrentUser(VPP01_RequestHeaderResDTO order) =>
            CurrentUserId.HasValue && order.CreateUserId == CurrentUserId.Value;

        private bool IsInCurrentCompany(VPP01_RequestHeaderResDTO order) =>
            !string.IsNullOrWhiteSpace(CurrentMemberCompanyCode)
            && string.Equals(order.MemberCompanyCode, CurrentMemberCompanyCode, StringComparison.OrdinalIgnoreCase);

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
