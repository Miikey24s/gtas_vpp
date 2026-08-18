using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Permission;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gtas_vpp_be.Authorization;

public enum PermissionMappingMutationOutcome
{
    Success,
    BadRequest,
    NotFound,
    Conflict
}

public sealed record PermissionMappingMutationError(string Code, string Message);

public sealed record PermissionMappingMutationResult(
    PermissionMappingMutationOutcome Outcome,
    object? Payload);

public interface IPermissionMappingMutationService
{
    Task<PermissionMappingMutationResult> PatchAsync(
        PatchComponentMappingReqDTO request,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<PermissionMappingMutationResult> PatchBatchAsync(
        BatchPatchComponentMappingsReqDTO request,
        int actorUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sở hữu command path thay đổi quyền giao diện. Service kiểm tra trần quyền chuẩn trước khi ghi,
/// giữ batch nguyên tử, ghi audit và chỉ phát tín hiệu làm mới sau khi dữ liệu đã lưu thành công.
/// </summary>
public sealed class PermissionMappingMutationService(
    IGenericRepository<GroupPageComponentMapping> mappingRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IPermissionChangeNotifier permissionChangeNotifier) : IPermissionMappingMutationService
{
    private readonly IGenericRepository<GroupPageComponentMapping> _mappingRepository = mappingRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IPermissionChangeNotifier _permissionChangeNotifier = permissionChangeNotifier;

    public async Task<PermissionMappingMutationResult> PatchAsync(
        PatchComponentMappingReqDTO request,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = (await _mappingRepository.ReadAsync(
                mapping => mapping.PageComponentMappingId == request.PageComponentMappingId
                    && mapping.PermissionGroupId == request.PermissionGroupId))
            .FirstOrDefault();
        if (current is null)
        {
            return NotFound("Component mapping not found.");
        }

        var componentCode = await _unitOfWork.VPPContext.Set<PageComponentMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.Id == current.PageComponentMappingId)
            .Select(mapping => mapping.PermissionComponent != null
                ? mapping.PermissionComponent.ComponentCode
                : string.Empty)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var validation = ValidateChange(
            current.PermissionGroupId,
            current.MemberCompanyCode,
            componentCode,
            request.IsVisible,
            request.IsEnable,
            "This UI component is outside the canonical role ceiling.");
        if (validation is not null)
        {
            return validation;
        }

        var before = new { current.IsVisible, current.IsEnable };
        request.Adapt(current);
        current.UpdatedByUserId = actorUserId;
        current.UpdatedAtUtc = _dateTimeProvider.Now;

        var updated = await _mappingRepository.UpdateAsync(
            current,
            [
                mapping => mapping.IsEnable,
                mapping => mapping.IsVisible,
                mapping => mapping.UpdatedByUserId,
                mapping => mapping.UpdatedAtUtc
            ]);

        Serilog.Log.Information(
            "Permission changed: Actor={ActorUserId}, Group={GroupId}, Company={CompanyCode}, Component={ComponentCode}, Before={@Before}, After={@After}",
            actorUserId,
            current.PermissionGroupId,
            current.MemberCompanyCode,
            componentCode,
            before,
            new { current.IsVisible, current.IsEnable });

        await _permissionChangeNotifier.NotifyGroupChangedAsync(
            current.PermissionGroupId,
            cancellationToken);
        return Success(updated);
    }

    public async Task<PermissionMappingMutationResult> PatchBatchAsync(
        BatchPatchComponentMappingsReqDTO request,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var requestValidation = ValidateBatchRequest(request);
        if (requestValidation is not null)
        {
            return requestValidation;
        }

        IDbContextTransaction? transaction = null;
        try
        {
            if (_unitOfWork.VPPContext.Database.IsRelational())
            {
                transaction = await _unitOfWork.VPPContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);
            }

            var mappings = await LoadBatchMappingsAsync(request, cancellationToken);
            if (mappings.Count != request.Items.Count)
            {
                return NotFound(new PermissionMappingMutationError(
                    "PERMISSION_MAPPING_NOT_FOUND",
                    "One or more permission mappings no longer exist. Reload before saving."));
            }

            var requestedMappings = new List<RequestedMapping>(request.Items.Count);
            var mappingById = mappings.ToDictionary(mapping => mapping.PageComponentMappingId);
            foreach (var item in request.Items)
            {
                var mapping = mappingById[item.PageComponentMappingId];
                var componentCode = mapping.PageComponentMapping?.PermissionComponent?.ComponentCode ?? string.Empty;
                var validation = ValidateChange(
                    request.PermissionGroupId,
                    mapping.MemberCompanyCode,
                    componentCode,
                    item.IsVisible,
                    item.IsEnable,
                    "A UI component is outside the canonical role ceiling.");
                if (validation is not null)
                {
                    return validation;
                }

                requestedMappings.Add(new RequestedMapping(mapping, item));
            }

            var changed = requestedMappings
                .Where(item => item.Mapping.IsVisible != item.Request.IsVisible
                    || item.Mapping.IsEnable != item.Request.IsEnable)
                .ToList();
            ApplyChanges(changed, actorUserId);

            if (changed.Count > 0)
            {
                AddBatchAudit(request, actorUserId, changed.Count);
                await _unitOfWork.VPPContext.SaveChangesAsync(cancellationToken);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            if (changed.Count > 0)
            {
                await _permissionChangeNotifier.NotifyGroupChangedAsync(
                    request.PermissionGroupId,
                    cancellationToken);
            }

            return Success(new BatchPatchComponentMappingsResDTO
            {
                PermissionGroupId = request.PermissionGroupId,
                UpdatedCount = changed.Count
            });
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task<List<GroupPageComponentMapping>> LoadBatchMappingsAsync(
        BatchPatchComponentMappingsReqDTO request,
        CancellationToken cancellationToken)
    {
        var mappingIds = request.Items.Select(item => item.PageComponentMappingId).ToArray();
        return await _unitOfWork.VPPContext.Set<GroupPageComponentMapping>()
            .Include(mapping => mapping.PageComponentMapping)!
            .ThenInclude(mapping => mapping!.PermissionComponent)
            .Where(mapping => mapping.PermissionGroupId == request.PermissionGroupId
                && mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                && mappingIds.Contains(mapping.PageComponentMappingId))
            .ToListAsync(cancellationToken);
    }

    private void ApplyChanges(IReadOnlyCollection<RequestedMapping> changed, int actorUserId)
    {
        foreach (var item in changed)
        {
            item.Mapping.IsVisible = item.Request.IsVisible;
            item.Mapping.IsEnable = item.Request.IsEnable;
            item.Mapping.UpdatedByUserId = actorUserId;
            item.Mapping.UpdatedAtUtc = _dateTimeProvider.Now;
        }
    }

    private void AddBatchAudit(
        BatchPatchComponentMappingsReqDTO request,
        int actorUserId,
        int updatedCount)
    {
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Cập nhật quyền giao diện theo lô."
            : request.Reason.Trim();
        _unitOfWork.VPPContext.SecurityAudits.Add(new SecurityAudit
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId > 0 ? actorUserId : null,
            Action = "PERMISSION_UI_BATCH_UPDATED",
            ResourceType = "PermissionGroup",
            ResourceId = request.PermissionGroupId.ToString(),
            Outcome = "Succeeded",
            Summary = $"Updated {updatedCount} UI permission mapping(s).",
            Reason = reason.Length > 500 ? reason[..500] : reason,
            CorrelationId = Activity.Current?.TraceId.ToString(),
            OccurredAtUtc = _dateTimeProvider.Now
        });
    }

    private static PermissionMappingMutationResult? ValidateBatchRequest(
        BatchPatchComponentMappingsReqDTO request)
    {
        if (request.PermissionGroupId == Guid.Empty || request.Items.Count is 0 or > 500)
        {
            return BadRequest(
                "INVALID_PERMISSION_BATCH",
                "A canonical group and between 1 and 500 mappings are required.");
        }

        if (request.Items.Any(item => item.PageComponentMappingId == Guid.Empty)
            || request.Items.Select(item => item.PageComponentMappingId).Distinct().Count() != request.Items.Count)
        {
            return BadRequest(
                "DUPLICATE_PERMISSION_MAPPING",
                "Each permission mapping must appear exactly once.");
        }

        return CanonicalRbac.Personas.Any(persona => persona.GroupId == request.PermissionGroupId)
            ? null
            : Conflict(
                "NON_CANONICAL_ROLE_MAPPING",
                "Only canonical single-company role mappings can be administered.");
    }

    private static PermissionMappingMutationResult? ValidateChange(
        Guid groupId,
        long memberCompanyCode,
        string componentCode,
        bool isVisible,
        bool isEnable,
        string outsideRoleCeilingMessage)
    {
        if (memberCompanyCode != CanonicalRbac.DefaultMemberCompanyCode
            || !CanonicalRbac.Personas.Any(persona => persona.GroupId == groupId))
        {
            return Conflict(
                "NON_CANONICAL_ROLE_MAPPING",
                "Only canonical single-company role mappings can be administered.");
        }

        if (Permissions.IsActionCode(componentCode))
        {
            return Conflict(
                "ACTION_MATRIX_IMMUTABLE",
                "Backend action grants are fixed by the reviewed role matrix.");
        }

        if (!CanonicalRbac.GetUiComponents(groupId)
                .Contains(componentCode, StringComparer.OrdinalIgnoreCase))
        {
            return Conflict(
                "COMPONENT_OUTSIDE_ROLE_CEILING",
                outsideRoleCeilingMessage);
        }

        if (isEnable && !isVisible)
        {
            return BadRequest(
                "ENABLED_COMPONENT_MUST_BE_VISIBLE",
                "An enabled UI component must also be visible.");
        }

        var isProtectedSystemAdminNavigation = groupId == CanonicalRbac.SystemAdmin.GroupId
            && (string.Equals(componentCode, Permissions.MenuPermission, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase));
        return isProtectedSystemAdminNavigation && (!isVisible || !isEnable)
            ? Conflict(
                "SYSTEM_ADMIN_NAVIGATION_REQUIRED",
                "System Admin access-administration navigation cannot be disabled.")
            : null;
    }

    private static PermissionMappingMutationResult Success(object? payload) =>
        new(PermissionMappingMutationOutcome.Success, payload);

    private static PermissionMappingMutationResult BadRequest(string code, string message) =>
        new(PermissionMappingMutationOutcome.BadRequest, new PermissionMappingMutationError(code, message));

    private static PermissionMappingMutationResult NotFound(object payload) =>
        new(PermissionMappingMutationOutcome.NotFound, payload);

    private static PermissionMappingMutationResult Conflict(string code, string message) =>
        new(PermissionMappingMutationOutcome.Conflict, new PermissionMappingMutationError(code, message));

    private sealed record RequestedMapping(
        GroupPageComponentMapping Mapping,
        BatchPatchComponentMappingItemReqDTO Request);
}
