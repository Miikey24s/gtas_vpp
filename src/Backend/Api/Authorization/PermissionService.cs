using System.Security.Claims;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Authorization;

public interface IPermissionService
{
    Task<PermissionSnapshotResDTO> GetSnapshotAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        string permissionCode,
        CancellationToken cancellationToken = default);
}

public sealed class PermissionService(VPPContext context) : IPermissionService
{
    private readonly VPPContext _context = context;

    public async Task<PermissionSnapshotResDTO> GetSnapshotAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userIdClaim = user.FindFirst("UserID")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user.Identity?.IsAuthenticated != true || !int.TryParse(userIdClaim, out var userId))
        {
            return new PermissionSnapshotResDTO();
        }

        var canonicalGroupIds = CanonicalRbac.Personas
            .Select(persona => persona.GroupId)
            .ToArray();
        var activeGroups = await _context.Set<UserGroupMembership>()
            .AsNoTracking()
            .Where(mapping => mapping.UserId == userId
                && mapping.AccountId == userId
                && !mapping.IsDeleted
                && mapping.PermissionGroup != null
                && !mapping.PermissionGroup.IsDeleted
                && mapping.PermissionGroup.ParentGroupId == null
                && canonicalGroupIds.Contains(mapping.PermissionGroupId))
            .Select(mapping => new
            {
                mapping.PermissionGroupId,
                mapping.PermissionGroup!.GroupCode
            })
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        // Identity contract hiện tại chỉ hỗ trợ đúng một group đang hoạt động.
        // Fail-closed khi dữ liệu legacy có không hoặc nhiều assignment.
        if (activeGroups.Count != 1)
        {
            return new PermissionSnapshotResDTO();
        }

        var activeGroup = activeGroups[0];
        var canonicalGroup = CanonicalRbac.Personas.SingleOrDefault(persona =>
            persona.GroupId == activeGroup.PermissionGroupId
            && string.Equals(persona.GroupCode, activeGroup.GroupCode, StringComparison.Ordinal));
        if (canonicalGroup is null)
        {
            return new PermissionSnapshotResDTO();
        }

        var groupId = canonicalGroup.GroupId;

        var companyClaim = user.FindFirst("MemberCompanyCode")?.Value;
        var hasCompany = long.TryParse(companyClaim, out var memberCompanyCode);

        var mappings = await _context.Set<GroupPageComponentMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.PermissionGroupId == groupId
                && mapping.PageComponentMapping != null
                && mapping.PageComponentMapping.PermissionPage != null
                && !mapping.PageComponentMapping.PermissionPage.IsDeleted
                && mapping.PageComponentMapping.PermissionComponent != null
                && !mapping.PageComponentMapping.PermissionComponent.IsDeleted
                && (!hasCompany || mapping.MemberCompanyCode == memberCompanyCode))
            .Select(mapping => new
            {
                mapping.MemberCompanyCode,
                mapping.IsVisible,
                mapping.IsEnable,
                mapping.UpdatedAtUtc,
                PageCode = mapping.PageComponentMapping!.PermissionPage!.PageCode,
                ComponentCode = mapping.PageComponentMapping.PermissionComponent!.ComponentCode
            })
            .ToListAsync(cancellationToken);

        // Token cũ có thể thiếu company claim. Chỉ chấp nhận khi group ánh xạ đúng
        // một công ty, tránh hợp quyền mơ hồ giữa nhiều công ty.
        if (!hasCompany)
        {
            var companies = mappings.Select(mapping => mapping.MemberCompanyCode).Distinct().ToArray();
            if (companies.Length != 1)
            {
                return new PermissionSnapshotResDTO { GroupId = groupId };
            }

            memberCompanyCode = companies[0];
            hasCompany = true;
        }

        var grantedComponents = mappings
            .Where(mapping => mapping.IsVisible && mapping.IsEnable)
            .Select(mapping => mapping.ComponentCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Khả năng hiển thị component UI/menu được tách chủ động khỏi quyền backend.
        // Chỉ action code được seed tường minh mới thỏa policy; component legacy
        // không được mở rộng thành quyền lớn hơn.
        var actionCeiling = CanonicalRbac.GetActionPermissions(groupId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effectivePermissions = grantedComponents
            .Where(Permissions.IsActionCode)
            .Where(actionCeiling.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new PermissionSnapshotResDTO
        {
            Version = mappings.Count == 0 ? 0 : mappings.Max(mapping => mapping.UpdatedAtUtc.Ticks),
            GroupId = groupId,
            MemberCompanyCode = hasCompany ? memberCompanyCode : null,
            Permissions = effectivePermissions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Pages = mappings
                .GroupBy(mapping => mapping.PageCode, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new PermissionSnapshotPageResDTO
                {
                    PageCode = group.Key,
                    Components = group
                        .GroupBy(mapping => mapping.ComponentCode, StringComparer.OrdinalIgnoreCase)
                        .Select(componentGroup => new PermissionComponentResDTO
                        {
                            ComponentCode = componentGroup.Key,
                            IsVisible = componentGroup.Any(mapping => mapping.IsVisible),
                            IsEnable = componentGroup.Any(mapping => mapping.IsVisible && mapping.IsEnable)
                        })
                        .OrderBy(component => component.ComponentCode, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal user,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await GetSnapshotAsync(user, cancellationToken);
        return snapshot.Permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}
