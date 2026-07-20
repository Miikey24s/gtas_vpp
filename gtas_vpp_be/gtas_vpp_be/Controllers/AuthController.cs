using gtas_vpp_be.Authorization;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        CancellationToken cancellationToken)
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
        return result is null
            ? Unauthorized(new { message = "Tên đăng nhập hoặc mật khẩu không hợp lệ." })
            : Ok(result);
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
        return revoked ? NoContent() : Unauthorized();
    }

    [HttpGet("me/permissions")]
    [ProducesResponseType<PermissionSnapshotResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPermissions(CancellationToken cancellationToken)
    {
        var snapshot = await _permissionService.GetSnapshotAsync(User, cancellationToken);
        return Ok(snapshot);
    }
}
