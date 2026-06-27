using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VPPPriceController : ControllerBase
    {
        private readonly IVPPPriceService _priceService;

        public VPPPriceController(IVPPPriceService priceService)
        {
            _priceService = priceService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

        [HttpGet("by-vpp/{vppId:guid}")]
        public async Task<IActionResult> ListByVPP(Guid vppId, [FromQuery] Guid? priceListId)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceService.ListByVPPAsync(vppId, priceListId);
            return Ok(result);
        }

        [HttpGet("by-supplier/{supplierId:guid}")]
        public async Task<IActionResult> ListBySupplier(Guid supplierId, [FromQuery] Guid? priceListId, [FromQuery] bool? showDeleted = false)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceService.ListBySupplierAsync(supplierId, priceListId, showDeleted ?? false);
            return Ok(result);
        }

        [HttpGet("item-prices")]
        public async Task<IActionResult> QueryItemPrices(
            [FromQuery] Guid supplierId,
            [FromQuery] Guid? priceListId = null,
            [FromQuery] bool? showDeleted = false,
            [FromQuery] string? search = null,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (supplierId == Guid.Empty) return BadRequest(new { Message = "supplierId is required." });

            var result = await _priceService.QueryItemPricesAsync(
                supplierId,
                priceListId,
                showDeleted ?? false,
                search,
                filter,
                skip,
                top,
                orderby,
                distinct,
                distinctFilter);

            Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
            return Ok(result.Data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] L06_PriceCreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceService.CreateAsync(req, CurrentUserId.Value);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] L06_PriceUpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            req.Id = id;
            var result = await _priceService.UpdateAsync(req, CurrentUserId.Value);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceService.DeleteAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPost("{id:guid}/set-default")]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceService.SetDefaultAsync(id, CurrentUserId.Value);
            return NoContent();
        }
    }
}
