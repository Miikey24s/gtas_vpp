using gtas_vpp_be.Authorization;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using gtas_vpp_shared.DTOs.Res.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(IAccountLifecycleService lifecycleService) : ControllerBase
{
    private readonly IAccountLifecycleService _lifecycleService = lifecycleService;

    [AllowAnonymous]
    [EnableRateLimiting("account-register")]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] AccountRegistrationReqDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.RegisterAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("account-confirm")]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(
        [FromQuery] int userId,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.ConfirmEmailAsync(
            new EmailConfirmationReqDTO { UserId = userId, Token = token },
            cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery")]
    [HttpPost("password/recovery")]
    public async Task<IActionResult> RequestPasswordRecovery(
        [FromBody] PasswordRecoveryReqDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.RequestPasswordResetAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [EnableRateLimiting("account-recovery")]
    [HttpPost("password/reset")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] PasswordResetReqDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.ResetPasswordAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [EnableRateLimiting("account-password")]
    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] PasswordChangeReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _lifecycleService.ChangePasswordAsync(
            userId,
            request,
            cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = Permissions.PermissionManage)]
    [HttpPost("admin/activate")]
    [ProducesResponseType(typeof(MembershipAdministrationResDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(
        [FromBody] AdminAccountActivationReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
        {
            return Unauthorized();
        }

        var result = await _lifecycleService.ActivateAsync(
            actorId,
            request,
            cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = Permissions.PermissionManage)]
    [EnableRateLimiting("account-password")]
    [HttpPost("admin/reset-password")]
    [ProducesResponseType(typeof(AccountLifecycleResDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminResetPassword(
        [FromBody] AdminPasswordResetReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var actorId))
        {
            return Unauthorized();
        }

        var result = await _lifecycleService.AdminResetPasswordAsync(
            actorId,
            request,
            cancellationToken);
        return ToActionResult(result);
    }

    private bool TryGetCurrentUserId(out int userId) =>
        int.TryParse(User.FindFirst("UserID")?.Value, out userId) && userId > 0;

    private IActionResult ToActionResult(AccountLifecycleResult result) =>
        result.StatusCode switch
        {
            >= 200 and < 300 => result.Account is not null
                ? StatusCode(result.StatusCode, result.Account)
                : StatusCode(result.StatusCode, new { code = result.Code, message = result.Message }),
            StatusCodes.Status401Unauthorized => Unauthorized(new { code = result.Code, message = result.Message }),
            StatusCodes.Status404NotFound => NotFound(new { code = result.Code, message = result.Message }),
            StatusCodes.Status409Conflict => Conflict(new { code = result.Code, message = result.Message }),
            _ => BadRequest(new { code = result.Code, message = result.Message })
        };

    private IActionResult ToActionResult(MembershipAdministrationResult result) =>
        result.Succeeded
            ? Ok(result.Membership)
            : StatusCode(result.StatusCode, new { code = result.Code, message = result.Message });
}
