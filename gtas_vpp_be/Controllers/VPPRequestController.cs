using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpPost("orders/{id:guid}/submit")]
        public async Task<IActionResult> SubmitOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.SubmitOrderAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpPost("orders/{id:guid}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.CancelOrderAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpDelete("orders/{id:guid}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.DeleteDraftAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpPost("orders/{id:guid}/delete")]
        public async Task<IActionResult> DeleteOrderByPost(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.DeleteDraftAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpPost("orders/{id:guid}/undo-delete")]
        public async Task<IActionResult> UndoDeleteOrder(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _vppService.UndoDeleteAsync(id, CurrentUserId.Value);
            return Ok();
        }

        [HttpPost("orders/copy-previous")]
        public async Task<IActionResult> CopyPreviousMonth([FromBody] CopyPreviousMonthReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _vppService.CopyPreviousMonthAsync(
                CurrentUserId.Value, req.Year, req.Month,
                CurrentDepartmentCode, CurrentMemberCompanyCode);
            return Ok(result);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] Guid? categoryId, [FromQuery] string? search)
        {
            var data = await ReadEntitiesAsync<L04_VPP>(
                true,
                x => !x.IsDeleted
                     && (categoryId == null || x.VPPCategoryId == categoryId)
                     && (search == null || (x.VPPName != null && x.VPPName.Contains(search)) || (x.VPPCode != null && x.VPPCode.Contains(search))),
                q => q.Include(x => x.UOM).Include(x => x.VPPCategory).Include(x => x.L06_VPPSupplierMappings));

            // Return a flattened payload to avoid EF navigation cycles during JSON serialization.
            var result = (data ?? new()).Select(x => new
            {
                x.Id,
                x.VPPCode,
                x.VPPName,
                x.Description,
                x.VPPCategoryId,
                VPPCategoryCode = x.VPPCategory?.VPPCategoryCode,
                VPPCategoryName = x.VPPCategory?.VPPCategoryName,
                x.UOMId,
                UOMCode = x.UOM?.ClassDetailCode,
                UOMName = x.UOM?.ClassDetailValue,
                SupplierCount = x.L06_VPPSupplierMappings?.Count ?? 0
            });

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

            // Department summary shows Submitted (1), Closed (5), and Approved (7) orders
            // If no status filter, default to these statuses
            var allowedStatuses = new[] { 1, 5, 7 }; // Submitted, Closed, Approved
            
            // If status is provided and it's one of the allowed statuses, use it
            // Otherwise, get all allowed statuses
            int? filteredStatus = null;
            if (status.HasValue && allowedStatuses.Contains(status.Value))
            {
                filteredStatus = status;
            }

            var data = await _vppService.GetDepartmentOrdersAsync(year, month, filteredStatus, departmentCode, allowedStatuses);
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
