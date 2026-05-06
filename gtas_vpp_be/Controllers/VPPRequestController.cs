using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            [FromQuery] List<int>? statuses)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var data = await _vppService.GetMyOrdersSummaryAsync(
                CurrentUserId.Value,
                MergeIntFilters(year, years),
                MergeIntFilters(month, months),
                MergeIntFilters(status, statuses));

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
                    UOMName = x.UOM != null ? x.UOM.ClassDetailValue : null,
                    SupplierCount = x.L06_VPPSupplierMappings != null
                        ? x.L06_VPPSupplierMappings.Count(mapping => !mapping.IsDeleted)
                        : 0
                });

            if (!string.IsNullOrWhiteSpace(filter))
            {
                try
                {
                    query = query.Where(filter);
                }
                catch
                {
                    // Ignore malformed client filters and fall back to the base query.
                }
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                try
                {
                    query = query.OrderBy(orderby);
                }
                catch
                {
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
        public async Task<IActionResult> GetAllOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode)
        {
            var data = await _vppService.GetAllOrdersAsync(year, month, status, departmentCode);
            return Ok(data);
        }

        [HttpGet("department-orders")]
        public async Task<IActionResult> GetDepartmentOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status, [FromQuery] string? departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                departmentCode = CurrentDepartmentCode;
            }

            var data = await _vppService.GetDepartmentOrdersAsync(year, month, status, departmentCode);
            return Ok(data);
        }

        [HttpGet("additional-orders/pending")]
        public async Task<IActionResult> GetPendingAdditionalOrders()
        {
            var data = await _vppService.GetPendingAdditionalOrdersAsync();
            return Ok(data);
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
