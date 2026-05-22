using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public VPPRequestController(IServiceProvider serviceProvider, IUserNameResolver userNameResolver, IUnitOfWork unitOfWork, IVPPRequestService vppService)
            : base(serviceProvider, userNameResolver, unitOfWork)
        {
            _vppService = vppService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;
        private string CurrentDepartmentCode => User.FindFirstValue("DepartmentCode") ?? string.Empty;
        private string CurrentMemberCompanyCode => User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

        [HttpGet("my-orders")]
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

            return Ok(data);
        }

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] VPP01_CreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _vppService.CreateOrderAsync(req, CurrentUserId.Value, CurrentDepartmentCode, CurrentMemberCompanyCode);
            return Ok(result);
        }

        [HttpPut("orders/{id:guid}")]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] VPP01_UpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            req.Id = id;
            req.UpdateUserId = CurrentUserId.Value;
            var result = await _vppService.UpdateOrderAsync(req);
            return Ok(result);
        }

        [HttpPost("orders/{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.CancelOrderAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpGet("orders/previous-items")]
        public async Task<IActionResult> GetPreviousOrderItems()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _vppService.GetPreviousOrderItemsAsync(CurrentUserId.Value);
            if (result == null) return NotFound(new { Message = "No previous order found." });
            return Ok(result);
        }

        [HttpGet("period-info")]
        public async Task<IActionResult> GetPeriodInfo()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var info = await _vppService.GetCurrentPeriodInfoAsync(CurrentUserId.Value);
            return Ok(info);
        }

        [HttpGet("products/lookup")]
        public async Task<IActionResult> GetProductsLookup()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var query = await _unitOfWork.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (x.VPPCategory == null || !x.VPPCategory.IsDeleted))
                .OrderBy(x => x.VPPCode)
                .Select(x => new
                {
                    x.Id,
                    x.VPPCode,
                    x.VPPName,
                    UOMCode = x.UOM != null ? x.UOM.ClassDetailCode : null,
                    UOMName = x.UOM != null ? x.UOM.ClassDetailValue : null,
                    VPPCategoryName = x.VPPCategory != null ? x.VPPCategory.VPPCategoryName : null
                })
                .ToListAsync();

            return Ok(query);
        }

        [HttpGet("products")]
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
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var query = _unitOfWork.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (x.VPPCategory == null || !x.VPPCategory.IsDeleted)
                     && (categoryId == null || x.VPPCategoryId == categoryId)
                     && (search == null || (x.VPPName != null && x.VPPName.Contains(search)) || (x.VPPCode != null && x.VPPCode.Contains(search))))
                .Select(x => new
                {
                    x.Id,
                    x.VPPCode,
                    x.VPPName,
                    x.Description,
                    x.VPPCategoryId,
                    VPPCategoryCode = x.VPPCategory != null ? x.VPPCategory.VPPCategoryCode : null,
                    VPPCategoryName = x.VPPCategory != null ? x.VPPCategory.VPPCategoryName : null,
                    x.UOMId,
                    UOMCode = x.UOM != null ? x.UOM.ClassDetailCode : null,
                    UOMName = x.UOM != null ? x.UOM.ClassDetailValue : null,
                    SupplierCount = x.L06_VPPSupplierMappings!.Count(m => !m.IsDeleted
                        && m.L07_PriceList != null
                        && m.L07_PriceList.IsDefault
                        && !m.L07_PriceList.IsDeleted
                        && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted)),
                    DefaultVatRate = VppPricingDefaults.VatRate,
                    DefaultPrice = x.L06_VPPSupplierMappings!
                        .Where(m => !m.IsDeleted
                            && m.L07_PriceList != null
                            && m.L07_PriceList.IsDefault
                            && !m.L07_PriceList.IsDeleted
                            && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                        .OrderByDescending(m => m.IsDefault)
                        .ThenBy(m => m.L05_VPPSupplier != null && m.L05_VPPSupplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                        .ThenBy(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
                        .Select(m => (decimal?)m.Price)
                        .FirstOrDefault(),
                    DefaultSupplierName = x.L06_VPPSupplierMappings!
                        .Where(m => !m.IsDeleted
                            && m.L07_PriceList != null
                            && m.L07_PriceList.IsDefault
                            && !m.L07_PriceList.IsDeleted
                            && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                        .OrderByDescending(m => m.IsDefault)
                        .ThenBy(m => m.L05_VPPSupplier != null && m.L05_VPPSupplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                        .ThenBy(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
                        .Select(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
                        .FirstOrDefault()
                });

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "VPP filter parse failed, using base query");
                }
            }

            // ── Distinct branch: return only unique values for the requested column ──
            if (!string.IsNullOrWhiteSpace(distinct))
            {
                try
                {
                    var distinctQuery = query;
                    if (!string.IsNullOrWhiteSpace(distinctFilter))
                    {
                        distinctQuery = distinctQuery.Where($"{distinct} != null && {distinct}.Contains(@0)", distinctFilter);
                    }

                    var distinctValues = await distinctQuery
                        .Select(distinct)
                        .Distinct()
                        .ToDynamicListAsync();

                    var results = distinctValues
                        .Where(val => val != null)
                        .Select(val =>
                        {
                            var dict = new Dictionary<string, object?>();
                            dict[distinct] = val;
                            return dict;
                        })
                        .ToList();

                    Response.Headers.Append("X-Total-Count", results.Count.ToString());
                    return Ok(results);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "VPP distinct query failed for column {Column}", distinct);
                    return Ok(new List<object>());
                }
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                try
                {
                    query = query.OrderBy(orderby);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Warning(ex, "VPP orderby parse failed, falling back to VPPCode");
                    query = query.OrderBy(x => x.VPPCode);
                }
            }
            else
            {
                query = query.OrderBy(x => x.VPPCode);
            }

            if (skip.HasValue && skip.Value > 0)
            {
                query = query.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                query = query.Take(top.Value);
            }

            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            var result = await query.ToListAsync();

            return Ok(result);
        }

        [HttpGet("categories")]
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
        public async Task<IActionResult> GetAllOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetAllOrdersAsync(year, month, status, departmentCode);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetAllOrdersPagedAsync(year, month, status, departmentCode, skip, top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        [HttpGet("department-orders")]
        public async Task<IActionResult> GetDepartmentOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                departmentCode = CurrentDepartmentCode;
            }

            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetDepartmentOrdersAsync(year, month, status, departmentCode);
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetDepartmentOrdersPagedAsync(year, month, status, departmentCode, skip, top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        [HttpGet("additional-orders/pending")]
        public async Task<IActionResult> GetPendingAdditionalOrders([FromQuery] int? skip, [FromQuery] int? top, [FromQuery] string? filter, [FromQuery] string? orderby)
        {
            if (!string.IsNullOrWhiteSpace(filter) || !string.IsNullOrWhiteSpace(orderby))
            {
                var scopedData = await _vppService.GetPendingAdditionalOrdersAsync();
                var (filteredData, filteredTotalCount, filteredTotalLines, filteredTotalQty) = ApplyOrderGridOperations(scopedData, filter, orderby, skip, top);
                Response.Headers.Append("X-Total-Count", filteredTotalCount.ToString());
                Response.Headers.Append("X-Total-Lines", filteredTotalLines.ToString());
                Response.Headers.Append("X-Total-Qty", filteredTotalQty.ToString());
                return Ok(filteredData);
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetPendingAdditionalOrdersPagedAsync(skip, top);
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
                if (string.Equals(scope, "department", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(departmentCode))
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
                "department" => await _vppService.GetDepartmentOrdersAsync(year, month, status, departmentCode),
                "pending" => await _vppService.GetPendingAdditionalOrdersAsync(),
                "my-orders" => CurrentUserId.HasValue 
                    ? await _vppService.GetMyOrdersAsync(
                        CurrentUserId.Value,
                        MergeIntFilters(year, years),
                        MergeIntFilters(month, months),
                        MergeIntFilters(status, statuses)) 
                    : new(),
                _ => await _vppService.GetAllOrdersAsync(year, month, status, departmentCode)
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
        public async Task<IActionResult> GetDashboardCharts()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            // P3.3 (F-13): GroupBy executed on the database side (Monthly + StatusDistribution +
            // TotalOrders) so we never materialize the full order set per dashboard load.
            // Previously: ToListAsync() pulled every order of the user (200/year × 1000 user
            // × n requests/day) and grouped in memory — biggest dashboard hot path.
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
                    Status = StatusDisplay.GetText(g.Status),
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
        public async Task<IActionResult> ApproveAdditionalOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.ApproveAdditionalOrderAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpPost("additional-orders/{id:guid}/reject")]
        public async Task<IActionResult> RejectAdditionalOrder(Guid id, [FromBody] RejectOrderReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.RejectAdditionalOrderAsync(id, CurrentUserId.Value, req.Reason);
            return Ok();
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
