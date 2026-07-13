using System.Security.Claims;
using gtas_vpp_be.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IAppNotificationService notificationService) : ControllerBase
{
    private readonly IAppNotificationService _notificationService = notificationService;

    [HttpGet]
    public async Task<IActionResult> GetInbox(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(out var userId, out var companyCode)) return Unauthorized();

        return Ok(await _notificationService.GetInboxAsync(
            userId, companyCode, skip, take, unreadOnly, cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(out var userId, out var companyCode)) return Unauthorized();

        return await _notificationService.MarkReadAsync(
            id, userId, companyCode, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken = default)
    {
        if (!TryGetScope(out var userId, out var companyCode)) return Unauthorized();

        var changed = await _notificationService.MarkAllReadAsync(
            userId, companyCode, cancellationToken);
        return Ok(new { Changed = changed });
    }

    private bool TryGetScope(out int userId, out string companyCode)
    {
        companyCode = User.FindFirstValue("MemberCompanyCode") ?? string.Empty;
        return int.TryParse(User.FindFirstValue("UserID"), out userId)
            && !string.IsNullOrWhiteSpace(companyCode);
    }
}
