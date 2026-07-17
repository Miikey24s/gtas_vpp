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
        var activeGroups = await _context.Set<P04_UserGroup>()
            .AsNoTracking()
            .Where(mapping => mapping.UserId == userId
                && mapping.AccountId == userId
                && !mapping.IsDeleted
                && mapping.P02_Group != null
                && !mapping.P02_Group.IsDeleted
                && mapping.P02_Group.ParentGroupId == null
                && canonicalGroupIds.Contains(mapping.P02_GroupId))
            .Select(mapping => new
            {
                mapping.P02_GroupId,
                mapping.P02_Group!.GroupCode
            })
            .Distinct()
            .Take(2)
            .ToListAsync(cancellationToken);

        // The current identity contract supports exactly one active group. Fail
        // closed when legacy data contains zero or multiple assignments.
        if (activeGroups.Count != 1)
        {
            return new PermissionSnapshotResDTO();
        }

        var activeGroup = activeGroups[0];
        var canonicalGroup = CanonicalRbac.Personas.SingleOrDefault(persona =>
            persona.GroupId == activeGroup.P02_GroupId
            && string.Equals(persona.GroupCode, activeGroup.GroupCode, StringComparison.Ordinal));
        if (canonicalGroup is null)
        {
            return new PermissionSnapshotResDTO();
        }

        var groupId = canonicalGroup.GroupId;

        var companyClaim = user.FindFirst("MemberCompanyCode")?.Value;
        var hasCompany = long.TryParse(companyClaim, out var memberCompanyCode);

        var mappings = await _context.Set<P06_GroupPageComponentMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.P02_GroupId == groupId
                && mapping.P05_PageComponentMapping != null
                && mapping.P05_PageComponentMapping.P01_Page != null
                && !mapping.P05_PageComponentMapping.P01_Page.IsDeleted
                && mapping.P05_PageComponentMapping.P03_Component != null
                && !mapping.P05_PageComponentMapping.P03_Component.IsDeleted
                && (!hasCompany || mapping.MemberCompanyCode == memberCompanyCode))
            .Select(mapping => new
            {
                mapping.MemberCompanyCode,
                mapping.IsVisible,
                mapping.IsEnable,
                mapping.UpdateDate,
                PageCode = mapping.P05_PageComponentMapping!.P01_Page!.PageCode,
                ComponentCode = mapping.P05_PageComponentMapping.P03_Component!.ComponentCode
            })
            .ToListAsync(cancellationToken);

        // Older tokens may not contain a company claim. They are accepted only
        // when the group has mappings for exactly one company, preventing an
        // ambiguous cross-company permission union.
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

        // UI/menu component visibility is deliberately separate from backend
        // authority. Only explicitly seeded action codes can satisfy a policy;
        // no legacy component is expanded into broader permissions.
        var actionCeiling = CanonicalRbac.GetActionPermissions(groupId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var effectivePermissions = grantedComponents
            .Where(Permissions.IsActionCode)
            .Where(actionCeiling.Contains)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new PermissionSnapshotResDTO
        {
            Version = mappings.Count == 0 ? 0 : mappings.Max(mapping => mapping.UpdateDate.Ticks),
            GroupId = groupId,
            MemberCompanyCode = hasCompany ? memberCompanyCode : null,
            Permissions = effectivePermissions.Order(StringComparer.OrdinalIgnoreCase).ToList(),
            Pages = mappings
                .GroupBy(mapping => mapping.PageCode, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new PermissionPageResDTO
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
