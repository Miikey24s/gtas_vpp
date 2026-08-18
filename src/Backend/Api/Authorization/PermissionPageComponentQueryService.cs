using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using PermissionComponentDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionComponentAccessResDTO;
using PermissionPageDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionPageComponentResDTO;

namespace gtas_vpp_be.Authorization;

public sealed record PermissionPageComponentQueryResult(
    bool PersonaExists,
    IReadOnlyList<PermissionPageDto> Items);

public interface IPermissionPageComponentQueryService
{
    Task<PermissionPageComponentQueryResult> GetAsync(
        Guid groupId,
        bool includeDeleted,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sở hữu read path của ma trận quyền theo persona. Service giữ nguyên trần quyền chuẩn,
/// gom dữ liệu theo trang và chỉ cho UI cấu hình những component được phép thay đổi.
/// </summary>
public sealed class PermissionPageComponentQueryService(VPPContext context)
    : IPermissionPageComponentQueryService
{
    private readonly VPPContext _context = context;

    public async Task<PermissionPageComponentQueryResult> GetAsync(
        Guid groupId,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        if (!CanonicalRbac.Personas.Any(persona => persona.GroupId == groupId))
        {
            return new PermissionPageComponentQueryResult(false, []);
        }

        var query = _context.Set<GroupPageComponentMapping>()
            .AsNoTracking()
            .Include(mapping => mapping.PageComponentMapping)!
                .ThenInclude(mapping => mapping!.PermissionPage)
            .Include(mapping => mapping.PageComponentMapping)!
                .ThenInclude(mapping => mapping!.PermissionComponent)
            .Where(mapping => mapping.PermissionGroupId == groupId
                && mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                && mapping.PageComponentMapping != null
                && mapping.PageComponentMapping.PermissionPage != null
                && mapping.PageComponentMapping.PermissionComponent != null);

        if (!includeDeleted)
        {
            query = query.Where(mapping =>
                !mapping.PageComponentMapping!.PermissionPage!.IsDeleted
                && !mapping.PageComponentMapping.PermissionComponent!.IsDeleted);
        }

        var groupMappings = await query.ToListAsync(cancellationToken);
        if (groupMappings.Count == 0)
        {
            return new PermissionPageComponentQueryResult(true, []);
        }

        var companyLookup = await BuildCompanyLookupAsync(groupMappings, cancellationToken);
        var pages = BuildPages(groupId, groupMappings, companyLookup);
        return new PermissionPageComponentQueryResult(true, pages);
    }

    private async Task<IReadOnlyDictionary<long, v_WFXCompany>> BuildCompanyLookupAsync(
        IReadOnlyCollection<GroupPageComponentMapping> groupMappings,
        CancellationToken cancellationToken)
    {
        var memberCompanyCodes = groupMappings
            .Select(mapping => mapping.MemberCompanyCode)
            .Distinct()
            .ToArray();

        if (memberCompanyCodes.Length == 0)
        {
            return new Dictionary<long, v_WFXCompany>();
        }

        return (await _context.v_WFXCompanies
                .AsNoTracking()
                .Where(company => memberCompanyCodes.Contains(company.MemberCompanyCode))
                .ToListAsync(cancellationToken))
            .GroupBy(company => company.MemberCompanyCode)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static List<PermissionPageDto> BuildPages(
        Guid groupId,
        IReadOnlyCollection<GroupPageComponentMapping> groupMappings,
        IReadOnlyDictionary<long, v_WFXCompany> companyLookup) =>
        groupMappings
            .GroupBy(mapping => mapping.PageComponentMapping!.PermissionPageId)
            .OrderBy(group => group.First().PageComponentMapping!.PermissionPage!.PageCode)
            .Select(pageGroup => BuildPage(groupId, pageGroup, companyLookup))
            .ToList();

    private static PermissionPageDto BuildPage(
        Guid groupId,
        IGrouping<Guid, GroupPageComponentMapping> pageGroup,
        IReadOnlyDictionary<long, v_WFXCompany> companyLookup)
    {
        var page = pageGroup.First().PageComponentMapping!.PermissionPage!;
        return new PermissionPageDto
        {
            GroupId = groupId,
            PageId = page.Id,
            PageCode = page.PageCode,
            PageName = page.PageName,
            Description = page.Description,
            CreatedByUserId = page.CreatedByUserId,
            CreatedAtUtc = page.CreatedAtUtc,
            UpdatedByUserId = page.UpdatedByUserId,
            UpdatedAtUtc = page.UpdatedAtUtc,
            IsDeleted = page.IsDeleted,
            Components = pageGroup
                .OrderBy(mapping => mapping.PageComponentMapping!.PermissionComponent!.ComponentName)
                .Select(mapping => BuildComponent(groupId, page.Id, mapping, companyLookup))
                .ToList()
        };
    }

    private static PermissionComponentDto BuildComponent(
        Guid groupId,
        Guid pageId,
        GroupPageComponentMapping groupMapping,
        IReadOnlyDictionary<long, v_WFXCompany> companyLookup)
    {
        var pageComponentMapping = groupMapping.PageComponentMapping!;
        var component = pageComponentMapping.PermissionComponent!;
        companyLookup.TryGetValue(groupMapping.MemberCompanyCode, out var company);

        var isActionGrant = Permissions.IsActionCode(component.ComponentCode);
        var administrationMode = ResolveAdministrationMode(groupId, component.ComponentCode, isActionGrant);

        return new PermissionComponentDto
        {
            ComponentId = component.Id,
            ComponentCode = component.ComponentCode,
            ComponentName = component.ComponentName,
            Description = component.Description,
            IsVisible = groupMapping.IsVisible,
            IsEnable = groupMapping.IsEnable,
            PageId = pageId,
            GroupId = groupId,
            GroupPageComponentMappingId = pageComponentMapping.Id,
            MemberCompanyCode = groupMapping.MemberCompanyCode,
            CompanyName = company?.CompanyName,
            CompanyShortName = company?.CompanyShortName,
            IsDeleted = component.IsDeleted,
            IsActionGrant = isActionGrant,
            CanConfigure = !component.IsDeleted
                && string.Equals(administrationMode, "Configurable", StringComparison.Ordinal),
            AdministrationMode = administrationMode
        };
    }

    private static string ResolveAdministrationMode(
        Guid groupId,
        string componentCode,
        bool isActionGrant)
    {
        if (isActionGrant)
        {
            return "ActionMatrix";
        }

        // Ba điểm vào quản trị quyền luôn được giữ cho System Admin để tránh tự khóa hệ thống.
        var isProtectedSystemAdminNavigation = groupId == CanonicalRbac.SystemAdmin.GroupId
            && (string.Equals(componentCode, Permissions.MenuPermission, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentCode, Permissions.PermissionUser, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentCode, Permissions.PermissionComponent, StringComparison.OrdinalIgnoreCase));
        if (isProtectedSystemAdminNavigation)
        {
            return "Required";
        }

        return CanonicalRbac.GetUiComponents(groupId)
            .Contains(componentCode, StringComparer.OrdinalIgnoreCase)
                ? "Configurable"
                : "OutsideRoleCeiling";
    }
}
