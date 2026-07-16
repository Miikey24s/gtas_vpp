using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics;
using System.Data;
using System.Security.Claims;
using System.Text;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gtas_vpp_be.Authorization;

public interface IAppAuthenticationService
{
    Task<sp_Authentication_Login?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<CurrentUserResDTO?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeCurrentSessionAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}

public sealed class AppAuthenticationService(
    UserManager<AppUser> userManager,
    ICurrentUserContext currentUserContext,
    VPPContext context,
    JwtDeploymentSettings jwtSettings,
    ILogger<AppAuthenticationService> logger) : IAppAuthenticationService
{
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;
    private readonly VPPContext _context = context;
    private readonly JwtDeploymentSettings _jwtSettings = jwtSettings;
    private readonly ILogger<AppAuthenticationService> _logger = logger;

    public async Task<sp_Authentication_Login?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedInput = username.Trim();
        var account = await _userManager.FindByNameAsync(normalizedInput);
        if (account is null
            || account.AccountStatus != AppAccountStatus.Active
            || await _userManager.IsLockedOutAsync(account))
        {
            _logger.LogWarning("Authentication rejected for an unknown or unavailable account.");
            return null;
        }

        if (!await _userManager.CheckPasswordAsync(account, password))
        {
            await _userManager.AccessFailedAsync(account);
            _logger.LogWarning("Authentication rejected for AccountId={AccountId}.", account.Id);
            return null;
        }

        await _userManager.ResetAccessFailedCountAsync(account);
        var snapshot = await _currentUserContext.GetByUserIdAsync(account.Id, cancellationToken);
        if (snapshot is null)
        {
            _logger.LogWarning(
                "Authentication rejected because AccountId={AccountId} has no valid active membership.",
                account.Id);
            return null;
        }

        account.LastLoginAtUtc = DateTime.UtcNow;
        account.UpdatedAtUtc = account.LastLoginAtUtc.Value;
        var updateResult = await _userManager.UpdateAsync(account);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException("The account login timestamp could not be updated.");
        }

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);
        var token = GenerateAccessToken(account, expiresAtUtc);
        _logger.LogInformation("Authentication succeeded for AccountId={AccountId}.", account.Id);

        return new sp_Authentication_Login
        {
            UserID = account.Id,
            UserLogin = account.UserName,
            FullName = account.FullName,
            Email = account.Email,
            IsAdmin = string.Equals(
                snapshot.Group.GroupCode,
                CanonicalRbac.SystemAdmin.GroupCode,
                StringComparison.Ordinal),
            GroupId = snapshot.Group.Id,
            GroupName = snapshot.Group.GroupName,
            MemberCompanyCode = account.MemberCompanyCode.ToString(),
            MemberCompanyName = account.MemberCompanyCode.ToString(),
            DepartmentCode = snapshot.PrimaryDepartment.LEX02Code,
            DepartmentName = snapshot.PrimaryDepartment.LEX02Name,
            AccessToken = token,
            AccessTokenExpiresAtUtc = expiresAtUtc,
            SessionVersion = account.SessionVersion,
            AccountStatus = account.AccountStatus.ToString(),
            MustChangePassword = account.MustChangePassword
        };
    }

    public async Task<CurrentUserResDTO?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _currentUserContext.GetAsync(principal, cancellationToken);
        return snapshot is null ? null : MapCurrentUser(snapshot);
    }

    public async Task<bool> RevokeCurrentSessionAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _currentUserContext.GetAsync(principal, cancellationToken);
        if (snapshot is null)
        {
            return false;
        }

        var account = await _userManager.FindByIdAsync(snapshot.Account.Id.ToString());
        if (account is null)
        {
            return false;
        }

        IDbContextTransaction? transaction = null;
        try
        {
            if (_context.Database.IsRelational())
            {
                transaction = await _context.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted,
                    cancellationToken);
            }

            account.SessionVersion++;
            account.SecurityStamp = Guid.NewGuid().ToString("N");
            account.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            account.UpdatedAtUtc = DateTime.UtcNow;
            _context.A01_SecurityAudits.Add(new A01_SecurityAudit
            {
                ActorUserId = account.Id,
                TargetUserId = account.Id,
                Action = "SESSION_REVOKED",
                ResourceType = "AppUser",
                ResourceId = account.Id.ToString(),
                Outcome = "Succeeded",
                Summary = "Current account sessions were revoked.",
                CorrelationId = Activity.Current?.TraceId.ToString(),
                OccurredAtUtc = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private string GenerateAccessToken(AppUser account, DateTime expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(AppClaimTypes.UserId, account.Id.ToString()),
            new(AppClaimTypes.UserLogin, account.UserName ?? string.Empty),
            new(AppClaimTypes.SessionVersion, account.SessionVersion.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static CurrentUserResDTO MapCurrentUser(CurrentUserSnapshot snapshot) => new()
    {
        UserId = snapshot.Account.Id,
        UserLogin = snapshot.Account.UserName ?? string.Empty,
        FullName = snapshot.Account.FullName,
        Email = snapshot.Account.Email,
        AccountStatus = snapshot.Account.AccountStatus.ToString(),
        MustChangePassword = snapshot.Account.MustChangePassword,
        SessionVersion = snapshot.Account.SessionVersion,
        GroupId = snapshot.Group.Id,
        GroupCode = snapshot.Group.GroupCode,
        GroupName = snapshot.Group.GroupName ?? string.Empty,
        MemberCompanyCode = snapshot.Account.MemberCompanyCode,
        PrimaryDepartmentId = snapshot.PrimaryDepartment.Id,
        DepartmentCode = snapshot.PrimaryDepartment.LEX02Code ?? string.Empty,
        DepartmentName = snapshot.PrimaryDepartment.LEX02Name ?? string.Empty
    };
}
