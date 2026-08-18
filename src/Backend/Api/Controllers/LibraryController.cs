using System.Security.Claims;
using System.Text.Json;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LibraryController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ILibraryIntegrityService _libraryIntegrityService;
    private readonly ILibraryQueryService _libraryQueryService;
    private readonly ILibraryMutationService _libraryMutationService;

    public LibraryController(
        IUserNameResolver userNameResolver,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILibraryIntegrityService? libraryIntegrityService = null,
        ILibraryQueryService? libraryQueryService = null,
        ILibraryMutationService? libraryMutationService = null)
    {
        _libraryIntegrityService = libraryIntegrityService ?? new LibraryIntegrityService(unitOfWork);
        _libraryQueryService = libraryQueryService ?? new LibraryQueryService(unitOfWork, userNameResolver);
        _libraryMutationService = libraryMutationService
            ?? new LibraryMutationService(unitOfWork, dateTimeProvider, userNameResolver);
    }

    [HttpGet("{tableCode}")]
    [Authorize(Policy = Permissions.LibraryView)]
    public async Task<IActionResult> GenericGet(
        string tableCode,
        [FromQuery] Guid? id,
        [FromQuery] string? searchText,
        [FromQuery] Guid? lookupCategoryId,
        [FromQuery] string? filter,
        [FromQuery] int? skip,
        [FromQuery] int? top,
        [FromQuery] string? orderby,
        [FromQuery] string? distinct,
        [FromQuery] string? distinctFilter,
        [FromQuery] bool? showDeleted = false)
    {
        var result = await _libraryQueryService.QueryAsync(
            tableCode,
            new LibraryQueryRequest(
                id,
                searchText,
                lookupCategoryId,
                filter,
                skip,
                top,
                orderby,
                distinct,
                distinctFilter,
                showDeleted ?? false),
            HttpContext.RequestAborted);

        if (result.ErrorMessage is not null)
        {
            return BadRequest(new { Message = result.ErrorMessage });
        }

        if (result.TotalCount.HasValue)
        {
            Response.Headers["X-Total-Count"] = result.TotalCount.Value.ToString();
        }

        return Ok(result.Value);
    }

    [HttpGet("{tableCode}/{id:guid}")]
    [Authorize(Policy = Permissions.LibraryView)]
    public async Task<IActionResult> GenericGetById(
        string tableCode,
        Guid id,
        [FromQuery] bool? showDeleted = false)
    {
        var result = await _libraryQueryService.GetByIdAsync(
            tableCode,
            id,
            showDeleted ?? false,
            HttpContext.RequestAborted);
        if (result.ErrorMessage is not null)
        {
            return BadRequest(new { Message = result.ErrorMessage });
        }

        return result.NotFound
            ? NotFound(new { Message = $"Record with ID {id} not found." })
            : Ok(result.Value);
    }

    [HttpGet("{tableCode}/{id:guid}/dependency-impact")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GetDependencyImpact(string tableCode, Guid id)
    {
        var impact = await _libraryIntegrityService.GetDependencyImpactAsync(tableCode, id);
        return impact is null
            ? BadRequest(new { Message = $"Dependency impact for Table Code '{tableCode}' is not supported." })
            : Ok(impact);
    }

    [HttpPost("{tableCode}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
    {
        if (IsMigratedVppItemTable(tableCode)) return MigratedVppItemMutationProblem();

        if (tableCode.Equals("departments", StringComparison.OrdinalIgnoreCase))
        {
            var validation = await ValidateDepartmentPayloadAsync(payload, Guid.Empty);
            if (validation is not null) return validation;
        }

        var result = await _libraryMutationService.CreateAsync(
            tableCode,
            payload,
            CurrentUserId,
            HttpContext.RequestAborted);
        return MapMutationResult(result);
    }

    [HttpPut("{tableCode}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
    {
        if (IsMigratedVppItemTable(tableCode)) return MigratedVppItemMutationProblem();

        if (tableCode.Equals("departments", StringComparison.OrdinalIgnoreCase))
        {
            var dto = JsonSerializer.Deserialize<DepartmentResDTO>(payload.GetRawText(), JsonOptions);
            if (dto is null) return BadRequest(new { Message = "Department payload is invalid." });
            var validation = await ValidateDepartmentPayloadAsync(payload, dto.Id);
            if (validation is not null) return validation;
        }

        var result = await _libraryMutationService.UpdateAsync(
            tableCode,
            payload,
            CurrentUserId,
            HttpContext.RequestAborted);
        return MapMutationResult(result);
    }

    [HttpPatch("{tableCode}/{id:guid}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GenericPatch(string tableCode, Guid id, [FromBody] JsonElement payload)
    {
        if (IsMigratedVppItemTable(tableCode)) return MigratedVppItemMutationProblem();
        if (payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return BadRequest(new { Message = "Update payload must not be empty." });
        }

        var normalizedTableCode = tableCode.Trim().ToLowerInvariant();
        if (IsDeletionRequested(payload))
        {
            var impact = await _libraryIntegrityService.GetDependencyImpactAsync(normalizedTableCode, id);
            if (impact is not null && !impact.CanDeactivate)
            {
                return Conflict(new
                {
                    Message = $"Cannot deactivate this record while {impact.ActiveReferenceCount} active reference(s) still exist.",
                    impact.DependencyKind,
                    impact.ActiveReferenceCount
                });
            }
        }

        if (normalizedTableCode == "departments"
            && TryGetPropertyIgnoreCase(payload, nameof(Department.ParentDepartmentId), out var parentElement))
        {
            Guid? parentId;
            try { parentId = JsonSerializer.Deserialize<Guid?>(parentElement.GetRawText(), JsonOptions); }
            catch (JsonException) { return BadRequest(new { Message = "Parent department id is invalid." }); }
            var validation = await ValidateDepartmentParentAsync(id, parentId);
            if (validation is not null) return validation;
        }

        var result = await _libraryMutationService.PatchAsync(
            normalizedTableCode,
            id,
            payload,
            CurrentUserId,
            HttpContext.RequestAborted);
        return MapMutationResult(result);
    }

    [HttpDelete("{tableCode}/{id:guid}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
    {
        if (IsMigratedVppItemTable(tableCode)) return MigratedVppItemMutationProblem();

        return tableCode.ToLowerInvariant() switch
        {
            "lookup-categories" or "lookup-values" or "vpp-categories" or "suppliers"
                or "supplier-product-mappings" or "departments" => await HardDeleteLibraryRecordAsync(tableCode, id),
            _ => BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." })
        };
    }

    private async Task<IActionResult?> ValidateDepartmentPayloadAsync(JsonElement payload, Guid departmentId)
    {
        var dto = JsonSerializer.Deserialize<DepartmentResDTO>(payload.GetRawText(), JsonOptions);
        return dto is null
            ? BadRequest(new { Message = "Department payload is invalid." })
            : await ValidateDepartmentParentAsync(departmentId, dto.ParentDepartmentId);
    }

    private async Task<IActionResult?> ValidateDepartmentParentAsync(Guid departmentId, Guid? parentDepartmentId)
    {
        var validation = await _libraryIntegrityService.ValidateDepartmentParentAsync(departmentId, parentDepartmentId);
        return validation.IsValid ? null : BadRequest(new { Message = validation.Message });
    }

    private async Task<IActionResult> HardDeleteLibraryRecordAsync(string tableCode, Guid id)
    {
        var result = await _libraryIntegrityService.HardDeleteAsync(tableCode, id);
        return result?.Status switch
        {
            LibraryHardDeleteStatus.Deleted => Ok(new { Id = id }),
            LibraryHardDeleteStatus.NotFound => NotFound(),
            LibraryHardDeleteStatus.MustDeactivate => Problem(
                title: "Hard delete blocked",
                detail: "The record must be deactivated before permanent deletion.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "HardDeleteRequiresDeactivation" }),
            LibraryHardDeleteStatus.HasDependencies => Problem(
                title: "Hard delete blocked",
                detail: "The record is still referenced and cannot be permanently deleted.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = "HardDeleteBlockedByDependencies",
                    ["referenceCount"] = result.ReferenceCount
                }),
            _ => BadRequest(new { Message = $"Hard delete for Table Code '{tableCode}' is not supported." })
        };
    }

    private int CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var userId) ? userId : 0;

    private IActionResult MapMutationResult(LibraryMutationResult result)
        => result.Status switch
        {
            LibraryMutationStatus.Success => Ok(result.Value),
            LibraryMutationStatus.NotFound => NotFound(new { result.Message }),
            _ when result.Message is null => BadRequest(),
            _ => BadRequest(new { result.Message })
        };

    private static bool IsDeletionRequested(JsonElement payload)
        => TryGetPropertyIgnoreCase(payload, nameof(gtas_vpp_be.Model.Helpers.BaseModel.IsDeleted), out var value)
           && value.ValueKind == JsonValueKind.True;

    private static bool TryGetPropertyIgnoreCase(JsonElement payload, string propertyName, out JsonElement value)
    {
        foreach (var property in payload.EnumerateObject())
        {
            if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)) continue;
            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }

    private static bool IsMigratedVppItemTable(string tableCode)
        => string.Equals(tableCode, "vpp-items", StringComparison.OrdinalIgnoreCase);

    private IActionResult MigratedVppItemMutationProblem()
        => Problem(
            title: "Legacy catalog mutation disabled",
            detail: "VPP items must be changed through /api/catalog/items; hard delete is not supported.",
            statusCode: StatusCodes.Status405MethodNotAllowed,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = "CatalogTypedEndpointRequired",
                ["safeDetail"] = true
            });
}
