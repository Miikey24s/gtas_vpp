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
    public class VPPPriceListController : ControllerBase
    {
        private readonly IPriceListService _priceListService;

        public VPPPriceListController(IPriceListService priceListService)
        {
            _priceListService = priceListService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> List()
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.ListAsync());
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceListService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] L07_PriceListCreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CreateAsync(req, CurrentUserId.Value));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] L07_PriceListUpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            req.Id = id;
            return Ok(await _priceListService.UpdateAsync(req, CurrentUserId.Value));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.DeleteAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPost("{id:guid}/set-default")]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.SetDefaultAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPost("clone")]
        public async Task<IActionResult> Clone([FromBody] L07_PriceListCloneReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CloneAsync(req, CurrentUserId.Value));
        }
    }
}
