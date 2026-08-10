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
    IDateTimeProvider dateTimeProvider) : IPostSettlementOrderCorrectionService
{
    private readonly IUnitOfWork _scopedUow = scopedUow;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

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
                && x.Status == PostSettlementOrderCorrectionStatus.Pending
                && !x.IsDeleted, cancellationToken))
        {
            throw new ConflictException("Đơn này đã có một yêu cầu điều chỉnh đang chờ duyệt.");
        }

        var normalizedItems = action == PostSettlementOrderCorrectionAction.Cancel
            ? []
            : ValidateItems(request.Items, settlement.Items);
        var nowUtc = NormalizeUtc(_dateTimeProvider.Now);
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
                .Include(x => x.Charges.Where(charge => !charge.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == correction.SettlementId
                    && x.IsCurrentRevision && !x.IsDeleted, cancellationToken)
                ?? throw new ConflictException("Bảng chốt đã có revision mới; hãy tạo lại yêu cầu điều chỉnh.");
            var period = await _scopedUow.VPPContext.Set<VppPeriod>()
                .FirstAsync(x => x.Id == correction.PeriodId && !x.IsDeleted, cancellationToken);
            if (period.State != VppPeriodState.Settled)
                throw new ConflictException("Kỳ không còn ở trạng thái đã chốt.");

            var nowUtc = NormalizeUtc(_dateTimeProvider.Now);
            var replacement = BuildRequestRevision(correction, currentRequest, currentSettlement, actorUserId, nowUtc);
            currentRequest.IsCurrentRevision = false;
            currentRequest.SupersededByRequestId = replacement.Id;
            currentRequest.UpdatedByUserId = actorUserId;
            currentRequest.UpdatedAtUtc = nowUtc;
            _scopedUow.VPPContext.Set<VppRequest>().Add(replacement);
            await _scopedUow.SaveChangesAsync(cancellationToken);

            var settlementRevision = await BuildSettlementRevisionAsync(
                correction, currentSettlement, period, actorUserId, nowUtc, cancellationToken);
            currentSettlement.IsCurrentRevision = false;
            currentSettlement.SupersededBySettlementId = settlementRevision.Id;
            currentSettlement.UpdatedByUserId = actorUserId;
            currentSettlement.UpdatedAtUtc = nowUtc;
            _scopedUow.VPPContext.Set<Settlement>().Add(settlementRevision);

            correction.Status = PostSettlementOrderCorrectionStatus.Confirmed;
            correction.DecisionReason = NormalizeOptionalText(request.Reason, 500);
            correction.DecidedByUserId = actorUserId;
            correction.DecidedAtUtc = nowUtc;
            correction.ResultRequestId = replacement.Id;
            correction.ResultSettlementId = settlementRevision.Id;
            correction.UpdatedByUserId = actorUserId;
            correction.UpdatedAtUtc = nowUtc;
            period.LastTransitionUserId = actorUserId;
            period.LastTransitionAtUtc = nowUtc;
            period.LastTransitionReason = correction.Action == PostSettlementOrderCorrectionAction.Cancel
                ? $"Đã duyệt hủy đơn {currentRequest.VppCode}; lưu bản chốt {settlementRevision.RevisionNumber}."
                : $"Đã duyệt cập nhật đơn {currentRequest.VppCode}; lưu bản chốt {settlementRevision.RevisionNumber}.";
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
                    SettlementRevisionId = settlementRevision.Id,
                    settlementRevision.RevisionNumber
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

    private async Task<Settlement> BuildSettlementRevisionAsync(
        PostSettlementOrderCorrection correction,
        Settlement current,
        VppPeriod period,
        int actorUserId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var headers = await _scopedUow.VPPContext.Set<VppRequest>()
            .Where(x => x.PeriodId == period.Id && !x.IsDeleted && x.IsCurrentRevision
                && ((x.IsAdditionalOrder && x.Status == (int)VPPStatus.Approved)
                    || (!x.IsAdditionalOrder && (x.Status == (int)VPPStatus.Submitted
                        || x.Status == (int)VPPStatus.Approved))))
            .Include(x => x.RequestDetails.Where(detail => !detail.IsDeleted))
            .ToListAsync(cancellationToken);
        var snapshots = current.Items.ToDictionary(x => x.VppId);
        var totals = headers.SelectMany(x => x.RequestDetails)
            .GroupBy(x => x.VppId)
            .ToDictionary(x => x.Key, x => x.Sum(detail => (decimal)detail.Qty));
        var missing = totals.Keys.Where(vppId => !snapshots.ContainsKey(vppId)).ToArray();
        if (missing.Length > 0)
            throw new ConflictException(
                "Bản chốt hiện tại chưa có mặt hàng này. Vui lòng chỉ điều chỉnh các mặt hàng đã có.");

        var revision = new Settlement
        {
            Id = Guid.NewGuid(),
            PeriodId = current.PeriodId,
            MemberCompanyCode = current.MemberCompanyCode,
            Year = current.Year,
            Month = current.Month,
            RevisionNumber = current.RevisionNumber + 1,
            IsCurrentRevision = true,
            IsCorrection = true,
            SupersedesSettlementId = current.Id,
            CorrectionReason = correction.Action == PostSettlementOrderCorrectionAction.Cancel
                ? $"Hủy đơn: {correction.Reason}"
                : $"Cập nhật đơn: {correction.Reason}",
            PrimarySupplierId = current.PrimarySupplierId,
            PrimarySupplierName = current.PrimarySupplierName,
            PriceListId = current.PriceListId,
            PriceListCode = current.PriceListCode,
            PriceListName = current.PriceListName,
            PriceListVersion = current.PriceListVersion,
            PriceAsOfUtc = current.PriceAsOfUtc,
            CalculationVersion = current.CalculationVersion,
            InputHash = Hash(totals.OrderBy(x => x.Key).Select(x => new { x.Key, x.Value })),
            IdempotencyKey = $"post-settlement-order-correction:{correction.Id:N}",
            CommandPayloadHash = Hash(new { correction.Id, Command = "confirm" }),
            CurrencyCode = current.CurrencyCode,
            ConfirmedAtUtc = nowUtc,
            ConfirmedByUserId = actorUserId,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        };

        foreach (var itemGroup in headers
            .SelectMany(header => header.RequestDetails.Select(detail => new { header, detail }))
            .GroupBy(x => x.detail.VppId)
            .OrderBy(x => x.Key))
        {
            var snapshot = snapshots[itemGroup.Key];
            var quantity = itemGroup.Sum(x => (decimal)x.detail.Qty);
            if (snapshot.MinimumOrderQuantity > 0 && quantity < snapshot.MinimumOrderQuantity)
                throw new ConflictException($"Số lượng sau điều chỉnh của {snapshot.VppCode} thấp hơn MOQ đã chốt.");
            var calculation = PriceCalculationEngine.CalculateLine(snapshot.NetUnitPrice, snapshot.VatRate, quantity);
            var revisionItem = new SettlementItem
            {
                Id = Guid.NewGuid(),
                SettlementId = revision.Id,
                VppId = snapshot.VppId,
                VppCode = snapshot.VppCode,
                VppName = snapshot.VppName,
                UomId = snapshot.UomId,
                UomCode = snapshot.UomCode,
                UomName = snapshot.UomName,
                SupplierId = snapshot.SupplierId,
                PriceListId = snapshot.PriceListId,
                PriceBookItemId = snapshot.PriceBookItemId,
                SupplierSku = snapshot.SupplierSku,
                Quantity = quantity,
                NetUnitPrice = snapshot.NetUnitPrice,
                VatRate = snapshot.VatRate,
                NetAmount = calculation.NetAmount,
                VatAmount = calculation.VatAmount,
                GrossAmount = calculation.GrossAmount,
                MinimumOrderQuantity = snapshot.MinimumOrderQuantity,
                LeadTimeDays = snapshot.LeadTimeDays,
                IsSupplierException = snapshot.IsSupplierException,
                SupplierExceptionReason = snapshot.SupplierExceptionReason,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = nowUtc
            };
            revision.Items.Add(revisionItem);

            var sources = itemGroup.OrderBy(x => x.header.Id).ThenBy(x => x.detail.Id).ToList();
            var weights = sources.Select(x => (decimal)x.detail.Qty).ToArray();
            var netShares = Allocate(calculation.NetAmount, weights);
            var vatShares = Allocate(calculation.VatAmount, weights);
            for (var index = 0; index < sources.Count; index++)
            {
                var source = sources[index];
                revision.Allocations.Add(new SettlementAllocation
                {
                    Id = Guid.NewGuid(),
                    SettlementId = revision.Id,
                    SettlementItemId = revisionItem.Id,
                    RequestHeaderId = source.header.Id,
                    RequestDetailId = source.detail.Id,
                    DepartmentCode = source.header.DepartmentCode,
                    RequesterUserId = source.header.CreatedByUserId,
                    Quantity = source.detail.Qty,
                    NetAmount = netShares[index],
                    VatAmount = vatShares[index],
                    GrossAmount = netShares[index] + vatShares[index],
                    CreatedByUserId = actorUserId,
                    CreatedAtUtc = nowUtc,
                    UpdatedByUserId = actorUserId,
                    UpdatedAtUtc = nowUtc
                });
            }
        }

        revision.Subtotal = PriceCalculationEngine.RoundMoney(revision.Items.Sum(x => x.NetAmount));
        revision.VatAmount = PriceCalculationEngine.RoundMoney(revision.Items.Sum(x => x.VatAmount));
        if (revision.Items.Count > 0)
        {
            var discountRate = current.Subtotal == 0 ? 0 : current.DiscountAmount * 100m / current.Subtotal;
            revision.RebateAmount = current.RebateAmount;
            revision.FeeAmount = current.FeeAmount;
            revision.ShippingAmount = current.ShippingAmount;
            var basket = PriceCalculationEngine.CalculateBasket(
                revision.Subtotal, revision.VatAmount, discountRate,
                revision.RebateAmount, revision.FeeAmount, revision.ShippingAmount);
            revision.DiscountAmount = basket.DiscountAmount;
            revision.GrandTotal = basket.GrandTotal;
            var exact = revision.Subtotal - revision.DiscountAmount - revision.RebateAmount
                + revision.FeeAmount + revision.ShippingAmount + revision.VatAmount;
            revision.RoundingAdjustment = revision.GrandTotal - exact;
        }

        AddCharge(revision, "Discount", -revision.DiscountAmount, actorUserId, nowUtc);
        AddCharge(revision, "Rebate", -revision.RebateAmount, actorUserId, nowUtc);
        AddCharge(revision, "Fee", revision.FeeAmount, actorUserId, nowUtc);
        AddCharge(revision, "Shipping", revision.ShippingAmount, actorUserId, nowUtc);
        AddCharge(revision, "Rounding", revision.RoundingAdjustment, actorUserId, nowUtc);

        var allocations = revision.Allocations.OrderBy(x => x.RequestHeaderId).ThenBy(x => x.RequestDetailId).ToList();
        if (allocations.Count > 0)
        {
            var commercial = -revision.DiscountAmount - revision.RebateAmount + revision.FeeAmount + revision.ShippingAmount;
            var weights = allocations.Select(x => x.NetAmount).ToArray();
            if (weights.Sum() == 0) weights = allocations.Select(x => x.Quantity).ToArray();
            var commercialShares = Allocate(commercial, weights);
            var roundingShares = Allocate(revision.RoundingAdjustment, weights);
            for (var index = 0; index < allocations.Count; index++)
            {
                allocations[index].CommercialAdjustmentAmount = commercialShares[index];
                allocations[index].RoundingAdjustment = roundingShares[index];
                allocations[index].GrossAmount = allocations[index].NetAmount + allocations[index].VatAmount
                    + commercialShares[index] + roundingShares[index];
            }
            if (revision.GrandTotal != allocations.Sum(x => x.GrossAmount))
                throw new BusinessException("Phân bổ revision bảng chốt không cân bằng.");
        }

        return revision;
    }

    private static void AddCharge(Settlement settlement, string type, decimal amount, int actorUserId, DateTime nowUtc)
        => settlement.Charges.Add(new SettlementCharge
        {
            Id = Guid.NewGuid(),
            SettlementId = settlement.Id,
            ChargeType = type,
            Amount = amount,
            AllocationBasis = "net-amount",
            CreatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedByUserId = actorUserId,
            UpdatedAtUtc = nowUtc
        });

    private static List<PostSettlementOrderCorrectionItemReqDTO> ValidateItems(
        IReadOnlyCollection<PostSettlementOrderCorrectionItemReqDTO>? items,
        IEnumerable<SettlementItem> settlementItems)
    {
        if (items is null || items.Count == 0)
            throw new BusinessException("Điều chỉnh phải có ít nhất một mặt hàng.");
        if (items.Any(x => x.VppId == Guid.Empty || x.Qty <= 0))
            throw new BusinessException("Mặt hàng và số lượng điều chỉnh không hợp lệ.");
        if (items.GroupBy(x => x.VppId).Any(group => group.Count() > 1))
            throw new BusinessException("Mỗi mặt hàng chỉ được xuất hiện một lần.");
        var settledIds = settlementItems.Select(x => x.VppId).ToHashSet();
        if (items.Any(x => !settledIds.Contains(x.VppId)))
            throw new BusinessException("Không thể thêm mặt hàng chưa có trong bảng chốt hiện hành.");
        return items.Select(x => new PostSettlementOrderCorrectionItemReqDTO
        {
            VppId = x.VppId,
            Qty = x.Qty,
            Description = NormalizeOptionalText(x.Description, 500)
        }).ToList();
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
        Settlement settlement) => new()
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
            RowVersion = correction.RowVersion,
            Items = correction.Items.Select(x => new PostSettlementOrderCorrectionItemResDTO
            {
                VppId = x.VppId,
                Qty = x.Qty,
                Description = x.Description
            }).ToArray()
        };

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

    private static decimal[] Allocate(decimal total, IReadOnlyList<decimal> weights)
    {
        if (weights.Count == 0) return [];
        var result = new decimal[weights.Count];
        var totalWeight = weights.Sum();
        var allocated = 0m;
        for (var index = 0; index < weights.Count - 1; index++)
        {
            result[index] = totalWeight == 0 ? 0
                : PriceCalculationEngine.RoundMoney(total * weights[index] / totalWeight);
            allocated += result[index];
        }
        result[^1] = total - allocated;
        return result;
    }

    private static string Hash(object payload)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
