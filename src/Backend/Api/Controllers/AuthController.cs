using gtas_vpp_be.Authorization;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AuthController(
    IAppAuthenticationService authenticationService,
    IPermissionService permissionService) : ControllerBase
{
    private readonly IAppAuthenticationService _authenticationService = authenticationService;
    private readonly IPermissionService _permissionService = permissionService;

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType<AuthenticationResultDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] AuthenticationLoginRequest request,
        CancellationToken cancellationToken,
        [FromQuery] bool useCookies = false)
    {
        if (string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Tên đăng nhập và mật khẩu là bắt buộc." });
        }

        var result = await _authenticationService.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không hợp lệ." });
        }

        if (useCookies)
        {
            var principal = CreateCookiePrincipal(result);
            await HttpContext.SignInAsync(
                AppAuthenticationSchemes.Cookie,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = false,
                    ExpiresUtc = result.AccessTokenExpiresAtUtc
                });
            result.AccessToken = null;
        }

        return Ok(result);
    }

    [HttpGet("antiforgery")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetAntiforgeryToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(
            AppAuthenticationSchemes.AntiforgeryCookieName,
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                SameSite = SameSiteMode.Strict,
                Secure = Request.IsHttps,
                Path = "/"
            });
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var currentUser = await _authenticationService.GetCurrentUserAsync(User, cancellationToken);
        return currentUser is null
            ? Unauthorized(new { message = "Phiên đăng nhập không còn hợp lệ." })
            : Ok(currentUser);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var revoked = await _authenticationService.RevokeCurrentSessionAsync(User, cancellationToken);
        if (!revoked)
        {
            return Unauthorized();
        }

        await HttpContext.SignOutAsync(AppAuthenticationSchemes.Cookie);
        Response.Cookies.Delete(
            AppAuthenticationSchemes.AntiforgeryCookieName,
            new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpGet("me/permissions")]
    [ProducesResponseType<PermissionSnapshotResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var snapshot = await _permissionService.GetSnapshotAsync(User, cancellationToken);
        return Ok(snapshot);
    }

    private static ClaimsPrincipal CreateCookiePrincipal(AuthenticationResultDTO result)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserID.ToString()),
            new Claim(AppClaimTypes.UserId, result.UserID.ToString()),
            new Claim(AppClaimTypes.UserLogin, result.UserLogin ?? string.Empty),
            new Claim(AppClaimTypes.SessionVersion, result.SessionVersion.ToString())
        };
        return new ClaimsPrincipal(
            new ClaimsIdentity(claims, AppAuthenticationSchemes.Cookie));
    }
}
