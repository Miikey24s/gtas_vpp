using System.Globalization;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using AuthGroupDto = gtas_vpp_shared.DTOs.Res.Auth.PermissionGroupResDTO;
using UserListDto = gtas_vpp_shared.DTOs.Res.Auth.UserAdministrationResDTO;

namespace gtas_vpp_be.Authorization;

public sealed record UserAdministrationQuery(
    string? Search,
    string? AccountStatus,
    Guid? GroupId,
    Guid? DepartmentId,
    bool? HasActiveMembership,
    string? Filter,
    int? Skip,
    int? Top,
    string? OrderBy,
    string? Distinct,
    string? DistinctFilter);

public sealed record UserAdministrationQueryResult(
    IReadOnlyList<UserListDto> Items,
    int TotalCount,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public bool Succeeded => ErrorCode is null;

    public static UserAdministrationQueryResult InvalidAccountStatus() =>
        new(
            [],
            0,
            "INVALID_ACCOUNT_STATUS",
            "Account status must be Active, PendingApproval or Disabled.");
}

public interface IUserAdministrationQueryService
{
    Task<UserAdministrationQueryResult> GetPageAsync(
        UserAdministrationQuery request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sở hữu read path của danh sách tài khoản quản trị. Nguồn tài khoản luôn là
/// Identity của ứng dụng; membership chỉ bổ sung persona và phòng ban hiện hành.
/// </summary>
public sealed class UserAdministrationQueryService(
    VPPContext context,
    IUserNameResolver userNameResolver) : IUserAdministrationQueryService
{
    private readonly VPPContext _context = context;
    private readonly IUserNameResolver _userNameResolver = userNameResolver;

    public async Task<UserAdministrationQueryResult> GetPageAsync(
        UserAdministrationQuery request,
        CancellationToken cancellationToken = default)
    {
        var usersQuery = _context.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.AccountStatus))
        {
            if (!Enum.TryParse<AppAccountStatus>(request.AccountStatus.Trim(), true, out var parsedStatus))
            {
                return UserAdministrationQueryResult.InvalidAccountStatus();
            }

            usersQuery = usersQuery.Where(user => user.AccountStatus == parsedStatus);
        }

        var searchTerms = VietnameseSearch.Tokenize(request.Search);
        if (searchTerms.Count > 0)
        {
            if (_context.Database.IsSqlServer())
            {
                foreach (var term in searchTerms)
                {
                    var pattern = VietnameseSearch.BuildContainsPattern(term);
                    usersQuery = usersQuery.Where(user =>
                        (user.UserName != null && EF.Functions.Like(EF.Functions.Collate(user.UserName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                        || (user.UserName != null && EF.Functions.Like(EF.Functions.Collate(user.UserName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                        || (user.Email != null && EF.Functions.Like(EF.Functions.Collate(user.Email.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                        || (user.FullName != null && EF.Functions.Like(EF.Functions.Collate(user.FullName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                        || (user.FullName != null && EF.Functions.Like(EF.Functions.Collate(user.FullName.Replace(" ", "").Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")));
                }
            }
            else
            {
                foreach (var term in searchTerms)
                {
                    usersQuery = usersQuery.Where(user =>
                        VietnameseSearch.Normalize(user.UserName).Contains(term)
                        || VietnameseSearch.Compact(user.UserName).Contains(term)
                        || VietnameseSearch.Normalize(user.Email).Contains(term)
                        || VietnameseSearch.Normalize(user.FullName).Contains(term)
                        || VietnameseSearch.Compact(user.FullName).Contains(term));
                }
            }
        }

        // Chỉ membership app-owned, active và cùng account/user mới được chiếu lên màn quản trị.
        var memberships = _context.Set<UserGroupMembership>()
            .AsNoTracking()
            .Where(mapping => !mapping.IsDeleted
                && mapping.AccountId.HasValue
                && mapping.UserId == mapping.AccountId)
            .Include(mapping => mapping.PermissionGroup)
            .Include(mapping => mapping.Department);

        IQueryable<UserListDto> query =
            from user in usersQuery
            join membership in memberships on (int?)user.Id equals membership.AccountId into membershipJoin
            from membership in membershipJoin.DefaultIfEmpty()
            select new UserListDto
            {
                Id = membership == null ? Guid.Empty : membership.Id,
                UserId = user.Id,
                UserLogin = user.UserName,
                FullName = user.FullName,
                Email = user.Email,
                EmployeeCode = user.EmployeeCode,
                EmailConfirmed = user.EmailConfirmed,
                MustChangePassword = user.MustChangePassword,
                GoogleEmail = null,
                IsAdmin = membership != null
                          && membership.PermissionGroup != null
                          && membership.PermissionGroup.GroupCode == CanonicalRbac.SystemAdmin.GroupCode,
                GroupId = membership == null ? Guid.Empty : membership.PermissionGroupId,
                GroupName = membership == null || membership.PermissionGroup == null
                    ? string.Empty
                    : membership.PermissionGroup.GroupName,
                CreatedByUserId = membership == null ? 0 : membership.CreatedByUserId,
                CreatedAtUtc = membership == null ? null : membership.CreatedAtUtc,
                UpdatedByUserId = membership == null ? 0 : membership.UpdatedByUserId,
                UpdatedAtUtc = membership == null ? null : membership.UpdatedAtUtc,
                // Hai trường tương thích này giữ nguyên contract của Radzen grid hiện tại.
                IsDeleted = user.AccountStatus != AppAccountStatus.Active || membership == null,
                UserType = user.AccountStatus == AppAccountStatus.Active
                    ? "Active application account"
                    : user.AccountStatus == AppAccountStatus.PendingApproval
                        ? "Pending approval"
                        : "Disabled application account",
                Description = membership == null ? null : membership.Description,
                DepartmentName = membership == null || membership.Department == null
                    ? string.Empty
                    : membership.Department.Name,
                DepartmentId = membership == null ? null : membership.DepartmentId,
                AccountStatus = user.AccountStatus == AppAccountStatus.Active
                    ? nameof(AppAccountStatus.Active)
                    : user.AccountStatus == AppAccountStatus.PendingApproval
                        ? nameof(AppAccountStatus.PendingApproval)
                        : nameof(AppAccountStatus.Disabled),
                LastLoginAtUtc = user.LastLoginAtUtc,
                SessionVersion = user.SessionVersion,
                GroupCode = membership == null || membership.PermissionGroup == null
                    ? null
                    : membership.PermissionGroup.GroupCode,
                IsActive = user.AccountStatus == AppAccountStatus.Active && membership != null,
                RowVersion = membership == null ? null : membership.RowVersion,
                UserGroup = membership == null || membership.PermissionGroup == null
                    ? null
                    : new AuthGroupDto
                    {
                        Id = membership.PermissionGroup.Id,
                        GroupName = membership.PermissionGroup.GroupName,
                        ParentGroupId = membership.PermissionGroup.ParentGroupId,
                        Description = membership.PermissionGroup.Description,
                        CreatedByUserId = membership.PermissionGroup.CreatedByUserId,
                        CreatedAtUtc = membership.PermissionGroup.CreatedAtUtc,
                        UpdatedByUserId = membership.PermissionGroup.UpdatedByUserId,
                        UpdatedAtUtc = membership.PermissionGroup.UpdatedAtUtc,
                        IsDeleted = membership.PermissionGroup.IsDeleted
                    }
            };

        query = ApplyTypedFilters(query, request);
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
        page = await _userNameResolver.WithUserNamesAsync(page, _context);
        return new UserAdministrationQueryResult(page, totalCount);
    }

    private static IQueryable<UserListDto> ApplyTypedFilters(
        IQueryable<UserListDto> query,
        UserAdministrationQuery request)
    {
        if (request.GroupId.HasValue)
        {
            query = query.Where(user => user.GroupId == request.GroupId.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(user => user.DepartmentId == request.DepartmentId.Value);
        }

        if (request.HasActiveMembership.HasValue)
        {
            query = request.HasActiveMembership.Value
                ? query.Where(user => user.GroupId != Guid.Empty)
                : query.Where(user => user.GroupId == Guid.Empty);
        }

        return query;
    }

    private static IQueryable<UserListDto> ApplyDynamicFilter(
        IQueryable<UserListDto> query,
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
            // Biểu thức Radzen chưa hỗ trợ không được làm hỏng toàn bộ danh sách quản trị.
            return query;
        }
    }

    private static async Task<UserAdministrationQueryResult?> TryBuildDistinctPageAsync(
        IQueryable<UserListDto> query,
        UserAdministrationQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Distinct))
        {
            return null;
        }

        var property = typeof(UserListDto).GetProperty(request.Distinct);
        if (property is null)
        {
            return null;
        }

        var values = await query
            .Select(request.Distinct)
            .Distinct()
            .ToDynamicListAsync(cancellationToken);
        var normalizedDistinctFilter = VietnameseSearch.Normalize(request.DistinctFilter);
        var filteredValues = values
            .Where(value => value != null)
            .Where(value => VietnameseSearch.Contains(
                Convert.ToString(value, CultureInfo.CurrentCulture),
                normalizedDistinctFilter))
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
            var dto = new UserListDto();
            property.SetValue(dto, value);
            return dto;
        }).ToList();

        return new UserAdministrationQueryResult(items, filteredValues.Count);
    }

    private static IQueryable<UserListDto> ApplyOrder(
        IQueryable<UserListDto> query,
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

    private static IOrderedQueryable<UserListDto> DefaultOrder(IQueryable<UserListDto> query) =>
        query.OrderBy(user => user.IsDeleted)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.UserLogin);
}
