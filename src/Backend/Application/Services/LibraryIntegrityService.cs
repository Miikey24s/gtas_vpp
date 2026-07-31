using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface ILibraryIntegrityService
{
    Task<LibraryDependencyImpactResDTO?> GetDependencyImpactAsync(string tableCode, Guid id, CancellationToken cancellationToken = default);
    Task<LibraryHardDeleteResult?> HardDeleteAsync(string tableCode, Guid id, CancellationToken cancellationToken = default);
    Task<LibraryHardDeleteResult?> HardDeleteLookupAsync(string tableCode, Guid id, CancellationToken cancellationToken = default);
    Task<LibraryIntegrityValidationResult> ValidateDepartmentParentAsync(Guid departmentId, Guid? parentDepartmentId, CancellationToken cancellationToken = default);
}

public sealed class LibraryIntegrityService : ILibraryIntegrityService
{
    private readonly IUnitOfWork _unitOfWork;

    public LibraryIntegrityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LibraryDependencyImpactResDTO?> GetDependencyImpactAsync(
        string tableCode,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var normalizedTableCode = tableCode.Trim().ToLowerInvariant();
        var context = _unitOfWork.VPPContext;
        var (dependencyKind, count) = normalizedTableCode switch
        {
            "lookup-categories" => ("lookup-values", await context.LookupValues.CountAsync(x => x.LookupCategoryId == id && !x.IsDeleted, cancellationToken)),
            "lookup-values" => ("vpp-items", await context.VppItems.CountAsync(x => x.UomId == id && !x.IsDeleted, cancellationToken)),
            "vpp-categories" => ("vpp-items", await context.VppItems.CountAsync(x => x.VppCategoryId == id && !x.IsDeleted, cancellationToken)),
            "suppliers" => ("supplier-product-mappings,price-lists", await GetSupplierReferenceCountAsync(id, cancellationToken)),
            "departments" => ("child-departments,user-group-memberships", await GetDepartmentReferenceCountAsync(id, cancellationToken)),
            _ => (string.Empty, -1)
        };

        if (count < 0) return null;

        return new LibraryDependencyImpactResDTO
        {
            RecordId = id,
            TableCode = normalizedTableCode,
            DependencyKind = dependencyKind,
            ActiveReferenceCount = count,
            CanDeactivate = count == 0
        };
    }

    public async Task<LibraryHardDeleteResult?> HardDeleteLookupAsync(
        string tableCode,
        Guid id,
        CancellationToken cancellationToken = default)
        => await HardDeleteAsync(tableCode, id, cancellationToken);

    public async Task<LibraryHardDeleteResult?> HardDeleteAsync(
        string tableCode,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var normalizedTableCode = tableCode.Trim().ToLowerInvariant();
        if (normalizedTableCode is not (
            "lookup-categories" or
            "lookup-values" or
            "vpp-categories" or
            "suppliers" or
            "supplier-product-mappings" or
            "departments"))
        {
            return null;
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var result = normalizedTableCode switch
            {
                "lookup-categories" => await PrepareLookupCategoryHardDeleteAsync(id, cancellationToken),
                "lookup-values" => await PrepareLookupValueHardDeleteAsync(id, cancellationToken),
                "vpp-categories" => await PrepareVppCategoryHardDeleteAsync(id, cancellationToken),
                "suppliers" => await PrepareSupplierHardDeleteAsync(id, cancellationToken),
                "supplier-product-mappings" => await PrepareSupplierProductMappingHardDeleteAsync(id, cancellationToken),
                "departments" => await PrepareDepartmentHardDeleteAsync(id, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported hard-delete table '{normalizedTableCode}'.")
            };

            if (result.Status == LibraryHardDeleteStatus.Deleted)
            {
                await _unitOfWork.CommitAsync();
            }
            else
            {
                await _unitOfWork.RollbackAsync();
            }

            return result;
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackAsync();
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, 1);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<LibraryIntegrityValidationResult> ValidateDepartmentParentAsync(
        Guid departmentId,
        Guid? parentDepartmentId,
        CancellationToken cancellationToken = default)
    {
        if (!parentDepartmentId.HasValue) return LibraryIntegrityValidationResult.Valid;
        if (departmentId != Guid.Empty && departmentId == parentDepartmentId)
        {
            return LibraryIntegrityValidationResult.Invalid("A department cannot be its own parent.");
        }

        var departments = await _unitOfWork.VPPContext.Set<Department>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new DepartmentNode(x.Id, x.ParentDepartmentId))
            .ToListAsync(cancellationToken);
        var byId = departments.ToDictionary(x => x.Id);

        if (!byId.ContainsKey(parentDepartmentId.Value))
        {
            return LibraryIntegrityValidationResult.Invalid("The selected parent department does not exist or is inactive.");
        }

        var visited = new HashSet<Guid>();
        var current = parentDepartmentId;
        while (current.HasValue)
        {
            if (!visited.Add(current.Value))
            {
                return LibraryIntegrityValidationResult.Invalid("The department hierarchy already contains a cycle.");
            }

            if (departmentId != Guid.Empty && current == departmentId)
            {
                return LibraryIntegrityValidationResult.Invalid("A department cannot be moved under one of its own descendants.");
            }

            current = byId.TryGetValue(current.Value, out var node) ? node.ParentDepartmentId : null;
        }

        return LibraryIntegrityValidationResult.Valid;
    }

    private async Task<int> GetSupplierReferenceCountAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var mappings = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .CountAsync(x => x.SupplierId == supplierId && !x.IsDeleted, cancellationToken);
        var priceLists = await _unitOfWork.VPPContext.Set<PriceList>()
            .CountAsync(x => x.SupplierId == supplierId && !x.IsDeleted, cancellationToken);
        return mappings + priceLists;
    }

    private async Task<int> GetDepartmentReferenceCountAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var childDepartments = await _unitOfWork.VPPContext.Set<Department>()
            .CountAsync(x => x.ParentDepartmentId == departmentId && !x.IsDeleted, cancellationToken);
        var memberships = await _unitOfWork.VPPContext.Set<gtas_vpp_be.Model.Auth.UserGroupMembership>()
            .CountAsync(x => x.DepartmentId == departmentId && !x.IsDeleted, cancellationToken);
        return childDepartments + memberships;
    }

    private async Task<LibraryHardDeleteResult> PrepareLookupCategoryHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var category = await context.LookupCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        }

        if (!category.IsDeleted)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);
        }

        var referenceCount = await context.LookupValues.CountAsync(x => x.LookupCategoryId == id, cancellationToken);
        if (referenceCount > 0)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);
        }

        var translations = await context.LookupCategoryTranslations
            .Where(x => x.LookupCategoryId == id)
            .ToListAsync(cancellationToken);
        context.LookupCategoryTranslations.RemoveRange(translations);
        context.LookupCategories.Remove(category);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private async Task<LibraryHardDeleteResult> PrepareLookupValueHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var value = await context.LookupValues.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (value is null)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        }

        if (!value.IsDeleted)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);
        }

        var referenceCount = await context.VppItems.CountAsync(x => x.UomId == id, cancellationToken);
        if (referenceCount > 0)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);
        }

        var translations = await context.LookupValueTranslations
            .Where(x => x.LookupValueId == id)
            .ToListAsync(cancellationToken);
        context.LookupValueTranslations.RemoveRange(translations);
        context.LookupValues.Remove(value);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private async Task<LibraryHardDeleteResult> PrepareVppCategoryHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var category = await context.VppCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (category is null) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        if (!category.IsDeleted) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);

        var referenceCount = await context.VppItems.CountAsync(x => x.VppCategoryId == id, cancellationToken);
        if (referenceCount > 0) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);

        var translations = await context.VppCategoryTranslations.Where(x => x.VppCategoryId == id).ToListAsync(cancellationToken);
        context.VppCategoryTranslations.RemoveRange(translations);
        context.VppCategories.Remove(category);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private async Task<LibraryHardDeleteResult> PrepareSupplierHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var supplier = await context.Suppliers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (supplier is null) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        if (!supplier.IsDeleted) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);

        var referenceCount = await context.SupplierProductMappings.CountAsync(x => x.SupplierId == id, cancellationToken)
            + await context.PriceLists.CountAsync(x => x.SupplierId == id, cancellationToken)
            + await context.Settlements.CountAsync(x => x.PrimarySupplierId == id, cancellationToken)
            + await context.SettlementItems.CountAsync(x => x.SupplierId == id, cancellationToken);
        if (referenceCount > 0) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);

        var translations = await context.SupplierTranslations.Where(x => x.SupplierId == id).ToListAsync(cancellationToken);
        context.SupplierTranslations.RemoveRange(translations);
        context.Suppliers.Remove(supplier);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private async Task<LibraryHardDeleteResult> PrepareSupplierProductMappingHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var mapping = await context.SupplierProductMappings.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mapping is null) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        if (!mapping.IsDeleted) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);

        var settlementReferenceCount = await context.SettlementItems.CountAsync(x => x.PriceBookItemId == id, cancellationToken);
        if (settlementReferenceCount > 0)
        {
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, settlementReferenceCount);
        }

        context.SupplierProductMappings.Remove(mapping);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private async Task<LibraryHardDeleteResult> PrepareDepartmentHardDeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = _unitOfWork.VPPContext;
        var department = await context.Departments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (department is null) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
        if (!department.IsDeleted) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);

        var referenceCount = await context.Departments.CountAsync(x => x.ParentDepartmentId == id, cancellationToken)
            + await context.UserGroupMemberships.CountAsync(x => x.DepartmentId == id, cancellationToken);
        if (referenceCount > 0) return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);

        var translations = await context.DepartmentTranslations.Where(x => x.DepartmentId == id).ToListAsync(cancellationToken);
        context.DepartmentTranslations.RemoveRange(translations);
        context.Departments.Remove(department);
        return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
    }

    private sealed record DepartmentNode(Guid Id, Guid? ParentDepartmentId);
}

public sealed record LibraryIntegrityValidationResult(bool IsValid, string? Message)
{
    public static LibraryIntegrityValidationResult Valid { get; } = new(true, null);

    public static LibraryIntegrityValidationResult Invalid(string message) => new(false, message);
}

public enum LibraryHardDeleteStatus
{
    Deleted,
    NotFound,
    MustDeactivate,
    HasDependencies
}

public sealed record LibraryHardDeleteResult(LibraryHardDeleteStatus Status, int ReferenceCount);
