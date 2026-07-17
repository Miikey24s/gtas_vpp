using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VPPPriceListController : ControllerBase
    {
        private readonly IPriceListService _priceListService;
        private readonly IPriceBookWorkflowService? _workflowService;

        public VPPPriceListController(IPriceListService priceListService)
            : this(priceListService, null)
        {
        }

        public VPPPriceListController(IPriceListService priceListService, IPriceBookWorkflowService? workflowService)
        {
            _priceListService = priceListService;
            _workflowService = workflowService;
        }

        private int? CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

        [HttpGet]
        [Authorize(Policy = Permissions.LibraryView)]
        public async Task<IActionResult> List(
            [FromQuery] bool? showDeleted = false,
            [FromQuery] string? filter = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? top = null,
            [FromQuery] string? orderby = null,
            [FromQuery] string? distinct = null,
            [FromQuery] string? distinctFilter = null)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var isGridRequest = !string.IsNullOrWhiteSpace(filter)
                                || skip.HasValue
                                || top.HasValue
                                || !string.IsNullOrWhiteSpace(orderby)
                                || !string.IsNullOrWhiteSpace(distinct)
                                || !string.IsNullOrWhiteSpace(distinctFilter);

            if (isGridRequest)
            {
                var result = await _priceListService.QueryAsync(showDeleted == true, filter, skip, top, orderby, distinct, distinctFilter);
                Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
                return Ok(result.Data);
            }

            return Ok(await _priceListService.ListAsync(showDeleted == true));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            var result = await _priceListService.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Create([FromBody] L07_PriceListCreateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CreateAsync(req, CurrentUserId.Value));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] L07_PriceListUpdateReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            req.Id = id;
            return Ok(await _priceListService.UpdateAsync(req, CurrentUserId.Value));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.DeleteAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPatch("{id:guid}/deleted")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> SetDeleted(Guid id, [FromBody] JsonElement payload)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            if (!payload.TryGetProperty("isDeleted", out var isDeletedJson)
                && !payload.TryGetProperty("IsDeleted", out isDeletedJson))
            {
                return BadRequest(new { Message = "IsDeleted is required." });
            }

            var result = await _priceListService.SetDeletedAsync(id, isDeletedJson.GetBoolean(), CurrentUserId.Value);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/hard")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> HardDelete(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.HardDeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id:guid}/set-default")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            await _priceListService.SetDefaultAsync(id, CurrentUserId.Value);
            return NoContent();
        }

        [HttpPost("clone")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Clone([FromBody] L07_PriceListCloneReqDTO req)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });

            return Ok(await _priceListService.CloneAsync(req, CurrentUserId.Value));
        }

        [HttpPost("{id:guid}/publish")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Publish(
            Guid id,
            [FromBody] PriceBookStatusReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.PublishAsync(id, req, CurrentUserId.Value, cancellationToken));
        }

        [HttpPost("{id:guid}/expire")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> Expire(
            Guid id,
            [FromBody] PriceBookStatusReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.ExpireAsync(id, req, CurrentUserId.Value, cancellationToken));
        }

        [HttpPost("compare")]
        [Authorize(Policy = Permissions.LibraryView)]
        public async Task<IActionResult> Compare(
            [FromBody] PriceBookComparisonReqDTO req,
            CancellationToken cancellationToken)
        {
            if (CurrentUserId is null) return Unauthorized(new { Message = "Invalid UserID claim." });
            if (_workflowService is null) return StatusCode(StatusCodes.Status503ServiceUnavailable);
            return Ok(await _workflowService.CompareAsync(req, cancellationToken));
        }
    }
}
