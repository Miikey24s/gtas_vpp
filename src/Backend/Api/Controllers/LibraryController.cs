using System.Security.Claims;
using System.Text.Json;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Library;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LibraryController : BaseGenericController
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly HashSet<string> WriteDeniedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "CreatedByUserId",
        "CreatedAtUtc",
        "UpdatedByUserId",
        "UpdatedAtUtc"
    };

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILibraryIntegrityService _libraryIntegrityService;
    private readonly ILibraryQueryService _libraryQueryService;

    public LibraryController(
        IServiceProvider serviceProvider,
        IUserNameResolver userNameResolver,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ILibraryIntegrityService? libraryIntegrityService = null,
        ILibraryQueryService? libraryQueryService = null)
        : base(serviceProvider, userNameResolver, unitOfWork)
    {
        _dateTimeProvider = dateTimeProvider;
        _libraryIntegrityService = libraryIntegrityService ?? new LibraryIntegrityService(unitOfWork);
        _libraryQueryService = libraryQueryService ?? new LibraryQueryService(unitOfWork, userNameResolver);
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

        var json = payload.GetRawText();
        return tableCode.ToLowerInvariant() switch
        {
            "lookup-categories" => await CreateAsync<LookupCategory, LookupCategoryResDTO>(json),
            "lookup-values" => await CreateAsync<LookupValue, LookupValueResDTO>(json),
            "vpp-categories" => await CreateAsync<VppCategory, VppCategoryResDTO>(json),
            "vpp-items" => await CreateAsync<VppItem, VppItemResDTO>(json),
            "suppliers" => await CreateAsync<Supplier, SupplierResDTO>(json),
            "supplier-product-mappings" => await CreateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(json),
            "departments" => await CreateAsync<Department, DepartmentResDTO>(json),
            _ => BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." })
        };
    }

    [HttpPut("{tableCode}")]
    [Authorize(Policy = Permissions.LibraryManage)]
    public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
    {
        if (IsMigratedVppItemTable(tableCode)) return MigratedVppItemMutationProblem();

        var json = payload.GetRawText();
        if (tableCode.Equals("departments", StringComparison.OrdinalIgnoreCase))
        {
            var dto = JsonSerializer.Deserialize<DepartmentResDTO>(json, JsonOptions);
            if (dto is null) return BadRequest(new { Message = "Department payload is invalid." });
            var validation = await ValidateDepartmentPayloadAsync(payload, dto.Id);
            if (validation is not null) return validation;
        }

        return tableCode.ToLowerInvariant() switch
        {
            "lookup-categories" => await UpdateAsync<LookupCategory, LookupCategoryResDTO>(json),
            "lookup-values" => await UpdateAsync<LookupValue, LookupValueResDTO>(json),
            "vpp-categories" => await UpdateAsync<VppCategory, VppCategoryResDTO>(json),
            "vpp-items" => await UpdateAsync<VppItem, VppItemResDTO>(json),
            "suppliers" => await UpdateAsync<Supplier, SupplierResDTO>(json),
            "supplier-product-mappings" => await UpdateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(json),
            "departments" => await UpdateAsync<Department, DepartmentResDTO>(json),
            _ => BadRequest(new { Message = $"Update for Table Code '{tableCode}' is not supported." })
        };
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

        return normalizedTableCode switch
        {
            "lookup-categories" => await ApplyPatchAsync<LookupCategory, LookupCategoryResDTO>(id, payload),
            "lookup-values" => await ApplyPatchAsync<LookupValue, LookupValueResDTO>(id, payload),
            "vpp-categories" => await ApplyPatchAsync<VppCategory, VppCategoryResDTO>(id, payload),
            "vpp-items" => await ApplyPatchAsync<VppItem, VppItemResDTO>(id, payload),
            "suppliers" => await ApplyPatchAsync<Supplier, SupplierResDTO>(id, payload),
            "supplier-product-mappings" => await ApplyPatchAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id, payload),
            "departments" => await ApplyPatchAsync<Department, DepartmentResDTO>(id, payload),
            _ => BadRequest(new { Message = $"Patch for Table Code '{tableCode}' is not supported." })
        };
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

    private async Task<IActionResult> CreateAsync<TModel, TDto>(string json)
        where TModel : gtas_vpp_be.Model.Helpers.BaseModel
        where TDto : class
    {
        var dto = JsonSerializer.Deserialize<TDto>(json, JsonOptions);
        if (dto is null) return BadRequest();

        var entity = dto.Adapt<TModel>();
        var userId = CurrentUserId;
        var now = _dateTimeProvider.Now;
        entity.Id = Guid.Empty;
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        entity.CreatedByUserId = userId;
        entity.UpdatedByUserId = userId;
        entity.IsDeleted = false;
        var created = await GetRepository<TModel>().AddAsync(entity);
        return Ok(created?.Adapt<TDto>());
    }

    private async Task<IActionResult> UpdateAsync<TModel, TDto>(string json)
        where TModel : gtas_vpp_be.Model.Helpers.BaseModel
        where TDto : class
    {
        var dto = JsonSerializer.Deserialize<TDto>(json, JsonOptions);
        if (dto is null) return BadRequest();

        var entity = dto.Adapt<TModel>();
        var existing = await GetEntityByIdAsync<TModel>(entity.Id, false);
        if (existing is null) return NotFound(new { Message = $"Record with ID {entity.Id} not found." });

        entity.CreatedByUserId = existing.CreatedByUserId;
        entity.CreatedAtUtc = existing.CreatedAtUtc;
        entity.IsDeleted = existing.IsDeleted;
        entity.UpdatedAtUtc = _dateTimeProvider.Now;
        entity.UpdatedByUserId = CurrentUserId;
        _unitOfWork.VPPContext.Entry(existing).State = EntityState.Detached;
        var updated = await GetRepository<TModel>().UpdateAsync(entity);
        return Ok(updated.Adapt<TDto>());
    }

    private async Task<IActionResult> ApplyPatchAsync<TModel, TDto>(Guid id, JsonElement payload)
        where TModel : class
        where TDto : class
    {
        var entity = await GetEntityByIdAsync<TModel>(id, true);
        if (entity is null) return NotFound(new { Message = $"Record with ID {id} not found." });

        var type = typeof(TModel);
        foreach (var jsonProperty in payload.EnumerateObject())
        {
            if (WriteDeniedFields.Contains(jsonProperty.Name)) continue;
            var property = type.GetProperty(
                jsonProperty.Name,
                System.Reflection.BindingFlags.IgnoreCase
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance);
            if (property is null || !property.CanWrite) continue;
            property.SetValue(entity, JsonSerializer.Deserialize(jsonProperty.Value.GetRawText(), property.PropertyType));
        }

        type.GetProperty("UpdatedAtUtc")?.SetValue(entity, _dateTimeProvider.Now);
        type.GetProperty("UpdatedByUserId")?.SetValue(entity, CurrentUserId);
        var updated = await GetRepository<TModel>().UpdateAsync(entity);
        return Ok(updated.Adapt<TDto>());
    }

    private int CurrentUserId => int.TryParse(User.FindFirstValue("UserID"), out var userId) ? userId : 0;

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
