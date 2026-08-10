using System.Security.Claims;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gtas_vpp_be.Controllers;

[ApiController]
[Authorize(Policy = Permissions.PeriodSettle)]
[Route("api/post-settlement-order-corrections")]
public sealed class PostSettlementOrderCorrectionsController(
    IPostSettlementOrderCorrectionService corrections,
    IAppNotificationService notifications) : ControllerBase
{
    private readonly IPostSettlementOrderCorrectionService _corrections = corrections;
    private readonly IAppNotificationService _notifications = notifications;

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue("UserID"), out var id) ? id : null;

    private string CurrentMemberCompanyCode =>
        User.FindFirstValue("MemberCompanyCode") ?? string.Empty;

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PostSettlementOrderCorrectionResDTO>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? periodId,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        return Ok(await _corrections.ListAsync(
            CurrentMemberCompanyCode, periodId, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<PostSettlementOrderCorrectionResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(
        [FromBody] PostSettlementOrderCorrectionCreateReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        var result = await _corrections.CreateAsync(
            CurrentMemberCompanyCode, CurrentUserId!.Value, request, cancellationToken);
        await TryNotifyManagersAsync(result, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType<PostSettlementOrderCorrectionResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        var result = await _corrections.ConfirmAsync(
            id, CurrentUserId!.Value, request, cancellationToken);
        await TryNotifyEmployeeAsync(result, confirmed: true, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<PostSettlementOrderCorrectionResDTO>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken)
    {
        if (!HasRequiredClaims()) return Forbid();
        var result = await _corrections.RejectAsync(
            id, CurrentUserId!.Value, request, cancellationToken);
        await TryNotifyEmployeeAsync(result, confirmed: false, cancellationToken);
        return Ok(result);
    }

    private bool HasRequiredClaims()
        => CurrentUserId.HasValue && !string.IsNullOrWhiteSpace(CurrentMemberCompanyCode);

    private async Task TryNotifyManagersAsync(
        PostSettlementOrderCorrectionResDTO result,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipients = await _notifications.GetRecipientsWithPermissionAsync(
                result.MemberCompanyCode, Permissions.PeriodSettle, cancellationToken);
            await _notifications.PublishAsync(
                recipients.Where(userId => userId != CurrentUserId),
                result.MemberCompanyCode,
                "period.order-correction.pending",
                "Yêu cầu sửa đơn sau chốt",
                $"Đơn {result.RequestCode} có yêu cầu {result.Action.ToLowerInvariant()} đang chờ một quản lý khác xác nhận.",
                $"/dashboard?tab=5&periodTab=periods&periodId={result.PeriodId}",
                result.Id.ToString("N"),
                cancellationToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not publish post-settlement correction notification for {CorrectionId}", result.Id);
        }
    }

    private async Task TryNotifyEmployeeAsync(
        PostSettlementOrderCorrectionResDTO result,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            var decisionText = confirmed ? "đã được xác nhận" : "đã bị từ chối";
            var note = confirmed ? result.EmployeeNote : result.DecisionReason;
            await _notifications.PublishAsync(
                [result.RequestOwnerUserId],
                result.MemberCompanyCode,
                confirmed ? "period.order-correction.confirmed" : "period.order-correction.rejected",
                confirmed ? "Đơn sau chốt đã được điều chỉnh" : "Yêu cầu điều chỉnh đơn bị từ chối",
                $"Yêu cầu {result.Action.ToLowerInvariant()} cho đơn {result.RequestCode} {decisionText}. {note}",
                $"/dashboard?tab=1&orderId={result.ResultRequestId ?? result.RequestId}",
                $"{result.Id:N}:{result.Status}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Could not publish post-settlement decision notification for {CorrectionId}", result.Id);
        }
    }
}
