using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Authorization;

public sealed record CurrentUserSnapshot(
    AppUser Account,
    P04_UserGroup Membership,
    P02_Group Group,
    LEX02_CompanyDepartmentLocation PrimaryDepartment);

public interface ICurrentUserContext
{
    Task<CurrentUserSnapshot?> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<CurrentUserSnapshot?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    void EnrichPrincipal(ClaimsPrincipal principal, CurrentUserSnapshot snapshot);
}

public sealed class CurrentUserContext(VPPContext context) : ICurrentUserContext
{
    private readonly VPPContext _context = context;
    private int? _cachedUserId;
    private CurrentUserSnapshot? _cachedSnapshot;

    public async Task<CurrentUserSnapshot?> GetAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal.FindFirst(AppClaimTypes.UserId)?.Value;
        var sessionVersionValue = principal.FindFirst(AppClaimTypes.SessionVersion)?.Value;
        if (principal.Identity?.IsAuthenticated != true
            || !int.TryParse(userIdValue, out var userId)
            || !long.TryParse(sessionVersionValue, out var tokenSessionVersion))
        {
            return null;
        }

        var snapshot = await GetByUserIdAsync(userId, cancellationToken);
        return snapshot?.Account.SessionVersion == tokenSessionVersion ? snapshot : null;
    }

    public async Task<CurrentUserSnapshot?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (_cachedUserId == userId)
        {
            return _cachedSnapshot;
        }

        _cachedUserId = userId;
        var now = DateTimeOffset.UtcNow;
        var canonicalGroupIds = CanonicalRbac.Personas
            .Select(persona => persona.GroupId)
            .ToArray();
        var rows = await (
                from account in _context.Users.AsNoTracking()
                join membership in _context.P04_UserGroups.AsNoTracking()
                    on account.Id equals membership.AccountId
                join roleGroup in _context.P02_Groups.AsNoTracking()
                    on membership.P02_GroupId equals roleGroup.Id
                join department in _context.LEX02_CompanyDepartmentLocations.AsNoTracking()
                    on membership.LEX02_CompanyDepartmentLocationId equals department.Id
                where account.Id == userId
                      && account.AccountStatus == AppAccountStatus.Active
                      && account.MemberCompanyCode == CanonicalRbac.DefaultMemberCompanyCode
                      && (account.LockoutEnd == null || account.LockoutEnd <= now)
                      && !membership.IsDeleted
                      && membership.UserId == account.Id
                      && !roleGroup.IsDeleted
                      && roleGroup.ParentGroupId == null
                      && canonicalGroupIds.Contains(roleGroup.Id)
                      && !department.IsDeleted
                      && department.Id != Guid.Empty
                      && department.LEX02Type == "PhongBan"
                select new { account, membership, roleGroup, department })
            .Take(2)
            .ToListAsync(cancellationToken);

        var row = rows.Count == 1 ? rows[0] : null;
        var isCanonical = row is not null && CanonicalRbac.Personas.Any(persona =>
            persona.GroupId == row.roleGroup.Id
            && string.Equals(persona.GroupCode, row.roleGroup.GroupCode, StringComparison.Ordinal));
        _cachedSnapshot = row is not null && isCanonical
            ? new CurrentUserSnapshot(row.account, row.membership, row.roleGroup, row.department)
            : null;
        return _cachedSnapshot;
    }

    public void EnrichPrincipal(ClaimsPrincipal principal, CurrentUserSnapshot snapshot)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        ReplaceClaim(identity, AppClaimTypes.GroupId, snapshot.Group.Id.ToString());
        ReplaceClaim(identity, AppClaimTypes.GroupCode, snapshot.Group.GroupCode);
        ReplaceClaim(identity, AppClaimTypes.MemberCompanyCode, snapshot.Account.MemberCompanyCode.ToString());
        ReplaceClaim(identity, AppClaimTypes.DepartmentCode, snapshot.PrimaryDepartment.LEX02Code ?? string.Empty);
        ReplaceClaim(
            identity,
            AppClaimTypes.IsAdmin,
            string.Equals(
                snapshot.Group.GroupCode,
                CanonicalRbac.SystemAdmin.GroupCode,
                StringComparison.Ordinal).ToString());
    }

    private static void ReplaceClaim(ClaimsIdentity identity, string type, string value)
    {
        foreach (var claim in identity.FindAll(type).ToArray())
        {
            identity.TryRemoveClaim(claim);
        }

        identity.AddClaim(new Claim(type, value));
    }
}
