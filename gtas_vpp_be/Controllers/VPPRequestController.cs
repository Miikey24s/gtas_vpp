using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq.Dynamic.Core;
using System.Security.Claims;

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
            [FromQuery] int? top)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetMyOrdersSummaryPagedAsync(
                CurrentUserId.Value,
                MergeIntFilters(year, years),
                MergeIntFilters(month, months),
                MergeIntFilters(status, statuses),
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

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] string? filter,
            [FromQuery] int? skip,
            [FromQuery] int? top,
            [FromQuery] string? orderby)
        {
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var query = _unitOfWork.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
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
                    UOMName = x.UOM != null ? x.UOM.ClassDetailValue : null
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
        public async Task<IActionResult> GetAllOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top)
        {
            var (data, totalCount, totalLines, totalQty) = await _vppService.GetAllOrdersPagedAsync(year, month, status, departmentCode, skip, top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        [HttpGet("department-orders")]
        public async Task<IActionResult> GetDepartmentOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode, [FromQuery] int? skip, [FromQuery] int? top)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                departmentCode = CurrentDepartmentCode;
            }

            var (data, totalCount, totalLines, totalQty) = await _vppService.GetDepartmentOrdersPagedAsync(year, month, status, departmentCode, skip, top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
        }

        [HttpGet("additional-orders/pending")]
        public async Task<IActionResult> GetPendingAdditionalOrders([FromQuery] int? skip, [FromQuery] int? top)
        {
            var (data, totalCount, totalLines, totalQty) = await _vppService.GetPendingAdditionalOrdersPagedAsync(skip, top);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Total-Lines", totalLines.ToString());
            Response.Headers.Append("X-Total-Qty", totalQty.ToString());
            return Ok(data);
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
