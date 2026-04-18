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

        public VPPRequestController(IBussinessService bussinessService, IVPPRequestService vppService)
            : base(bussinessService)
        {
            _vppService = vppService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;
        private string CurrentDepartmentCode => User.FindFirstValue("DepartmentCode") ?? string.Empty;
        private string CurrentMemberCompanyCode => User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] int? year, [FromQuery] int? month, [FromQuery] int? status)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var data = await _vppService.GetMyOrdersAsync(CurrentUserId.Value, year, month, status);
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
            var data = await _bussinessService.BaseService<L04_VPP>(
                gtas_vpp_be.Service.Helpers.Config.EF_BASEMETHOD.EF_GetTAsync,
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
            var data = await _bussinessService.BaseService<L03_VPPCategory>(
                gtas_vpp_be.Service.Helpers.Config.EF_BASEMETHOD.EF_GetTAsync,
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
    }

    public class CopyPreviousMonthReqDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
