using System.Reflection;
using System.Text.Json;
using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Res.Library;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Ghi danh mục quản trị, bảo vệ audit field và chuẩn hóa timestamp do server quản lý.
/// </summary>
public sealed class LibraryMutationService : ILibraryMutationService
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

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUserNameResolver _userNameResolver;

    public LibraryMutationService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IUserNameResolver userNameResolver)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _userNameResolver = userNameResolver;
    }

    public Task<LibraryMutationResult> CreateAsync(
        string tableCode,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default)
        => tableCode.Trim().ToLowerInvariant() switch
        {
            "lookup-categories" => CreateAsync<LookupCategory, LookupCategoryResDTO>(payload, actorUserId, cancellationToken),
            "lookup-values" => CreateAsync<LookupValue, LookupValueResDTO>(payload, actorUserId, cancellationToken),
            "vpp-categories" => CreateAsync<VppCategory, VppCategoryResDTO>(payload, actorUserId, cancellationToken),
            "suppliers" => CreateAsync<Supplier, SupplierResDTO>(payload, actorUserId, cancellationToken),
            "supplier-product-mappings" => CreateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(payload, actorUserId, cancellationToken),
            "departments" => CreateAsync<Department, DepartmentResDTO>(payload, actorUserId, cancellationToken),
            _ => Task.FromResult(new LibraryMutationResult(
                LibraryMutationStatus.BadRequest,
                Message: $"Create for Table Code '{tableCode}' is not supported."))
        };

    public Task<LibraryMutationResult> UpdateAsync(
        string tableCode,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default)
        => tableCode.Trim().ToLowerInvariant() switch
        {
            "lookup-categories" => UpdateAsync<LookupCategory, LookupCategoryResDTO>(payload, actorUserId, cancellationToken),
            "lookup-values" => UpdateAsync<LookupValue, LookupValueResDTO>(payload, actorUserId, cancellationToken),
            "vpp-categories" => UpdateAsync<VppCategory, VppCategoryResDTO>(payload, actorUserId, cancellationToken),
            "suppliers" => UpdateAsync<Supplier, SupplierResDTO>(payload, actorUserId, cancellationToken),
            "supplier-product-mappings" => UpdateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(payload, actorUserId, cancellationToken),
            "departments" => UpdateAsync<Department, DepartmentResDTO>(payload, actorUserId, cancellationToken),
            _ => Task.FromResult(new LibraryMutationResult(
                LibraryMutationStatus.BadRequest,
                Message: $"Update for Table Code '{tableCode}' is not supported."))
        };

    public Task<LibraryMutationResult> PatchAsync(
        string tableCode,
        Guid id,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken = default)
        => tableCode.Trim().ToLowerInvariant() switch
        {
            "lookup-categories" => PatchAsync<LookupCategory, LookupCategoryResDTO>(id, payload, actorUserId, cancellationToken),
            "lookup-values" => PatchAsync<LookupValue, LookupValueResDTO>(id, payload, actorUserId, cancellationToken),
            "vpp-categories" => PatchAsync<VppCategory, VppCategoryResDTO>(id, payload, actorUserId, cancellationToken),
            "suppliers" => PatchAsync<Supplier, SupplierResDTO>(id, payload, actorUserId, cancellationToken),
            "supplier-product-mappings" => PatchAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id, payload, actorUserId, cancellationToken),
            "departments" => PatchAsync<Department, DepartmentResDTO>(id, payload, actorUserId, cancellationToken),
            _ => Task.FromResult(new LibraryMutationResult(
                LibraryMutationStatus.BadRequest,
                Message: $"Patch for Table Code '{tableCode}' is not supported."))
        };

    private async Task<LibraryMutationResult> CreateAsync<TModel, TDto>(
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken)
        where TModel : BaseModel
        where TDto : class
    {
        var dto = JsonSerializer.Deserialize<TDto>(payload.GetRawText(), JsonOptions);
        if (dto is null) return new LibraryMutationResult(LibraryMutationStatus.BadRequest);

        var entity = dto.Adapt<TModel>();
        var now = _dateTimeProvider.Now;
        entity.Id = Guid.Empty;
        entity.CreatedAtUtc = now;
        entity.UpdatedAtUtc = now;
        entity.CreatedByUserId = actorUserId;
        entity.UpdatedByUserId = actorUserId;
        entity.IsDeleted = false;
        _unitOfWork.VPPContext.Set<TModel>().Add(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new LibraryMutationResult(LibraryMutationStatus.Success, entity.Adapt<TDto>());
    }

    private async Task<LibraryMutationResult> UpdateAsync<TModel, TDto>(
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken)
        where TModel : BaseModel
        where TDto : class
    {
        var dto = JsonSerializer.Deserialize<TDto>(payload.GetRawText(), JsonOptions);
        if (dto is null) return new LibraryMutationResult(LibraryMutationStatus.BadRequest);

        var entity = dto.Adapt<TModel>();
        var existing = await _unitOfWork.VPPContext.Set<TModel>()
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == entity.Id, cancellationToken);
        if (existing is null)
        {
            return new LibraryMutationResult(
                LibraryMutationStatus.NotFound,
                Message: $"Record with ID {entity.Id} not found.");
        }

        entity.CreatedByUserId = existing.CreatedByUserId;
        entity.CreatedAtUtc = existing.CreatedAtUtc;
        entity.IsDeleted = existing.IsDeleted;
        entity.UpdatedAtUtc = _dateTimeProvider.Now;
        entity.UpdatedByUserId = actorUserId;
        _unitOfWork.VPPContext.Set<TModel>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new LibraryMutationResult(LibraryMutationStatus.Success, entity.Adapt<TDto>());
    }

    private async Task<LibraryMutationResult> PatchAsync<TModel, TDto>(
        Guid id,
        JsonElement payload,
        int actorUserId,
        CancellationToken cancellationToken)
        where TModel : class
        where TDto : class
    {
        var entity = await _unitOfWork.VPPContext.Set<TModel>().FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return new LibraryMutationResult(
                LibraryMutationStatus.NotFound,
                Message: $"Record with ID {id} not found.");
        }

        await _userNameResolver.IncludeUserInfoAsync(entity, _unitOfWork.VPPContext);
        var type = typeof(TModel);
        foreach (var jsonProperty in payload.EnumerateObject())
        {
            if (WriteDeniedFields.Contains(jsonProperty.Name)) continue;
            var property = type.GetProperty(
                jsonProperty.Name,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property is null || !property.CanWrite) continue;
            property.SetValue(entity, JsonSerializer.Deserialize(jsonProperty.Value.GetRawText(), property.PropertyType));
        }

        type.GetProperty("UpdatedAtUtc")?.SetValue(entity, _dateTimeProvider.Now);
        type.GetProperty("UpdatedByUserId")?.SetValue(entity, actorUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new LibraryMutationResult(LibraryMutationStatus.Success, entity.Adapt<TDto>());
    }
}
