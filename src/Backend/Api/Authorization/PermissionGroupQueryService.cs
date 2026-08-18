using System.Globalization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Permission;
using Mapster;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace gtas_vpp_be.Authorization;

public sealed record PermissionGroupQuery(
    bool IncludeUserNames,
    string? Filter,
    int? Skip,
    int? Top,
    string? OrderBy,
    string? Distinct,
    string? DistinctFilter);

public sealed record PermissionGroupQueryResult(
    IReadOnlyList<PermissionGroupResDTO> Items,
    int TotalCount);

public interface IPermissionGroupQueryService
{
    Task<PermissionGroupQueryResult> GetPageAsync(
        PermissionGroupQuery request,
        CancellationToken cancellationToken = default);

    Task<PermissionGroupResDTO?> GetByIdAsync(
        Guid id,
        bool includeUserNames,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sở hữu read path của danh sách persona chuẩn. Service luôn loại group legacy,
/// tính số người dùng/quyền từ dữ liệu hiện hành và giữ contract lọc của Radzen.
/// </summary>
public sealed class PermissionGroupQueryService(
    VPPContext context,
    IUserNameResolver userNameResolver) : IPermissionGroupQueryService
{
    private readonly VPPContext _context = context;
    private readonly IUserNameResolver _userNameResolver = userNameResolver;

    public async Task<PermissionGroupQueryResult> GetPageAsync(
        PermissionGroupQuery request,
        CancellationToken cancellationToken = default)
    {
        var canonicalGroupIds = CanonicalRbac.Personas
            .Select(persona => persona.GroupId)
            .ToArray();

        IQueryable<PermissionGroupResDTO> query = _context.Set<PermissionGroup>()
            .AsNoTracking()
            .Where(group => canonicalGroupIds.Contains(group.Id) && !group.IsDeleted)
            .Select(group => new PermissionGroupResDTO
            {
                Id = group.Id,
                Description = group.Description,
                CreatedByUserId = group.CreatedByUserId,
                CreatedAtUtc = group.CreatedAtUtc,
                UpdatedByUserId = group.UpdatedByUserId,
                UpdatedAtUtc = group.UpdatedAtUtc,
                IsDeleted = group.IsDeleted,
                MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
                GroupCode = group.GroupCode,
                GroupName = group.GroupName,
                ParentGroupId = group.ParentGroupId,
                UserCount = group.UserGroupMemberships == null
                    ? 0
                    : group.UserGroupMemberships
                        .Where(membership => !membership.IsDeleted)
                        .Select(membership => membership.AccountId ?? membership.UserId)
                        .Distinct()
                        .Count(),
                PermissionCount = group.GroupPageComponentMappings == null
                    ? 0
                    : group.GroupPageComponentMappings.Count(mapping =>
                        mapping.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                        && mapping.IsVisible
                        && mapping.IsEnable
                        && mapping.PageComponentMapping != null
                        && mapping.PageComponentMapping.PermissionPage != null
                        && !mapping.PageComponentMapping.PermissionPage.IsDeleted
                        && mapping.PageComponentMapping.PermissionComponent != null
                        && !mapping.PageComponentMapping.PermissionComponent.IsDeleted)
            });

        query = ApplyDynamicFilter(query, request.Filter);
        var distinctResult = await TryBuildDistinctPageAsync(query, request, cancellationToken);
        if (distinctResult is not null)
        {
            return distinctResult;
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplyOrder(query, request.OrderBy);
        if (request.Skip.GetValueOrDefault() > 0)
        {
            query = query.Skip(request.Skip!.Value);
        }

        if (request.Top.GetValueOrDefault() > 0)
        {
            query = query.Take(request.Top!.Value);
        }

        var page = await query.ToListAsync(cancellationToken);
        if (request.IncludeUserNames)
        {
            page = await _userNameResolver.WithUserNamesAsync(page, _context);
        }

        return new PermissionGroupQueryResult(page, totalCount);
    }

    public async Task<PermissionGroupResDTO?> GetByIdAsync(
        Guid id,
        bool includeUserNames,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Set<PermissionGroup>()
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Id == id && !group.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (includeUserNames)
        {
            await _userNameResolver.IncludeUserInfoAsync(entity, _context);
        }

        return entity.Adapt<PermissionGroupResDTO>();
    }

    private static IQueryable<PermissionGroupResDTO> ApplyDynamicFilter(
        IQueryable<PermissionGroupResDTO> query,
        string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        try
        {
            return query.Where(filter);
        }
        catch
        {
            // Biểu thức Radzen chưa hỗ trợ không được làm hỏng danh sách persona chuẩn.
            return query;
        }
    }

    private static async Task<PermissionGroupQueryResult?> TryBuildDistinctPageAsync(
        IQueryable<PermissionGroupResDTO> query,
        PermissionGroupQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Distinct))
        {
            return null;
        }

        var property = typeof(PermissionGroupResDTO).GetProperty(request.Distinct);
        if (property is null)
        {
            return null;
        }

        var values = await query
            .Select(request.Distinct)
            .Distinct()
            .ToDynamicListAsync(cancellationToken);
        var filteredValues = values
            .Where(value => value != null)
            .Where(value => string.IsNullOrWhiteSpace(request.DistinctFilter)
                || (Convert.ToString(value, CultureInfo.CurrentCulture)?.Contains(
                    request.DistinctFilter,
                    StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        IEnumerable<object> pageValues = filteredValues.Cast<object>();
        if (request.Skip.GetValueOrDefault() > 0)
        {
            pageValues = pageValues.Skip(request.Skip!.Value);
        }

        if (request.Top.GetValueOrDefault() > 0)
        {
            pageValues = pageValues.Take(request.Top!.Value);
        }

        var items = pageValues.Select(value =>
        {
            var dto = new PermissionGroupResDTO();
            property.SetValue(dto, value);
            return dto;
        }).ToList();

        return new PermissionGroupQueryResult(items, filteredValues.Count);
    }

    private static IQueryable<PermissionGroupResDTO> ApplyOrder(
        IQueryable<PermissionGroupResDTO> query,
        string? orderBy)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return DefaultOrder(query);
        }

        try
        {
            return query.OrderBy(orderBy);
        }
        catch
        {
            return DefaultOrder(query);
        }
    }

    private static IOrderedQueryable<PermissionGroupResDTO> DefaultOrder(
        IQueryable<PermissionGroupResDTO> query) =>
        query.OrderBy(group => group.IsDeleted).ThenBy(group => group.GroupName);
}
