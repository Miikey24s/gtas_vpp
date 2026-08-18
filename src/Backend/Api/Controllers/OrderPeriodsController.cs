using System.Security.Claims;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize]
[Route("api/order-periods")]
public sealed class OrderPeriodsController(IVppPeriodService periods) : ControllerBase
{
    private readonly IVppPeriodService _periods = periods;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

    private string CurrentMemberCompanyCode =>
        User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

    [HttpGet]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<IReadOnlyList<VppManagedPeriodResDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.ListManagedAsync(
            CurrentMemberCompanyCode,
            cancellationToken));
    }

    [HttpGet("settings")]
    [Authorize(Policy = Permissions.PeriodSettingsManage)]
    [ProducesResponseType<VppOrderPeriodSettingsResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.GetEffectiveSettingsAsync(
            CurrentMemberCompanyCode,
            cancellationToken));
    }

    [HttpGet("settings/current")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppOrderPeriodSettingsResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentSettings(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.GetSettingsAsync(
            CurrentMemberCompanyCode,
            cancellationToken));
    }

    [HttpGet("settings/history")]
    [Authorize(Policy = Permissions.PeriodSettingsManage)]
    [ProducesResponseType<IReadOnlyList<VppOrderPeriodSettingsResDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSettingsHistory(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.ListSettingsVersionsAsync(
            CurrentMemberCompanyCode,
            cancellationToken));
    }

    [HttpPost("settings")]
    [Authorize(Policy = Permissions.PeriodSettingsManage)]
    [ProducesResponseType<VppOrderPeriodSettingsResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveSettings(
        [FromBody] VppOrderPeriodSettingsReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.SaveSettingsAsync(
            CurrentMemberCompanyCode,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("horizon-preview")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppPeriodHorizonPreviewResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewHorizon(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.PreviewHorizonAsync(
            CurrentMemberCompanyCode,
            cancellationToken));
    }

    [HttpPost("top-up")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<IReadOnlyList<VppManagedPeriodResDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TopUp(CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.TopUpOpenHorizonAsync(
            CurrentMemberCompanyCode,
            CurrentUserId!.Value,
            cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateManual(
        [FromBody] VppOrderPeriodManualCreateReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.CreateManualAsync(
            CurrentMemberCompanyCode,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSchedule(
        Guid id,
        [FromBody] VppOrderPeriodUpdateReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.UpdateScheduleAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("{id:guid}/extend-deadline")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExtendDeadline(
        Guid id,
        [FromBody] VppOrderPeriodExtendDeadlineReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.ExtendDeadlineAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("{id:guid}/close-submissions")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CloseSubmissions(
        Guid id,
        [FromBody] VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.CloseSubmissionsAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("{id:guid}/reopen-submissions")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReopenSubmissions(
        Guid id,
        [FromBody] VppOrderPeriodReopenSubmissionsReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.ReopenSubmissionsAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromBody] VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        await _periods.DeleteAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken);
        return Ok();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    [ProducesResponseType<VppManagedPeriodResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromBody] VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _periods.RestoreAsync(
            id,
            CurrentUserId!.Value,
            request,
            cancellationToken));
    }

    [HttpPost("{id:guid}/hard-delete")]
    [Authorize(Policy = Permissions.PeriodSettle)]
    public async Task<IActionResult> HardDelete(
        Guid id,
        [FromBody] VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        await _periods.HardDeleteAsync(id, request, cancellationToken);
        return Ok();
    }

    private bool HasRequiredClaims() =>
        CurrentUserId.HasValue && !string.IsNullOrWhiteSpace(CurrentMemberCompanyCode);
}
