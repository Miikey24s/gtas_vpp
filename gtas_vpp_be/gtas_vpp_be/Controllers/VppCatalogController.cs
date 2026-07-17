using System.Security.Claims;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/catalog")]
public sealed class VppCatalogController : ControllerBase
{
    private readonly IVppCatalogService _catalogService;

    public VppCatalogController(IVppCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("items")]
    [Authorize(Policy = Permissions.LibraryView)]
    public async Task<ActionResult<IReadOnlyList<VppItemResDTO>>> GetItems(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] string? filter,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        [FromQuery] string? orderby,
        [FromQuery] string? distinct,
        [FromQuery] string? distinctFilter,
        [FromQuery] bool showDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.QueryItemsAsync(
            categoryId, search, filter, skip ?? 0, top ?? 20, orderby, distinct, distinctFilter, showDeleted, cancellationToken);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        return Ok(result.Items);
    }

    [HttpPost("items")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<ActionResult<VppItemResDTO>> CreateItem(
        [FromBody] VppItemCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _catalogService.CreateItemAsync(request, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(GetItem), new { id = result.Id }, result);
    }

    [HttpGet("items/{id:guid}")]
    [Authorize(Policy = Permissions.LibraryView)]
    public async Task<ActionResult<VppItemResDTO>> GetItem(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _catalogService.GetItemAsync(id, includeDeleted: true, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPut("items/{id:guid}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<ActionResult<VppItemResDTO>> UpdateItem(
        Guid id,
        [FromBody] VppItemUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Id != Guid.Empty && request.Id != id)
        {
            return BadRequest(new { Message = "The route id and payload id must match." });
        }

        request.Id = id;
        return Ok(await _catalogService.UpdateItemAsync(request, CurrentUserId, cancellationToken));
    }

    [HttpPatch("items/{id:guid}/status")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<ActionResult<VppItemResDTO>> SetItemStatus(
        Guid id,
        [FromBody] VppItemStatusRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await _catalogService.SetItemStatusAsync(id, request, CurrentUserId, cancellationToken));

    private int CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var userId) ? userId : 0;
}
