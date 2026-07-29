using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface ILibraryIntegrityService
{
    Task<LibraryDependencyImpactResDTO?> GetDependencyImpactAsync(string tableCode, Guid id, CancellationToken cancellationToken = default);
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

    private sealed record DepartmentNode(Guid Id, Guid? ParentDepartmentId);
}

public sealed record LibraryIntegrityValidationResult(bool IsValid, string? Message)
{
    public static LibraryIntegrityValidationResult Valid { get; } = new(true, null);

    public static LibraryIntegrityValidationResult Invalid(string message) => new(false, message);
}
