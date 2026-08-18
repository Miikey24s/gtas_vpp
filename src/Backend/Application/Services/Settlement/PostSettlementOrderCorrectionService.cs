using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public interface IPostSettlementOrderCorrectionService
{
    Task<PostSettlementOrderCorrectionResDTO> CreateAsync(
        string memberCompanyCode,
        int actorUserId,
        PostSettlementOrderCorrectionCreateReqDTO request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PostSettlementOrderCorrectionResDTO>> ListAsync(
        string memberCompanyCode,
        Guid? periodId,
        CancellationToken cancellationToken = default);

    Task<PostSettlementOrderCorrectionResDTO> ConfirmAsync(
        Guid id,
        int actorUserId,
        PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken = default);

    Task<PostSettlementOrderCorrectionResDTO> RejectAsync(
        Guid id,
        int actorUserId,
        PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken = default);
}

public sealed class PostSettlementOrderCorrectionService(
    IUnitOfWork scopedUow,
    IDateTimeProvider dateTimeProvider,
    IOrderQuantityLimitService quantityLimits) : IPostSettlementOrderCorrectionService
{
    private readonly IUnitOfWork _scopedUow = scopedUow;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IOrderQuantityLimitService _quantityLimits = quantityLimits;

    public async Task<PostSettlementOrderCorrectionResDTO> CreateAsync(
        string memberCompanyCode,
        int actorUserId,
        PostSettlementOrderCorrectionCreateReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompany(memberCompanyCode);
        var action = ParseAction(request.Action);
        var reason = ValidateRequiredText(request.Reason, "Lý do", 5, 500);
        var employeeNote = ValidateRequiredText(request.EmployeeNote, "Ghi chú cho nhân viên", 5, 500);

        var header = await _scopedUow.VPPContext.Set<VppRequest>()
            .Include(x => x.RequestDetails.Where(item => !item.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == request.RequestId
                && x.MemberCompanyCode == company
                && !x.IsDeleted
                && x.IsCurrentRevision, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy revision đơn hiện tại.");
        EnsureRowVersion(header.RowVersion, request.RequestRowVersion, "Đơn đã thay đổi. Hãy tải lại trước khi yêu cầu điều chỉnh.");

        var period = await _scopedUow.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == header.PeriodId
                && x.MemberCompanyCode == company
                && !x.IsDeleted, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy kỳ của đơn.");
        if (period.State != VppPeriodState.Settled)
            throw new ConflictException("Chỉ dùng quy trình này cho kỳ đã chốt.");
        var nowUtc = NormalizeUtc(_dateTimeProvider.Now);
        EnsureAdjustmentWindowOpen(period, nowUtc);

        var settlement = await _scopedUow.VPPContext.Set<Settlement>()
            .AsNoTracking()
            .Include(x => x.Items.Where(item => !item.IsDeleted))
            .FirstOrDefaultAsync(x => x.PeriodId == period.Id
                && x.MemberCompanyCode == company
                && x.IsCurrentRevision
                && !x.IsDeleted, cancellationToken)
            ?? throw new ConflictException("Kỳ chưa có bảng chốt hiện hành.");

        if (await _scopedUow.VPPContext.Set<PostSettlementOrderCorrection>()
            .AnyAsync(x => x.MemberCompanyCode == company
                && x.RequestSeriesId == header.RequestSeriesId
                && (x.Status == PostSettlementOrderCorrectionStatus.Pending
                    || (x.Status == PostSettlementOrderCorrectionStatus.Confirmed
                        && x.ResultSettlementId == null))
                && !x.IsDeleted, cancellationToken))
        {
            throw new ConflictException("Đơn này đã có thay đổi đang chờ duyệt hoặc chờ chốt lại.");
        }

        var normalizedItems = action == PostSettlementOrderCorrectionAction.Cancel
            ? []
            : ValidateItems(request.Items, header.RequestDetails, settlement.Items);
        if (action == PostSettlementOrderCorrectionAction.Adjust)
        {
            await _quantityLimits.ValidateAsync(normalizedItems.Select(item => new VppRequestDetailItemReqDTO
            {
                VppId = item.VppId,
                Qty = item.Qty,
                Description = item.Description
            }), cancellationToken);
        }
        var correction = new PostSettlementOrderCorrection
        {
            Id = Guid.NewGuid(),
            PeriodId = period.Id,
            RequestId = header.Id,
            RequestSeriesId = header.RequestSeriesId,
            RequestRevisionNumber = header.RevisionNumber,
            SettlementId = settlement.Id,
            MemberCompanyCode = company,
            Action = action,
            Status = PostSettlementOrderCorrectionStatus.Pending,
            Reason = reason,
            EmployeeNote = employeeNote,
            RequestedByUserId = actorUserId,
            RequestedAtUtc = nowUtc,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc,
            Items = normalizedItems.Select(item => new PostSettlementOrderCorrectionItem
            {
                Id = Guid.NewGuid(),
                VppId = item.VppId,
                Qty = item.Qty,
                Description = item.Description,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = nowUtc
            }).ToList()
        };

        _scopedUow.VPPContext.Set<PostSettlementOrderCorrection>().Add(correction);
        await _scopedUow.SaveChangesAsync(cancellationToken);
        return Map(correction, header, settlement);
    }

    public async Task<IReadOnlyList<PostSettlementOrderCorrectionResDTO>> ListAsync(
        string memberCompanyCode,
        Guid? periodId,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompany(memberCompanyCode);
        var query = CorrectionQuery().AsNoTracking()
            .Where(x => x.MemberCompanyCode == company && !x.IsDeleted);
        if (periodId.HasValue)
            query = query.Where(x => x.PeriodId == periodId.Value);

        return (await query
                .OrderBy(x => x.Status)
                .ThenByDescending(x => x.RequestedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(x => Map(x, x.Request, x.Settlement))
            .ToArray();
    }

    public async Task<PostSettlementOrderCorrectionResDTO> ConfirmAsync(
        Guid id,
        int actorUserId,
        PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken = default)
    {
        await _scopedUow.BeginTransactionAsync();
        try
        {
            var correction = await CorrectionQuery()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu điều chỉnh.");
            EnsurePendingDecision(correction, actorUserId, request.RowVersion);

            var currentRequest = await _scopedUow.VPPContext.Set<VppRequest>()
                .Include(x => x.RequestDetails.Where(item => !item.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == correction.RequestId
                    && x.IsCurrentRevision && !x.IsDeleted, cancellationToken)
                ?? throw new ConflictException("Revision đơn đã thay đổi; yêu cầu điều chỉnh không còn hợp lệ.");
            if (currentRequest.RevisionNumber != correction.RequestRevisionNumber)
                throw new ConflictException("Revision đơn đã thay đổi; yêu cầu điều chỉnh không còn hợp lệ.");

            var currentSettlement = await _scopedUow.VPPContext.Set<Settlement>()
                .Include(x => x.Items.Where(item => !item.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == correction.SettlementId
                    && x.IsCurrentRevision && !x.IsDeleted, cancellationToken)
                ?? throw new ConflictException("Bảng chốt đã có revision mới; hãy tạo lại yêu cầu điều chỉnh.");
            var period = await _scopedUow.VPPContext.Set<VppPeriod>()
                .FirstAsync(x => x.Id == correction.PeriodId && !x.IsDeleted, cancellationToken);
            if (period.State != VppPeriodState.Settled)
                throw new ConflictException("Kỳ không còn ở trạng thái đã chốt.");

            var nowUtc = NormalizeUtc(_dateTimeProvider.Now);
            EnsureAdjustmentWindowOpen(period, nowUtc);
            var replacement = BuildRequestRevision(correction, currentRequest, currentSettlement, actorUserId, nowUtc);
            currentRequest.IsCurrentRevision = false;
            currentRequest.SupersededByRequestId = replacement.Id;
            currentRequest.UpdatedByUserId = actorUserId;
            currentRequest.UpdatedAtUtc = nowUtc;
            _scopedUow.VPPContext.Set<VppRequest>().Add(replacement);

            correction.Status = PostSettlementOrderCorrectionStatus.Confirmed;
            correction.DecisionReason = NormalizeOptionalText(request.Reason, 500);
            correction.DecidedByUserId = actorUserId;
            correction.DecidedAtUtc = nowUtc;
            correction.ResultRequestId = replacement.Id;
            correction.ResultSettlementId = null;
            correction.UpdatedByUserId = actorUserId;
            correction.UpdatedAtUtc = nowUtc;
            period.LastTransitionUserId = actorUserId;
            period.LastTransitionAtUtc = nowUtc;
            period.LastTransitionReason = correction.Action == PostSettlementOrderCorrectionAction.Cancel
                ? $"Đã duyệt hủy đơn {currentRequest.VppCode}; đang chờ chốt lại kỳ."
                : $"Đã duyệt cập nhật đơn {currentRequest.VppCode}; đang chờ chốt lại kỳ.";
            period.UpdatedByUserId = actorUserId;
            period.UpdatedAtUtc = nowUtc;

            _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
            {
                Id = Guid.NewGuid(),
                RequestId = replacement.Id,
                LogTitle = correction.Action == PostSettlementOrderCorrectionAction.Cancel
                    ? "POST_SETTLEMENT_CANCELLED" : "POST_SETTLEMENT_ADJUSTED",
                Action = correction.Action == PostSettlementOrderCorrectionAction.Cancel
                    ? "POST_SETTLEMENT_CANCEL" : "POST_SETTLEMENT_ADJUST",
                ActorUserId = actorUserId,
                MemberCompanyCode = correction.MemberCompanyCode,
                RevisionNumber = replacement.RevisionNumber,
                CorrelationId = correction.Id.ToString("N"),
                Reason = correction.Reason,
                LogDate = nowUtc,
                LogJS = JsonSerializer.Serialize(new
                {
                    CorrectionId = correction.Id,
                    correction.EmployeeNote,
                    RequestRevisionId = replacement.Id,
                    CurrentSettlementId = currentSettlement.Id,
                    Delta = BuildDelta(currentRequest, correction),
                    WaitingForResettlement = true
                })
            });

            await _scopedUow.CommitAsync();
            return Map(correction, currentRequest, currentSettlement);
        }
        catch
        {
            await _scopedUow.RollbackAsync();
            throw;
        }
    }

    public async Task<PostSettlementOrderCorrectionResDTO> RejectAsync(
        Guid id,
        int actorUserId,
        PostSettlementOrderCorrectionDecisionReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var reason = ValidateRequiredText(request.Reason, "Lý do từ chối", 5, 500);
        var correction = await CorrectionQuery()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu điều chỉnh.");
        EnsurePendingDecision(correction, actorUserId, request.RowVersion);
        var nowUtc = NormalizeUtc(_dateTimeProvider.Now);
        correction.Status = PostSettlementOrderCorrectionStatus.Rejected;
        correction.DecisionReason = reason;
        correction.DecidedByUserId = actorUserId;
        correction.DecidedAtUtc = nowUtc;
        correction.UpdatedByUserId = actorUserId;
        correction.UpdatedAtUtc = nowUtc;
        await _scopedUow.SaveChangesAsync(cancellationToken);
        return Map(correction, correction.Request, correction.Settlement);
    }

    private IQueryable<PostSettlementOrderCorrection> CorrectionQuery()
        => _scopedUow.VPPContext.Set<PostSettlementOrderCorrection>()
            .Include(x => x.Items.Where(item => !item.IsDeleted))
            .Include(x => x.Request)
                .ThenInclude(x => x.RequestDetails.Where(item => !item.IsDeleted))
            .Include(x => x.Settlement);

    private static VppRequest BuildRequestRevision(
        PostSettlementOrderCorrection correction,
        VppRequest source,
        Settlement settlement,
        int actorUserId,
        DateTime nowUtc)
    {
        var cancelled = correction.Action == PostSettlementOrderCorrectionAction.Cancel;
        var replacement = new VppRequest
        {
            Id = Guid.NewGuid(),
            Year = source.Year,
            Month = source.Month,
            PeriodId = source.PeriodId,
            RequestSeriesId = source.RequestSeriesId,
            RevisionNumber = source.RevisionNumber + 1,
            IsCurrentRevision = true,
            SupersedesRequestId = source.Id,
            VppCode = $"VPP-{source.Year:D4}{source.Month:D2}-{Guid.NewGuid():N}",
            Status = cancelled ? (int)VPPStatus.Cancelled : source.Status,
            Description = source.Description,
            DepartmentCode = source.DepartmentCode,
            MemberCompanyCode = source.MemberCompanyCode,
            SubmittedDate = source.SubmittedDate,
            IsAdditionalOrder = source.IsAdditionalOrder,
            BaseRequestId = source.BaseRequestId,
            BaseRequestSeriesId = source.BaseRequestSeriesId,
            SupplementSequence = source.SupplementSequence,
            SupplementAttemptNumber = source.SupplementAttemptNumber,
            SupplementReason = source.SupplementReason,
            ApprovedById = source.ApprovedById,
            ApprovedAt = source.ApprovedAt,
            CancelledById = cancelled ? actorUserId : null,
            CancelledAt = cancelled ? nowUtc : null,
            CancelReason = cancelled ? correction.Reason : null,
            SettledAt = nowUtc,
            SettledByUserId = actorUserId,
            SettledByPriceListId = settlement.PriceListId,
            CreatedByUserId = source.CreatedByUserId,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc,
            IdempotencyKey = $"post-settlement-order-correction:{correction.Id:N}",
            CommandPayloadHash = Hash(new
            {
                correction.Id,
                correction.Action,
                Items = correction.Items.OrderBy(x => x.VppId)
                    .Select(x => new { x.VppId, x.Qty, x.Description })
            })
        };

        if (!cancelled)
        {
            var oldPriceByVpp = source.RequestDetails
                .Where(x => !x.IsDeleted)
                .GroupBy(x => x.VppId)
                .ToDictionary(x => x.Key, x => x.First().CurrentSinglePrice);
            replacement.RequestDetails = correction.Items.Select(item => new VppRequestDetail
            {
                Id = Guid.NewGuid(),
                RequestId = replacement.Id,
                VppId = item.VppId,
                Qty = item.Qty,
                CurrentSinglePrice = oldPriceByVpp.GetValueOrDefault(item.VppId),
                Description = item.Description,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = nowUtc
            }).ToList();
        }

        return replacement;
    }

    private static List<PostSettlementOrderCorrectionItemReqDTO> ValidateItems(
        IReadOnlyCollection<PostSettlementOrderCorrectionItemReqDTO>? items,
        IEnumerable<VppRequestDetail> sourceItems,
        IEnumerable<SettlementItem> settlementItems)
    {
        if (items is null || items.Count == 0)
            throw new BusinessException("Điều chỉnh phải có ít nhất một mặt hàng.");
        if (items.Any(x => x.VppId == Guid.Empty || x.Qty <= 0))
            throw new BusinessException("Mặt hàng và số lượng điều chỉnh không hợp lệ.");
        if (items.GroupBy(x => x.VppId).Any(group => group.Count() > 1))
            throw new BusinessException("Mỗi mặt hàng chỉ được xuất hiện một lần.");
        var sourceQuantities = sourceItems
            .GroupBy(x => x.VppId)
            .ToDictionary(group => group.Key, group => group.First().Qty);
        var sourceIds = sourceQuantities.Keys.ToHashSet();
        if (items.Any(x => !sourceIds.Contains(x.VppId)))
            throw new BusinessException("Không thể thêm mặt hàng mới khi điều chỉnh đơn sau chốt.");
        var settledIds = settlementItems.Select(x => x.VppId).ToHashSet();
        if (items.Any(x => !settledIds.Contains(x.VppId)))
            throw new BusinessException("Mặt hàng không còn trong bản chốt hiện hành. Hãy tải lại trước khi điều chỉnh.");
        var normalized = items.Select(x => new PostSettlementOrderCorrectionItemReqDTO
        {
            VppId = x.VppId,
            Qty = x.Qty,
            Description = NormalizeOptionalText(x.Description, 500)
        }).ToList();
        var hasQuantityChange = normalized.Any(x => sourceQuantities[x.VppId] != x.Qty);
        var hasRemovedItem = normalized.Count != sourceQuantities.Count;
        if (!hasQuantityChange && !hasRemovedItem)
            throw new BusinessException("Đơn chưa có thay đổi nào để gửi duyệt.");
        return normalized;
    }

    private static void EnsurePendingDecision(
        PostSettlementOrderCorrection correction,
        int actorUserId,
        byte[]? expectedRowVersion)
    {
        if (correction.Status != PostSettlementOrderCorrectionStatus.Pending)
            throw new ConflictException("Yêu cầu này đã được xử lý.");
        if (correction.RequestedByUserId == actorUserId)
            throw new ConflictException("Người tạo yêu cầu không được tự xác nhận hoặc từ chối.");
        EnsureRowVersion(correction.RowVersion, expectedRowVersion, "Yêu cầu điều chỉnh đã thay đổi. Hãy tải lại.");
    }

    private static PostSettlementOrderCorrectionResDTO Map(
        PostSettlementOrderCorrection correction,
        VppRequest request,
        Settlement settlement)
    {
        var delta = BuildDelta(request, correction);
        return new PostSettlementOrderCorrectionResDTO
        {
            Id = correction.Id,
            PeriodId = correction.PeriodId,
            Year = request.Year,
            Month = request.Month,
            RequestId = correction.RequestId,
            RequestSeriesId = correction.RequestSeriesId,
            RequestRevisionNumber = correction.RequestRevisionNumber,
            RequestCode = request.VppCode ?? request.Id.ToString(),
            RequestOwnerUserId = request.CreatedByUserId,
            SettlementId = correction.SettlementId,
            SettlementRevisionNumber = settlement.RevisionNumber,
            MemberCompanyCode = correction.MemberCompanyCode,
            Action = correction.Action.ToString(),
            Status = correction.Status.ToString(),
            Reason = correction.Reason,
            EmployeeNote = correction.EmployeeNote,
            DecisionReason = correction.DecisionReason,
            RequestedByUserId = correction.RequestedByUserId,
            RequestedAtUtc = correction.RequestedAtUtc,
            DecidedByUserId = correction.DecidedByUserId,
            DecidedAtUtc = correction.DecidedAtUtc,
            ResultRequestId = correction.ResultRequestId,
            ResultSettlementId = correction.ResultSettlementId,
            ChangedItemCount = delta.ChangedItems.Length,
            RemovedItemCount = delta.RemovedItemIds.Length,
            RemovedItemIds = delta.RemovedItemIds,
            RowVersion = correction.RowVersion,
            Items = correction.Items.Select(x => new PostSettlementOrderCorrectionItemResDTO
            {
                VppId = x.VppId,
                Qty = x.Qty,
                Description = x.Description
            }).ToArray()
        };
    }

    private static CorrectionDelta BuildDelta(
        VppRequest source,
        PostSettlementOrderCorrection correction)
    {
        var original = source.RequestDetails
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.VppId)
            .ToDictionary(x => x.Key, x => x.First().Qty);
        if (correction.Action == PostSettlementOrderCorrectionAction.Cancel)
        {
            return new CorrectionDelta([], original.Keys.OrderBy(x => x).ToArray());
        }

        var target = correction.Items
            .Where(x => !x.IsDeleted)
            .GroupBy(x => x.VppId)
            .ToDictionary(x => x.Key, x => x.First().Qty);
        var changed = target
            .Where(x => original.TryGetValue(x.Key, out var oldQty) && oldQty != x.Value)
            .OrderBy(x => x.Key)
            .Select(x => new QuantityDelta(x.Key, original[x.Key], x.Value))
            .ToArray();
        var removed = original.Keys
            .Where(id => !target.ContainsKey(id))
            .OrderBy(id => id)
            .ToArray();
        return new CorrectionDelta(changed, removed);
    }

    private sealed record QuantityDelta(Guid VppId, int OldQty, int NewQty);
    private sealed record CorrectionDelta(QuantityDelta[] ChangedItems, Guid[] RemovedItemIds);

    private static PostSettlementOrderCorrectionAction ParseAction(string? value)
        => Enum.TryParse<PostSettlementOrderCorrectionAction>(value, true, out var action)
            ? action
            : throw new BusinessException("Action phải là Adjust hoặc Cancel.");

    private static string NormalizeCompany(string value)
        => string.IsNullOrWhiteSpace(value)
            ? throw new BusinessException("Thiếu mã công ty.")
            : value.Trim();

    private static string ValidateRequiredText(string? value, string label, int min, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < min || normalized.Length > max)
            throw new BusinessException($"{label} phải có từ {min} đến {max} ký tự.");
        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int max)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized)) return null;
        if (normalized.Length > max) throw new BusinessException($"Nội dung không được vượt quá {max} ký tự.");
        return normalized;
    }

    private static void EnsureRowVersion(byte[]? actual, byte[]? expected, string message)
    {
        if (expected is not { Length: > 0 })
            throw new BusinessException("RowVersion là bắt buộc.");
        if (actual is null || !actual.AsSpan().SequenceEqual(expected))
            throw new ConflictException(message);
    }

    private static string Hash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private static void EnsureAdjustmentWindowOpen(VppPeriod period, DateTime nowUtc)
    {
        var deadlineUtc = period.PostCloseAdjustmentDeadlineUtc
            ?? period.SubmissionDeadlineUtc.AddDays(10);
        if (nowUtc >= deadlineUtc)
            throw new ConflictException("Đã hết thời gian chỉnh đơn của kỳ này.");
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
