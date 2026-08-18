using System.Globalization;
using gtas_vpp_be.Model;
using gtas_vpp_be.Model.VPP;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public static partial class DemoPersonaScenarioSeeder
{
    private static async Task<VppRequest> UpsertRegularRequestAsync(
        VPPMigrationDbContext context,
        PersonaSeed persona,
        VppPeriod period,
        IReadOnlyList<ProductSeed> products,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var existing = await context.Requests
            .Include(request => request.RequestDetails)
            .SingleOrDefaultAsync(request =>
                request.CreatedByUserId == persona.Account.User.Id
                && request.PeriodId == period.Id
                && request.IsCurrentRevision
                && !request.IsAdditionalOrder
                && !request.IsDeleted,
                cancellationToken);
        var ownedId = StableGuid(
            $"persona-demo-request|regular|{persona.Account.User.Id}|{period.Year:D4}{period.Month:D2}");
        if (existing is not null && existing.Id != ownedId)
        {
            return existing;
        }

        var submittedAtUtc = period.StartAtUtc.AddDays(2).AddHours(2 + persona.ProductOffset);
        if (submittedAtUtc > nowUtc)
        {
            submittedAtUtc = nowUtc.AddMinutes(-(30 + persona.ProductOffset));
        }

        var request = existing;
        if (request is null)
        {
            request = new VppRequest
            {
                Id = ownedId,
                RequestSeriesId = StableGuid(
                    $"persona-demo-series|regular|{persona.Account.User.Id}|{period.Year:D4}{period.Month:D2}"),
                RevisionNumber = 1,
                IsCurrentRevision = true,
                IsAdditionalOrder = false,
                CreatedByUserId = persona.Account.User.Id,
                CreatedAtUtc = submittedAtUtc
            };
            context.Requests.Add(request);
        }

        request.VppCode = $"VPP-{period.Year:D4}{period.Month:D2}-{persona.Code}-01";
        request.Year = period.Year;
        request.Month = period.Month;
        request.PeriodId = period.Id;
        request.Status = (int)VPPStatus.Submitted;
        request.DepartmentCode = persona.Account.DepartmentCode;
        request.MemberCompanyCode = persona.Account.User.MemberCompanyCode.ToString(CultureInfo.InvariantCulture);
        request.SubmittedDate = submittedAtUtc;
        request.SupersedesRequestId = null;
        request.SupersededByRequestId = null;
        request.BaseRequestId = null;
        request.BaseRequestSeriesId = null;
        request.SupplementSequence = null;
        request.SupplementAttemptNumber = null;
        request.SupplementReason = null;
        request.ApprovedById = null;
        request.ApprovedAt = null;
        request.RejectedById = null;
        request.RejectedAt = null;
        request.RejectReason = null;
        request.CancelledById = null;
        request.CancelledAt = null;
        request.CancelReason = null;
        request.IdempotencyKey = $"{IdempotencyPrefix}-reg-{persona.Account.User.Id}-{period.Year:D4}{period.Month:D2}";
        request.CommandPayloadHash = Sha256Hex(request.IdempotencyKey);
        request.Description = $"Đơn thường demo của {persona.Account.User.FullName}.";
        request.UpdatedByUserId = persona.Account.User.Id;
        request.UpdatedAtUtc = submittedAtUtc;
        request.IsDeleted = false;

        var lineCount = 2 + Math.Abs(period.Month + persona.ProductOffset) % 3;
        await ReconcileDetailsAsync(
            context,
            request,
            products,
            lineCount,
            persona.ProductOffset + period.Month,
            submittedAtUtc,
            persona.Account.User.Id,
            cancellationToken);
        await UpsertLogAsync(
            context,
            request,
            "SUBMITTED",
            "Đã gửi đơn văn phòng phẩm",
            request.CreatedByUserId,
            submittedAtUtc,
            null,
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return request;
    }

    private static async Task UpsertSupplementAsync(
        VPPMigrationDbContext context,
        PersonaSeed persona,
        VppRequest baseRequest,
        VPPStatus status,
        string reason,
        IReadOnlyList<ProductSeed> products,
        CancellationToken cancellationToken)
    {
        if (baseRequest.PeriodId is null)
        {
            throw new InvalidOperationException("Demo supplement requires a persisted period.");
        }

        var scenario = status.ToString().ToLowerInvariant();
        var requestId = StableGuid(
            $"persona-demo-request|supplement|{scenario}|{persona.Account.User.Id}|{baseRequest.Year:D4}{baseRequest.Month:D2}");
        var request = await context.Requests
            .Include(candidate => candidate.RequestDetails)
            .SingleOrDefaultAsync(candidate => candidate.Id == requestId, cancellationToken);
        var period = await context.Periods.SingleAsync(
            candidate => candidate.Id == baseRequest.PeriodId.Value,
            cancellationToken);
        var submittedAtUtc = period.SubmissionDeadlineUtc.AddHours(1);
        var resolvedAtUtc = submittedAtUtc.AddHours(4);
        if (resolvedAtUtc > period.SupplementApprovalDeadlineUtc)
        {
            throw new InvalidOperationException(
                $"Demo supplement window is invalid for period {period.Month:00}/{period.Year}.");
        }
        if (request is null)
        {
            request = new VppRequest
            {
                Id = requestId,
                RequestSeriesId = StableGuid(
                    $"persona-demo-series|supplement|{scenario}|{persona.Account.User.Id}|{baseRequest.Year:D4}{baseRequest.Month:D2}"),
                RevisionNumber = 1,
                IsCurrentRevision = true,
                IsAdditionalOrder = true,
                CreatedByUserId = persona.Account.User.Id,
                CreatedAtUtc = submittedAtUtc
            };
            context.Requests.Add(request);
        }

        request.VppCode = $"VPP-{baseRequest.Year:D4}{baseRequest.Month:D2}-{persona.Code}-BS01";
        request.Year = baseRequest.Year;
        request.Month = baseRequest.Month;
        request.PeriodId = baseRequest.PeriodId;
        request.BaseRequestId = baseRequest.Id;
        request.BaseRequestSeriesId = baseRequest.RequestSeriesId;
        request.SupplementSequence = 1;
        request.SupplementAttemptNumber = 1;
        request.SupplementReason = reason;
        request.Status = (int)status;
        request.DepartmentCode = persona.Account.DepartmentCode;
        request.MemberCompanyCode = persona.Account.User.MemberCompanyCode.ToString(CultureInfo.InvariantCulture);
        request.SubmittedDate = submittedAtUtc;
        request.ApprovedById = status == VPPStatus.Approved ? persona.WorkflowActorUserId : null;
        request.ApprovedAt = status == VPPStatus.Approved ? resolvedAtUtc : null;
        request.RejectedById = status == VPPStatus.Rejected ? persona.WorkflowActorUserId : null;
        request.RejectedAt = status == VPPStatus.Rejected ? resolvedAtUtc : null;
        request.RejectReason = status == VPPStatus.Rejected ? reason : null;
        request.CancelledById = status == VPPStatus.Cancelled ? persona.Account.User.Id : null;
        request.CancelledAt = status == VPPStatus.Cancelled ? resolvedAtUtc : null;
        request.CancelReason = status == VPPStatus.Cancelled ? reason : null;
        request.IdempotencyKey =
            $"{IdempotencyPrefix}-sup-{scenario}-{persona.Account.User.Id}-{baseRequest.Year:D4}{baseRequest.Month:D2}";
        request.CommandPayloadHash = Sha256Hex(request.IdempotencyKey);
        request.Description = reason;
        request.UpdatedByUserId = status == VPPStatus.Cancelled
            ? persona.Account.User.Id
            : persona.WorkflowActorUserId;
        request.UpdatedAtUtc = resolvedAtUtc;
        request.IsDeleted = false;

        await ReconcileDetailsAsync(
            context,
            request,
            products,
            2,
            persona.ProductOffset + baseRequest.Month + 3,
            submittedAtUtc,
            persona.Account.User.Id,
            cancellationToken);
        await UpsertLogAsync(
            context,
            request,
            "SUBMITTED",
            "Đã gửi đơn bổ sung",
            request.CreatedByUserId,
            submittedAtUtc,
            reason,
            cancellationToken);

        var (action, title, actor) = status switch
        {
            VPPStatus.Approved => ("APPROVED", "Đã duyệt đơn bổ sung", persona.WorkflowActorUserId),
            VPPStatus.Rejected => ("REJECTED", "Đã từ chối đơn bổ sung", persona.WorkflowActorUserId),
            VPPStatus.Cancelled => ("CANCELLED", "Đã hủy đơn bổ sung", persona.Account.User.Id),
            _ => throw new InvalidOperationException($"Unsupported demo supplement status {status}.")
        };
        await UpsertLogAsync(
            context,
            request,
            action,
            title,
            actor,
            resolvedAtUtc,
            reason,
            cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task ReconcileDetailsAsync(
        VPPMigrationDbContext context,
        VppRequest request,
        IReadOnlyList<ProductSeed> products,
        int lineCount,
        int productOffset,
        DateTime timestamp,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var desiredIds = new HashSet<Guid>();
        for (var line = 0; line < lineCount; line++)
        {
            var product = products[(productOffset + line) % products.Count];
            var detailId = StableGuid($"persona-demo-detail|{request.Id:D}|{line + 1}");
            desiredIds.Add(detailId);
            var detail = request.RequestDetails.FirstOrDefault(candidate => candidate.Id == detailId);
            if (detail is null)
            {
                detail = new VppRequestDetail
                {
                    Id = detailId,
                    RequestId = request.Id,
                    CreatedByUserId = request.CreatedByUserId,
                    CreatedAtUtc = timestamp
                };
                request.RequestDetails.Add(detail);
            }

            detail.VppId = product.ItemId;
            detail.Qty = line + 1 + (Math.Abs(productOffset) % 3);
            detail.CurrentSinglePrice = product.Price;
            detail.Description = null;
            detail.UpdatedByUserId = actorUserId;
            detail.UpdatedAtUtc = timestamp;
            detail.IsDeleted = false;
        }

        foreach (var obsolete in request.RequestDetails.Where(detail =>
                     !detail.IsDeleted && !desiredIds.Contains(detail.Id)))
        {
            obsolete.IsDeleted = true;
            obsolete.UpdatedByUserId = actorUserId;
            obsolete.UpdatedAtUtc = timestamp;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertLogAsync(
        VPPMigrationDbContext context,
        VppRequest request,
        string action,
        string title,
        int actorUserId,
        DateTime occurredAtUtc,
        string? reason,
        CancellationToken cancellationToken)
    {
        var id = StableGuid($"persona-demo-log|{request.Id:D}|{action}");
        var log = await context.RequestLogs.SingleOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);
        if (log is null)
        {
            log = new RequestLog { Id = id, RequestId = request.Id };
            context.RequestLogs.Add(log);
        }

        log.LogDate = occurredAtUtc;
        log.LogTitle = title;
        log.ActorUserId = actorUserId;
        log.MemberCompanyCode = request.MemberCompanyCode;
        log.Action = action;
        log.RevisionNumber = request.RevisionNumber;
        log.CorrelationId = $"persona-demo-{request.Id:N}";
        log.Reason = reason;
        log.LogJS = null;
    }
}
