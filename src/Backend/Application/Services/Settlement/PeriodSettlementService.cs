using System.Text.Json;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class PeriodSettlementService : IPeriodSettlementService
    {
        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPriceBookWorkflowService? _priceBookWorkflowService;
        private readonly IPriceAsOfResolver? _priceAsOfResolver;

        public PeriodSettlementService(IUnitOfWork scopedUow, IDateTimeProvider dateTimeProvider)
            : this(scopedUow, dateTimeProvider, null, null)
        {
        }

        public PeriodSettlementService(
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IPriceBookWorkflowService? priceBookWorkflowService)
            : this(scopedUow, dateTimeProvider, priceBookWorkflowService, null)
        {
        }

        public PeriodSettlementService(
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IPriceBookWorkflowService? priceBookWorkflowService,
            IPriceAsOfResolver? priceAsOfResolver)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _priceBookWorkflowService = priceBookWorkflowService;
            _priceAsOfResolver = priceAsOfResolver;
        }

        public async Task<SettlementPreviewResDTO> PreviewAsync(
            SettlementPreviewReqDTO req,
            CancellationToken cancellationToken = default)
        {
            ValidatePeriod(req.Year, req.Month);
            if (_priceBookWorkflowService is null)
            {
                throw new BusinessException("Settlement preview is unavailable.");
            }

            var headers = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => x.Year == req.Year && x.Month == req.Month && !x.IsDeleted
                         && ((x.IsAdditionalOrder
                              && x.IsCurrentRevision
                              && x.Status == (int)VPPStatus.Approved)
                             || (!x.IsAdditionalOrder
                                 && x.IsCurrentRevision
                                 && (x.Status == (int)VPPStatus.Submitted
                                     || x.Status == (int)VPPStatus.Approved))))
                .Include(x => x.RequestDetails.Where(detail => !detail.IsDeleted))
                .ToListAsync(cancellationToken);

            var pendingAdditionalCount = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .CountAsync(x => x.Year == req.Year && x.Month == req.Month && !x.IsDeleted
                              && x.IsAdditionalOrder
                              && x.Status == (int)VPPStatus.Pending, cancellationToken);

            var totals = headers
                .SelectMany(x => x.RequestDetails)
                .GroupBy(x => x.VppId)
                .ToDictionary(group => group.Key, group => group.Sum(detail => (decimal)detail.Qty));

            var asOfUtc = req.PriceAsOfUtc.HasValue
                ? NormalizeUtc(req.PriceAsOfUtc.Value)
                : PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
            var response = new SettlementPreviewResDTO
            {
                Year = req.Year,
                Month = req.Month,
                PriceAsOfUtc = asOfUtc,
                RequestedItemCount = totals.Count,
                RequestedLineCount = headers.Sum(x => x.RequestDetails.Count),
                PendingAdditionalCount = pendingAdditionalCount
            };

            if (pendingAdditionalCount > 0)
            {
                response.Blockers.Add($"PENDING_SUPPLEMENTS:{pendingAdditionalCount}");
            }
            if (totals.Count == 0)
            {
                response.Blockers.Add("NO_SUBMITTED_ITEMS");
            }

            var duplicateExceptionItems = (req.Exceptions ?? [])
                .GroupBy(x => x.VppId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToHashSet();
            foreach (var exception in req.Exceptions ?? [])
            {
                var valid = exception.VppId != Guid.Empty
                             && exception.SupplierId != Guid.Empty
                             && totals.ContainsKey(exception.VppId)
                             && !duplicateExceptionItems.Contains(exception.VppId)
                             && !string.IsNullOrWhiteSpace(exception.Reason)
                             && exception.Reason.Trim().Length is >= 5 and <= 500;
                response.Exceptions.Add(new SettlementExceptionResDTO
                {
                    VppId = exception.VppId,
                    SupplierId = exception.SupplierId,
                    PriceListId = exception.PriceListId,
                    Reason = exception.Reason?.Trim(),
                    IsValid = valid
                });
                if (!valid)
                {
                    response.Blockers.Add($"INVALID_SUPPLIER_EXCEPTION:{exception.VppId}");
                }
            }

            if (totals.Count > 0)
            {
                var comparison = await _priceBookWorkflowService.CompareAsync(new PriceBookComparisonReqDTO
                {
                    PriceAsOfUtc = asOfUtc,
                    Items = totals.Select(x => new PriceBookComparisonItemReqDTO
                    {
                        VppId = x.Key,
                        Quantity = x.Value
                    }).ToList()
                }, cancellationToken);
                response.Quotes = comparison.Quotes;
                response.SupplierRecommendation = SettlementSupplierOptimizer.Build(
                    comparison.Quotes,
                    totals.Count);

                var candidates = response.Quotes.AsEnumerable();
                if (req.PriceListId.HasValue)
                {
                    candidates = candidates.Where(x => x.PriceListId == req.PriceListId.Value);
                }
                if (req.PrimarySupplierId.HasValue)
                {
                    candidates = candidates.Where(x => x.SupplierId == req.PrimarySupplierId.Value);
                }

                response.PrimaryQuote = (req.Exceptions?.Count > 0
                        ? candidates
                        : candidates.Where(x => x.IsEligible))
                    .FirstOrDefault();
                if (response.PrimaryQuote is not null && response.Exceptions.Count > 0)
                {
                    await ApplySupplierExceptionsAsync(response, totals, asOfUtc, cancellationToken);
                }
                if (response.PrimaryQuote is null)
                {
                    response.Blockers.Add(req.PrimarySupplierId.HasValue
                        ? "PRIMARY_SUPPLIER_NOT_COVERED"
                        : req.PriceListId.HasValue ? "PRICE_BOOK_NOT_COVERED" : "NO_COMPLETE_PRICE_COVERAGE");
                }
                else
                {
                    response.PrimarySupplierId = response.PrimaryQuote.SupplierId;
                    response.PrimaryPriceListId = response.PrimaryQuote.PriceListId;
                    response.PrimaryPriceListVersion = response.PrimaryQuote.Version;
                    if (!response.PrimaryQuote.IsEligible)
                    {
                        response.Blockers.Add("PRIMARY_QUOTE_HAS_UNRESOLVED_ITEMS");
                    }

                    response.Allocations = BuildPreviewAllocations(headers, response.PrimaryQuote);
                }
            }

            response.InputHash = ComputeInputHash(
                req,
                asOfUtc,
                totals,
                response.PrimarySupplierId,
                response.PrimaryPriceListId);
            return response;
        }

        public Task<SettlementRevisionResDTO> ConfirmAsync(
            SettlementConfirmReqDTO req,
            int userId,
            CancellationToken cancellationToken = default)
            => SaveRevisionAsync(req, null, null, userId, cancellationToken);

        public Task<SettlementRevisionResDTO> CorrectAsync(
            Guid settlementId,
            SettlementCorrectionReqDTO req,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var reason = ValidateReason(req.Reason, "Lý do điều chỉnh");
            return SaveRevisionAsync(req, settlementId, reason, userId, cancellationToken);
        }

        public async Task<SettlementRevisionResDTO?> GetCurrentAsync(
            int y,
            int m,
            CancellationToken cancellationToken = default)
        {
            ValidatePeriod(y, m);
            var company = CanonicalRbac.DefaultMemberCompanyCode.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            var entity = await SettlementQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted
                    && x.IsCurrentRevision
                    && x.MemberCompanyCode == company
                    && x.Year == y
                    && x.Month == m, cancellationToken);
            return entity is null ? null : MapRevision(entity);
        }

        public async Task<List<SettlementRevisionResDTO>> ListRevisionsAsync(
            int y,
            int m,
            CancellationToken cancellationToken = default)
        {
            ValidatePeriod(y, m);
            var company = CanonicalRbac.DefaultMemberCompanyCode.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            var entities = await SettlementQuery()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Year == y
                    && x.Month == m)
                .OrderByDescending(x => x.RevisionNumber)
                .ToListAsync(cancellationToken);
            return entities.Select(MapRevision).ToList();
        }

        public async Task<SettlementExportResult?> ExportPdfAsync(
            Guid settlementId,
            CancellationToken cancellationToken = default)
        {
            var settlement = await FindExportSettlementAsync(settlementId, cancellationToken);
            var lookups = settlement is null
                ? null
                : await LoadExportLookupsAsync(settlement, cancellationToken);
            return settlement is null
                ? null
                : new SettlementExportResult(
                    SettlementPdfBuilder.Build(settlement, lookups!.SupplierNames),
                    ExportFileContract.Settlement(
                        settlement.Year, settlement.Month, settlement.RevisionNumber, "pdf"),
                    ExportFileContract.PdfContentType);
        }

        public async Task<SettlementExportResult?> ExportWorkbookAsync(
            Guid settlementId,
            CancellationToken cancellationToken = default)
        {
            var settlement = await FindExportSettlementAsync(settlementId, cancellationToken);
            var lookups = settlement is null
                ? null
                : await LoadExportLookupsAsync(settlement, cancellationToken);
            return settlement is null
                ? null
                : new SettlementExportResult(
                    SettlementWorkbookBuilder.Build(
                        settlement,
                        lookups!.SupplierNames,
                        lookups.PriceListNames),
                    ExportFileContract.Settlement(
                        settlement.Year, settlement.Month, settlement.RevisionNumber, "xlsx"),
                    ExportFileContract.ExcelContentType);
        }

        private async Task<SettlementRevisionResDTO> SaveRevisionAsync(
            SettlementConfirmReqDTO req,
            Guid? correctionSettlementId,
            string? correctionReason,
            int userId,
            CancellationToken cancellationToken)
        {
            ValidateConfirmRequest(req);
            var company = CanonicalRbac.DefaultMemberCompanyCode.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            var asOfUtc = NormalizeUtc(req.PriceAsOfUtc);
            var commandHash = ComputeCommandPayloadHash(req, correctionSettlementId, correctionReason, asOfUtc);

            var replay = await FindIdempotentAsync(company, req.IdempotencyKey, cancellationToken);
            if (replay is not null)
            {
                return MatchIdempotent(replay, commandHash);
            }

            if (correctionSettlementId.HasValue)
            {
                var correctionTarget = await _scopedUow.VPPContext.Set<Settlement>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == correctionSettlementId.Value && !x.IsDeleted, cancellationToken)
                    ?? throw new BusinessException("Settlement revision not found.");
                if (!correctionTarget.IsCurrentRevision)
                {
                    throw new ConflictException("Bản chốt đã thay đổi. Hãy tải lại trước khi điều chỉnh.");
                }
                if (correctionTarget.ConfirmedByUserId == userId)
                {
                    throw new ConflictException("Một quản lý khác cần thực hiện lần điều chỉnh này.");
                }
                if (correctionTarget.HasExternalProcurementImpact)
                {
                    throw new ConflictException(
                        "Bản chốt đã được dùng cho hoạt động mua sắm nên không thể điều chỉnh trực tiếp.");
                }
            }

            var previewReq = new SettlementPreviewReqDTO
            {
                Year = req.Year,
                Month = req.Month,
                PriceAsOfUtc = asOfUtc,
                PriceListId = req.PriceListId,
                PrimarySupplierId = req.PrimarySupplierId,
                Exceptions = req.Exceptions ?? []
            };
            var preview = await PreviewAsync(previewReq, cancellationToken);
            if (!string.Equals(preview.InputHash, req.InputHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("Settlement inputs changed after preview. Run preview again.");
            }
            if (preview.Blockers.Count > 0 || preview.PrimaryQuote is null || !preview.PrimaryQuote.IsEligible)
            {
                throw new BusinessException("Settlement preview has blockers: " + string.Join("; ", preview.Blockers));
            }
            if (preview.PrimarySupplierId != req.PrimarySupplierId
                || preview.PrimaryPriceListId != req.PriceListId)
            {
                throw new ConflictException("The selected supplier or price book no longer matches the preview.");
            }

            if (correctionSettlementId.HasValue)
            {
                var pendingCorrections = await _scopedUow.VPPContext.Set<PostSettlementOrderCorrection>()
                    .CountAsync(x => !x.IsDeleted
                        && x.MemberCompanyCode == company
                        && x.SettlementId == correctionSettlementId.Value
                        && x.Status == PostSettlementOrderCorrectionStatus.Pending,
                        cancellationToken);
                if (pendingCorrections > 0)
                {
                    throw new ConflictException(
                        $"Còn {pendingCorrections} yêu cầu sửa hoặc hủy đơn đang chờ duyệt.");
                }
            }

            await _scopedUow.BeginTransactionAsync();
            try
            {
                replay = await FindIdempotentAsync(company, req.IdempotencyKey, cancellationToken);
                if (replay is not null)
                {
                    var replayResult = MatchIdempotent(replay, commandHash);
                    await _scopedUow.RollbackAsync();
                    return replayResult;
                }

                var current = await _scopedUow.VPPContext.Set<Settlement>()
                    .FirstOrDefaultAsync(x => !x.IsDeleted
                        && x.IsCurrentRevision
                        && x.MemberCompanyCode == company
                        && x.Year == req.Year
                        && x.Month == req.Month, cancellationToken);
                var latest = await _scopedUow.VPPContext.Set<Settlement>()
                    .Where(x => !x.IsDeleted
                        && x.MemberCompanyCode == company
                        && x.Year == req.Year
                        && x.Month == req.Month)
                    .OrderByDescending(x => x.RevisionNumber)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!correctionSettlementId.HasValue && current is not null)
                {
                    throw new ConflictException("Kỳ đã được chốt. Hãy dùng Điều chỉnh sau chốt.");
                }
                if (!correctionSettlementId.HasValue
                    && current is null
                    && latest is not null)
                {
                    throw new ConflictException("Lịch sử bản chốt không hợp lệ. Hãy tải lại dữ liệu.");
                }
                if (correctionSettlementId.HasValue
                    && (current is null || current.Id != correctionSettlementId.Value))
                {
                    throw new ConflictException("The settlement revision changed. Reload before correcting it.");
                }
                if (current is not null && current.ConfirmedByUserId == userId)
                {
                    throw new ConflictException("Một quản lý khác cần thực hiện lần điều chỉnh này.");
                }

                var period = await _scopedUow.VPPContext.Set<VppPeriod>()
                    .FirstOrDefaultAsync(x => !x.IsDeleted
                        && x.MemberCompanyCode == company
                        && x.Year == req.Year
                        && x.Month == req.Month, cancellationToken)
                    ?? throw new BusinessException("The persisted company period was not found.");
                var validState = correctionSettlementId.HasValue
                    ? period.State == VppPeriodState.Settled
                    : period.State is VppPeriodState.SubmissionClosed or VppPeriodState.Pricing;
                if (!validState)
                {
                    throw new ConflictException(correctionSettlementId.HasValue
                        ? "Kỳ không còn ở trạng thái đã chốt."
                        : "Kỳ phải đóng nhận đơn trước khi chốt.");
                }

                var headers = await EligibleHeaders(req.Year, req.Month)
                    .Include(x => x.RequestDetails.Where(detail => !detail.IsDeleted))
                    .ToListAsync(cancellationToken);
                var totals = headers.SelectMany(x => x.RequestDetails)
                    .GroupBy(x => x.VppId)
                    .ToDictionary(x => x.Key, x => x.Sum(detail => (decimal)detail.Qty));
                var transactionHash = ComputeInputHash(
                    previewReq,
                    asOfUtc,
                    totals,
                    preview.PrimarySupplierId,
                    preview.PrimaryPriceListId);
                if (!string.Equals(transactionHash, req.InputHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("Settlement request lines changed during confirmation.");
                }

                var vppIds = totals.Keys.ToArray();
                var book = await _scopedUow.VPPContext.Set<PriceList>()
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .Include(x => x.SupplierProductMappings!.Where(item => !item.IsDeleted && vppIds.Contains(item.VppItemId)))
                    .FirstOrDefaultAsync(x => x.Id == req.PriceListId && !x.IsDeleted, cancellationToken)
                    ?? throw new BusinessException("The selected price book was not found.");
                if (book.SupplierId != req.PrimarySupplierId
                    || book.Status != PriceListStatus.Published
                    || book.EffectiveFromUtc > asOfUtc
                    || (book.EffectiveToUtc.HasValue && asOfUtc >= book.EffectiveToUtc.Value))
                {
                    throw new ConflictException("The selected price book is no longer published and effective.");
                }

                var products = await _scopedUow.VPPContext.Set<VppItem>()
                    .AsNoTracking()
                    .Where(x => vppIds.Contains(x.Id) && !x.IsDeleted)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);
                if (products.Count != vppIds.Length)
                {
                    throw new BusinessException("One or more settlement catalog items are missing or inactive.");
                }

                var nowUtc = PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
                var settlement = new Settlement
                {
                    Id = Guid.NewGuid(),
                    PeriodId = period.Id,
                    MemberCompanyCode = company,
                    Year = req.Year,
                    Month = req.Month,
                    RevisionNumber = latest?.RevisionNumber + 1 ?? 1,
                    IsCurrentRevision = true,
                    IsCorrection = correctionSettlementId.HasValue,
                    SupersedesSettlementId = correctionSettlementId,
                    CorrectionReason = correctionReason,
                    PrimarySupplierId = req.PrimarySupplierId,
                    PrimarySupplierName = book.Supplier?.SupplierName ?? book.Supplier?.SupplierShortName ?? req.PrimarySupplierId.ToString(),
                    PriceListId = book.Id,
                    PriceListCode = book.PriceListCode,
                    PriceListName = book.PriceListName ?? book.PriceListCode ?? book.Id.ToString(),
                    PriceListVersion = book.Version,
                    PriceAsOfUtc = asOfUtc,
                    CalculationVersion = PriceCalculationEngine.CurrentVersion,
                    InputHash = req.InputHash.ToUpperInvariant(),
                    IdempotencyKey = req.IdempotencyKey.Trim(),
                    CommandPayloadHash = commandHash,
                    CurrencyCode = book.CurrencyCode,
                    RebateAmount = PriceCalculationEngine.RoundMoney(book.RebateAmount),
                    FeeAmount = PriceCalculationEngine.RoundMoney(book.FeeAmount),
                    ShippingAmount = PriceCalculationEngine.RoundMoney(book.ShippingAmount),
                    ConfirmedAtUtc = nowUtc,
                    ConfirmedByUserId = userId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = nowUtc,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = nowUtc,
                    IsDeleted = false
                };

                var exceptionByVpp = (req.Exceptions ?? []).ToDictionary(x => x.VppId);
                var primaryRows = (book.SupplierProductMappings ?? [])
                    .GroupBy(x => x.VppItemId)
                    .ToDictionary(x => x.Key, x => x.ToList());
                foreach (var itemGroup in headers.SelectMany(header => header.RequestDetails.Select(detail => new { header, detail }))
                             .GroupBy(x => x.detail.VppId)
                             .OrderBy(x => x.Key))
                {
                    var quantity = itemGroup.Sum(x => (decimal)x.detail.Qty);
                    var isException = exceptionByVpp.TryGetValue(itemGroup.Key, out var supplierException);
                    var evidence = isException
                        ? await ResolveExceptionEvidenceAsync(itemGroup.Key, quantity, supplierException!, asOfUtc, cancellationToken)
                        : ResolvePrimaryEvidence(itemGroup.Key, quantity, req.PrimarySupplierId, book.Id, primaryRows);
                    var product = products[itemGroup.Key];
                    var line = PriceCalculationEngine.CalculateLine(evidence.NetUnitPrice, evidence.VatRate, quantity);
                    var settlementItem = new SettlementItem
                    {
                        Id = Guid.NewGuid(),
                        SettlementId = settlement.Id,
                        VppId = product.Id,
                        VppCode = product.VppCode ?? product.Id.ToString(),
                        VppName = product.VppName ?? product.Id.ToString(),
                        UomId = product.UomId,
                        UomCode = product.Uom?.Code ?? product.UomId.ToString(),
                        UomName = product.Uom?.Value ?? product.Uom?.Code ?? product.UomId.ToString(),
                        SupplierId = evidence.SupplierId,
                        PriceListId = evidence.PriceListId,
                        PriceBookItemId = evidence.PriceBookItemId,
                        Quantity = quantity,
                        NetUnitPrice = evidence.NetUnitPrice,
                        VatRate = evidence.VatRate,
                        NetAmount = line.NetAmount,
                        VatAmount = line.VatAmount,
                        GrossAmount = line.GrossAmount,
                        MinimumOrderQuantity = evidence.MinimumOrderQuantity,
                        LeadTimeDays = evidence.LeadTimeDays,
                        IsSupplierException = isException,
                        SupplierExceptionReason = supplierException?.Reason?.Trim(),
                        CreatedByUserId = userId,
                        CreatedAtUtc = nowUtc,
                        UpdatedByUserId = userId,
                        UpdatedAtUtc = nowUtc
                    };
                    settlement.Items.Add(settlementItem);

                    var sources = itemGroup
                        .OrderBy(x => x.header.Id)
                        .ThenBy(x => x.detail.Id)
                        .ToList();
                    var weights = sources.Select(x => (decimal)x.detail.Qty).ToArray();
                    var netShares = AllocateAmount(line.NetAmount, weights);
                    var vatShares = AllocateAmount(line.VatAmount, weights);
                    for (var index = 0; index < sources.Count; index++)
                    {
                        var source = sources[index];
                        settlement.Allocations.Add(new SettlementAllocation
                        {
                            Id = Guid.NewGuid(),
                            SettlementId = settlement.Id,
                            SettlementItemId = settlementItem.Id,
                            RequestHeaderId = source.header.Id,
                            RequestDetailId = source.detail.Id,
                            DepartmentCode = source.header.DepartmentCode,
                            RequesterUserId = source.header.CreatedByUserId,
                            Quantity = source.detail.Qty,
                            NetAmount = netShares[index],
                            VatAmount = vatShares[index],
                            GrossAmount = netShares[index] + vatShares[index],
                            CreatedByUserId = userId,
                            CreatedAtUtc = nowUtc,
                            UpdatedByUserId = userId,
                            UpdatedAtUtc = nowUtc
                        });
                    }
                }

                settlement.Subtotal = PriceCalculationEngine.RoundMoney(settlement.Items.Sum(x => x.NetAmount));
                settlement.VatAmount = PriceCalculationEngine.RoundMoney(settlement.Items.Sum(x => x.VatAmount));
                var basket = PriceCalculationEngine.CalculateBasket(
                    settlement.Subtotal,
                    settlement.VatAmount,
                    book.DiscountRate,
                    settlement.RebateAmount,
                    settlement.FeeAmount,
                    settlement.ShippingAmount);
                settlement.DiscountAmount = basket.DiscountAmount;
                settlement.GrandTotal = basket.GrandTotal;
                var exactTotal = settlement.Subtotal - settlement.DiscountAmount - settlement.RebateAmount
                                 + settlement.FeeAmount + settlement.ShippingAmount + settlement.VatAmount;
                settlement.RoundingAdjustment = settlement.GrandTotal - exactTotal;

                AddCharge(settlement, "Discount", -settlement.DiscountAmount, userId, nowUtc);
                AddCharge(settlement, "Rebate", -settlement.RebateAmount, userId, nowUtc);
                AddCharge(settlement, "Fee", settlement.FeeAmount, userId, nowUtc);
                AddCharge(settlement, "Shipping", settlement.ShippingAmount, userId, nowUtc);
                AddCharge(settlement, "Rounding", settlement.RoundingAdjustment, userId, nowUtc);

                var orderedAllocations = settlement.Allocations
                    .OrderBy(x => x.RequestHeaderId)
                    .ThenBy(x => x.RequestDetailId)
                    .ToList();
                var commercialTotal = -settlement.DiscountAmount - settlement.RebateAmount
                                      + settlement.FeeAmount + settlement.ShippingAmount;
                var allocationWeights = orderedAllocations.Select(x => x.NetAmount).ToArray();
                if (allocationWeights.Sum() == 0m)
                {
                    allocationWeights = orderedAllocations.Select(x => x.Quantity).ToArray();
                }
                var commercialShares = AllocateAmount(commercialTotal, allocationWeights);
                var roundingShares = AllocateAmount(settlement.RoundingAdjustment, allocationWeights);
                for (var index = 0; index < orderedAllocations.Count; index++)
                {
                    orderedAllocations[index].CommercialAdjustmentAmount = commercialShares[index];
                    orderedAllocations[index].RoundingAdjustment = roundingShares[index];
                    orderedAllocations[index].GrossAmount = orderedAllocations[index].NetAmount
                        + orderedAllocations[index].VatAmount
                        + commercialShares[index]
                        + roundingShares[index];
                }
                EnsureReconciled(settlement);

                var superseded = current;
                if (superseded is not null)
                {
                    superseded.IsCurrentRevision = false;
                    superseded.SupersededBySettlementId = settlement.Id;
                    superseded.UpdatedByUserId = userId;
                    superseded.UpdatedAtUtc = nowUtc;
                }
                period.State = VppPeriodState.Settled;
                period.LastTransitionUserId = userId;
                period.LastTransitionAtUtc = nowUtc;
                period.LastTransitionReason = correctionSettlementId.HasValue
                    ? $"Đã lưu bản chốt {settlement.RevisionNumber}: {correctionReason}"
                    : $"Đã chốt kỳ · bản {settlement.RevisionNumber}";
                period.UpdatedByUserId = userId;
                period.UpdatedAtUtc = nowUtc;

                _scopedUow.VPPContext.Set<Settlement>().Add(settlement);
                foreach (var header in headers)
                {
                    _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                    {
                        Id = Guid.NewGuid(),
                        RequestId = header.Id,
                        LogDate = nowUtc,
                        LogTitle = correctionSettlementId.HasValue
                            ? "SETTLEMENT_CORRECTED"
                            : "SETTLEMENT_CONFIRMED",
                        LogJS = JsonSerializer.Serialize(new
                        {
                            SettlementId = settlement.Id,
                            settlement.RevisionNumber,
                            settlement.InputHash,
                            settlement.PrimarySupplierId,
                            settlement.PriceListId,
                            settlement.GrandTotal
                        })
                    });
                }

                if (correctionSettlementId.HasValue)
                {
                    var approvedCorrections = await _scopedUow.VPPContext.Set<PostSettlementOrderCorrection>()
                        .Where(x => !x.IsDeleted
                            && x.MemberCompanyCode == company
                            && x.SettlementId == correctionSettlementId.Value
                            && x.Status == PostSettlementOrderCorrectionStatus.Confirmed
                            && x.ResultSettlementId == null)
                        .ToListAsync(cancellationToken);
                    foreach (var approvedCorrection in approvedCorrections)
                    {
                        approvedCorrection.ResultSettlementId = settlement.Id;
                        approvedCorrection.UpdatedByUserId = userId;
                        approvedCorrection.UpdatedAtUtc = nowUtc;
                    }
                }

                await _scopedUow.CommitAsync();
                return MapRevision(settlement);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<PeriodSettlementResDTO> SettleAsync(PeriodSettlementReqDTO req, int userId)
        {
            ValidatePeriod(req.Year, req.Month);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var list = await ResolvePriceListAsync(req.PriceListId);
                if (list == null)
                {
                    throw new BusinessException("No price list available.");
                }

                var pendingCount = await _scopedUow.VPPContext.Set<VppRequest>()
                    .CountAsync(x => x.Year == req.Year && x.Month == req.Month && !x.IsDeleted
                                  && x.IsAdditionalOrder
                                  && x.Status == (int)VPPStatus.Pending);
                if (pendingCount > 0)
                {
                    throw new ConflictException(
                        $"Cannot settle: {pendingCount} additional order(s) still pending approval.");
                }

                var headers = await _scopedUow.VPPContext.Set<VppRequest>()
                    .Where(x => x.Year == req.Year && x.Month == req.Month && !x.IsDeleted
                             && (x.Status == (int)VPPStatus.Submitted
                              || x.Status == (int)VPPStatus.Approved))
                    .Include(h => h.RequestDetails.Where(d => !d.IsDeleted))
                    .ToListAsync();

                var distinctVppIds = headers
                    .SelectMany(x => x.RequestDetails)
                    .Select(x => x.VppId)
                    .Distinct()
                    .ToArray();

                var priceRows = distinctVppIds.Length == 0
                    ? new List<SupplierProductMapping>()
                    : await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                        .AsNoTracking()
                        .Where(x => x.PriceListId == list.Id
                                 && !x.IsDeleted
                                 && distinctVppIds.Contains(x.VppItemId))
                        .ToListAsync();

                var missing = distinctVppIds
                    .Where(id => !priceRows.Any(row => row.VppItemId == id))
                    .ToList();
                if (missing.Count > 0)
                {
                    var missingNames = await _scopedUow.VPPContext.Set<VppItem>()
                        .Where(v => missing.Contains(v.Id))
                        .Select(v => v.VppName)
                        .ToListAsync();

                    throw new BusinessException(
                        $"Price list '{list.PriceListName}' is missing prices for {missing.Count} item(s): "
                        + string.Join(", ", missingNames));
                }

                var priceByVppId = priceRows
                    .GroupBy(x => x.VppItemId)
                    .ToDictionary(
                        g => g.Key,
                        g => (long)(g.FirstOrDefault(x => x.IsDefault)?.Price ?? g.First().Price));

                var now = _dateTimeProvider.Now;
                foreach (var header in headers)
                {
                    foreach (var detail in header.RequestDetails)
                    {
                        detail.CurrentSinglePrice = priceByVppId[detail.VppId];
                        detail.UpdatedByUserId = userId;
                        detail.UpdatedAtUtc = now;
                    }

                    header.SettledAt = now;
                    header.SettledByUserId = userId;
                    header.SettledByPriceListId = list.Id;
                    header.UpdatedByUserId = userId;
                    header.UpdatedAtUtc = now;

                    _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                    {
                        Id = Guid.NewGuid(),
                        RequestId = header.Id,
                        LogDate = now,
                        LogTitle = "PERIOD_SETTLED",
                        LogJS = JsonSerializer.Serialize(new
                        {
                            req.Year,
                            req.Month,
                            list.Id,
                            list.PriceListName,
                            OrderId = header.Id
                        })
                    });
                }

                await _scopedUow.CommitAsync();
                return await GetStatusAsync(req.Year, req.Month);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<PeriodSettlementResDTO> GetStatusAsync(int y, int m)
        {
            ValidatePeriod(y, m);

            var baseQuery = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => x.Year == y && x.Month == m && !x.IsDeleted);

            var pendingAdditionalCount = await baseQuery
                .CountAsync(x => x.IsAdditionalOrder && x.Status == (int)VPPStatus.Pending);

            var orderCount = await baseQuery
                .CountAsync(x => x.Status == (int)VPPStatus.Submitted
                              || x.Status == (int)VPPStatus.Approved);

            var latest = await baseQuery
                .Where(x => x.SettledAt != null)
                .OrderByDescending(x => x.SettledAt)
                .Select(x => new
                {
                    x.SettledAt,
                    x.SettledByUserId,
                    x.SettledByPriceListId
                })
                .FirstOrDefaultAsync();

            var company = CanonicalRbac.DefaultMemberCompanyCode.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            var currentSnapshot = await _scopedUow.VPPContext.Set<Settlement>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.IsDeleted
                    && x.IsCurrentRevision
                    && x.MemberCompanyCode == company
                    && x.Year == y
                    && x.Month == m);
            var periodState = await _scopedUow.VPPContext.Set<VppPeriod>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Year == y
                    && x.Month == m)
                .Select(x => (VppPeriodState?)x.State)
                .FirstOrDefaultAsync();
            var isSettled = periodState.HasValue
                ? periodState == VppPeriodState.Settled && (currentSnapshot is not null || latest?.SettledAt is not null)
                : currentSnapshot is not null || latest?.SettledAt is not null;

            var result = new PeriodSettlementResDTO
            {
                Year = y,
                Month = m,
                IsSettled = isSettled,
                SettledAt = isSettled ? currentSnapshot?.ConfirmedAtUtc ?? latest?.SettledAt : null,
                SettledByUserId = isSettled ? currentSnapshot?.ConfirmedByUserId ?? latest?.SettledByUserId : null,
                PriceListId = isSettled ? currentSnapshot?.PriceListId ?? latest?.SettledByPriceListId : null,
                PriceListName = isSettled ? currentSnapshot?.PriceListName : null,
                OrderCount = orderCount,
                PendingAdditionalCount = pendingAdditionalCount,
                SettlementId = isSettled ? currentSnapshot?.Id : null,
                RevisionNumber = isSettled ? currentSnapshot?.RevisionNumber : null,
                GrandTotal = isSettled ? currentSnapshot?.GrandTotal : null,
                PrimarySupplierId = isSettled ? currentSnapshot?.PrimarySupplierId : null,
                PrimarySupplierName = isSettled ? currentSnapshot?.PrimarySupplierName : null
            };

            await PopulateNamesAsync(new[] { result });
            return result;
        }

        public async Task<List<PeriodSettlementResDTO>> ListSettledAsync()
        {
            var settledRows = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.SettledAt != null)
                .Select(x => new
                {
                    x.Year,
                    x.Month,
                    x.SettledAt,
                    x.SettledByUserId,
                    x.SettledByPriceListId
                })
                .ToListAsync();

            var snapshotPeriods = await _scopedUow.VPPContext.Set<Settlement>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision)
                .Select(x => new { x.Year, x.Month })
                .ToListAsync();

            var result = new List<PeriodSettlementResDTO>();
            var periods = settledRows.Select(x => new { x.Year, x.Month })
                .Concat(snapshotPeriods)
                .Distinct()
                .ToList();
            foreach (var period in periods)
            {
                var status = await GetStatusAsync(period.Year, period.Month);
                result.Add(status);
            }

            await PopulateNamesAsync(result);
            return result
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();
        }

        private IQueryable<VppRequest> EligibleHeaders(int y, int m)
            => _scopedUow.VPPContext.Set<VppRequest>()
                .Where(x => x.Year == y && x.Month == m && !x.IsDeleted
                         && ((x.IsAdditionalOrder
                              && x.IsCurrentRevision
                              && x.Status == (int)VPPStatus.Approved)
                             || (!x.IsAdditionalOrder
                                 && x.IsCurrentRevision
                                 && (x.Status == (int)VPPStatus.Submitted
                                     || x.Status == (int)VPPStatus.Approved))));

        private IQueryable<Settlement> SettlementQuery()
            => _scopedUow.VPPContext.Set<Settlement>()
                .Include(x => x.Items)
                .Include(x => x.Allocations);

        private async Task<Settlement?> FindExportSettlementAsync(
            Guid settlementId,
            CancellationToken cancellationToken)
        {
            var company = CanonicalRbac.DefaultMemberCompanyCode.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            return await _scopedUow.VPPContext.Set<Settlement>()
                .Include(x => x.Items)
                .Include(x => x.Allocations)
                    .ThenInclude(x => x.SettlementItem)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == settlementId
                    && !x.IsDeleted
                    && x.MemberCompanyCode == company,
                    cancellationToken);
        }

        private async Task<SettlementExportLookups> LoadExportLookupsAsync(
            Settlement settlement,
            CancellationToken cancellationToken)
        {
            var supplierIds = settlement.Items.Select(item => item.SupplierId).Distinct().ToArray();
            var priceListIds = settlement.Items.Select(item => item.PriceListId).Distinct().ToArray();
            var suppliers = await _scopedUow.VPPContext.Set<Supplier>()
                .AsNoTracking()
                .Where(supplier => supplierIds.Contains(supplier.Id))
                .ToDictionaryAsync(
                    supplier => supplier.Id,
                    supplier => supplier.SupplierName ?? supplier.SupplierShortName ?? supplier.Id.ToString(),
                    cancellationToken);
            var priceLists = await _scopedUow.VPPContext.Set<PriceList>()
                .AsNoTracking()
                .Where(priceList => priceListIds.Contains(priceList.Id))
                .ToDictionaryAsync(
                    priceList => priceList.Id,
                    priceList => priceList.PriceListName ?? priceList.PriceListCode ?? priceList.Id.ToString(),
                    cancellationToken);
            return new SettlementExportLookups(suppliers, priceLists);
        }

        private async Task<Settlement?> FindIdempotentAsync(
            string company,
            string idempotencyKey,
            CancellationToken cancellationToken)
            => await SettlementQuery().FirstOrDefaultAsync(x => !x.IsDeleted
                && x.MemberCompanyCode == company
                && x.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);

        private static SettlementRevisionResDTO MatchIdempotent(
            Settlement entity,
            string commandHash)
        {
            if (!string.Equals(entity.CommandPayloadHash, commandHash, StringComparison.Ordinal))
            {
                throw new ConflictException("The idempotency key was already used for another settlement command.");
            }
            return MapRevision(entity);
        }

        private async Task ApplySupplierExceptionsAsync(
            SettlementPreviewResDTO response,
            IReadOnlyDictionary<Guid, decimal> totals,
            DateTime asOfUtc,
            CancellationToken cancellationToken)
        {
            var quote = response.PrimaryQuote!;
            foreach (var supplierException in response.Exceptions)
            {
                if (!supplierException.IsValid)
                {
                    continue;
                }
                if (supplierException.SupplierId == quote.SupplierId)
                {
                    supplierException.IsValid = false;
                    supplierException.Blocker = "EXCEPTION_MUST_USE_ANOTHER_SUPPLIER";
                    response.Blockers.Add($"EXCEPTION_MUST_USE_ANOTHER_SUPPLIER:{supplierException.VppId}");
                    continue;
                }
                if (_priceAsOfResolver is null)
                {
                    supplierException.IsValid = false;
                    supplierException.Blocker = "EXCEPTION_RESOLVER_UNAVAILABLE";
                    response.Blockers.Add($"EXCEPTION_RESOLVER_UNAVAILABLE:{supplierException.VppId}");
                    continue;
                }

                var resolved = await _priceAsOfResolver.ResolveAsync(new PriceResolutionReqDTO
                {
                    VppId = supplierException.VppId,
                    SupplierId = supplierException.SupplierId,
                    LockedPriceListId = supplierException.PriceListId,
                    PriceAsOfUtc = asOfUtc,
                    Quantity = totals[supplierException.VppId]
                }, cancellationToken);
                if (!resolved.IsResolved
                    || !resolved.PriceListId.HasValue
                    || !resolved.PriceBookItemId.HasValue
                    || !resolved.PriceListVersion.HasValue)
                {
                    supplierException.IsValid = false;
                    supplierException.Blocker = resolved.BlockerCode.ToString();
                    response.Blockers.Add($"SUPPLIER_EXCEPTION_UNRESOLVED:{supplierException.VppId}:{resolved.BlockerCode}");
                    continue;
                }

                supplierException.PriceListId = resolved.PriceListId;
                supplierException.PriceListVersion = resolved.PriceListVersion;
                supplierException.PriceBookItemId = resolved.PriceBookItemId;
                supplierException.NetUnitPrice = resolved.NetUnitPrice;
                supplierException.VatRate = resolved.VatRate;
                supplierException.NetAmount = resolved.NetAmount;
                supplierException.VatAmount = resolved.VatAmount;
                supplierException.GrossAmount = resolved.GrossAmount;
                var existingLine = quote.Lines.SingleOrDefault(line => line.VppId == supplierException.VppId);
                if (existingLine is not null)
                {
                    quote.Subtotal -= existingLine.NetAmount;
                    quote.VatAmount -= existingLine.VatAmount;
                    quote.Lines.Remove(existingLine);
                }
                quote.Lines.Add(new PriceBookQuoteLineResDTO
                {
                    VppId = supplierException.VppId,
                    Quantity = totals[supplierException.VppId],
                    NetUnitPrice = resolved.NetUnitPrice,
                    VatRate = resolved.VatRate,
                    NetAmount = resolved.NetAmount,
                    VatAmount = resolved.VatAmount,
                    GrossAmount = resolved.GrossAmount
                });
                if (quote.MissingVppIds.Remove(supplierException.VppId))
                {
                    quote.CoveredItemCount++;
                }
                quote.Subtotal += resolved.NetAmount;
                quote.VatAmount += resolved.VatAmount;
                quote.MaximumLeadTimeDays = Math.Max(quote.MaximumLeadTimeDays, resolved.LeadTimeDays);
            }

            quote.Blockers.RemoveAll(x => x.StartsWith("MISSING_ITEMS:", StringComparison.Ordinal));
            if (quote.MissingVppIds.Count > 0)
            {
                quote.Blockers.Add($"MISSING_ITEMS:{quote.MissingVppIds.Count}");
            }
            quote.Subtotal = PriceCalculationEngine.RoundMoney(quote.Subtotal);
            quote.VatAmount = PriceCalculationEngine.RoundMoney(quote.VatAmount);
            var terms = await _scopedUow.VPPContext.Set<PriceList>()
                .AsNoTracking()
                .Where(x => x.Id == quote.PriceListId)
                .Select(x => new { x.DiscountRate, x.RebateAmount, x.FeeAmount, x.ShippingAmount })
                .SingleAsync(cancellationToken);
            var basket = PriceCalculationEngine.CalculateBasket(
                quote.Subtotal,
                quote.VatAmount,
                terms.DiscountRate,
                terms.RebateAmount,
                terms.FeeAmount,
                terms.ShippingAmount);
            quote.DiscountAmount = basket.DiscountAmount;
            quote.GrandTotal = basket.GrandTotal;
            quote.CoveragePercent = quote.RequestedItemCount == 0
                ? 0m
                : decimal.Round(quote.CoveredItemCount * 100m / quote.RequestedItemCount, 2, MidpointRounding.AwayFromZero);
            quote.IsEligible = quote.MissingVppIds.Count == 0 && quote.Blockers.Count == 0;
        }

        private static List<SettlementFinancialAllocationResDTO> BuildPreviewAllocations(
            IReadOnlyList<VppRequest> headers,
            PriceBookQuoteResDTO quote)
        {
            var allocations = new List<SettlementFinancialAllocationResDTO>();
            foreach (var line in quote.Lines.OrderBy(item => item.VppId))
            {
                var sources = headers
                    .SelectMany(header => header.RequestDetails
                        .Where(detail => !detail.IsDeleted && detail.VppId == line.VppId)
                        .Select(detail => new { Header = header, Detail = detail }))
                    .OrderBy(source => source.Header.Id)
                    .ThenBy(source => source.Detail.Id)
                    .ToList();
                if (sources.Count == 0)
                {
                    continue;
                }

                var weights = sources.Select(source => (decimal)source.Detail.Qty).ToArray();
                var netShares = AllocateAmount(line.NetAmount, weights);
                var vatShares = AllocateAmount(line.VatAmount, weights);
                for (var index = 0; index < sources.Count; index++)
                {
                    var source = sources[index];
                    allocations.Add(new SettlementFinancialAllocationResDTO
                    {
                        RequestHeaderId = source.Header.Id,
                        RequestDetailId = source.Detail.Id,
                        VppId = line.VppId,
                        DepartmentCode = source.Header.DepartmentCode,
                        RequesterUserId = source.Header.CreatedByUserId,
                        Quantity = source.Detail.Qty,
                        NetAmount = netShares[index],
                        VatAmount = vatShares[index]
                    });
                }
            }

            if (allocations.Count == 0)
            {
                return allocations;
            }

            var commercialTotal = quote.GrandTotal - quote.Subtotal - quote.VatAmount;
            var commercialWeights = allocations.Select(allocation => allocation.NetAmount).ToArray();
            if (commercialWeights.Sum() == 0m)
            {
                commercialWeights = allocations.Select(allocation => allocation.Quantity).ToArray();
            }

            var commercialShares = AllocateAmount(commercialTotal, commercialWeights);
            for (var index = 0; index < allocations.Count; index++)
            {
                allocations[index].CommercialAdjustmentAmount = commercialShares[index];
                allocations[index].GrossAmount = allocations[index].NetAmount
                    + allocations[index].VatAmount
                    + commercialShares[index];
            }

            return allocations;
        }

        private static SnapshotPriceEvidence ResolvePrimaryEvidence(
            Guid vppId,
            decimal quantity,
            Guid supplierId,
            Guid priceListId,
            IReadOnlyDictionary<Guid, List<SupplierProductMapping>> rowsByVpp)
        {
            if (!rowsByVpp.TryGetValue(vppId, out var rows) || rows.Count == 0)
            {
                throw new ConflictException($"The selected price book no longer covers item {vppId}.");
            }
            if (rows.Count != 1)
            {
                throw new ConflictException($"The selected price book has ambiguous rows for item {vppId}.");
            }
            var row = rows[0];
            if (row.SupplierId != supplierId || row.PriceListId != priceListId)
            {
                throw new ConflictException($"Price ownership changed for item {vppId}.");
            }
            if (row.MinimumOrderQuantity > 0 && quantity < row.MinimumOrderQuantity)
            {
                throw new BusinessException($"Minimum order quantity for item {vppId} is {row.MinimumOrderQuantity}.");
            }
            return new SnapshotPriceEvidence(
                supplierId,
                priceListId,
                row.Id,
                row.NetPrice == 0m && row.Price != 0m ? row.Price : row.NetPrice,
                row.VatRate,
                row.MinimumOrderQuantity,
                row.LeadTimeDays);
        }

        private async Task<SnapshotPriceEvidence> ResolveExceptionEvidenceAsync(
            Guid vppId,
            decimal quantity,
            SettlementExceptionReqDTO supplierException,
            DateTime asOfUtc,
            CancellationToken cancellationToken)
        {
            if (_priceAsOfResolver is null)
            {
                throw new BusinessException("Supplier exceptions are unavailable.");
            }
            var resolved = await _priceAsOfResolver.ResolveAsync(new PriceResolutionReqDTO
            {
                VppId = vppId,
                SupplierId = supplierException.SupplierId,
                LockedPriceListId = supplierException.PriceListId,
                PriceAsOfUtc = asOfUtc,
                Quantity = quantity
            }, cancellationToken);
            if (!resolved.IsResolved || !resolved.PriceListId.HasValue || !resolved.PriceBookItemId.HasValue)
            {
                throw new ConflictException($"Supplier exception for item {vppId} no longer resolves: {resolved.BlockerCode}.");
            }
            return new SnapshotPriceEvidence(
                supplierException.SupplierId,
                resolved.PriceListId.Value,
                resolved.PriceBookItemId.Value,
                resolved.NetUnitPrice,
                resolved.VatRate,
                resolved.MinimumOrderQuantity,
                resolved.LeadTimeDays);
        }

        private static decimal[] AllocateAmount(decimal total, IReadOnlyList<decimal> weights)
        {
            if (weights.Count == 0)
            {
                return [];
            }
            var result = new decimal[weights.Count];
            var totalWeight = weights.Sum();
            var allocated = 0m;
            for (var index = 0; index < weights.Count - 1; index++)
            {
                result[index] = totalWeight == 0m
                    ? 0m
                    : PriceCalculationEngine.RoundMoney(total * weights[index] / totalWeight);
                allocated += result[index];
            }
            result[^1] = total - allocated;
            return result;
        }

        private static void AddCharge(
            Settlement settlement,
            string type,
            decimal amount,
            int userId,
            DateTime nowUtc)
            => settlement.Charges.Add(new SettlementCharge
            {
                Id = Guid.NewGuid(),
                SettlementId = settlement.Id,
                ChargeType = type,
                Amount = amount,
                AllocationBasis = "net-amount",
                CreatedByUserId = userId,
                CreatedAtUtc = nowUtc,
                UpdatedByUserId = userId,
                UpdatedAtUtc = nowUtc
            });

        private static void EnsureReconciled(Settlement settlement)
        {
            if (settlement.Items.Count == 0 || settlement.Allocations.Count == 0)
            {
                throw new BusinessException("Settlement requires item and allocation snapshots.");
            }
            if (settlement.Items.Where(x => !x.IsSupplierException)
                .Any(x => x.SupplierId != settlement.PrimarySupplierId || x.PriceListId != settlement.PriceListId))
            {
                throw new BusinessException("A settlement revision must use exactly one primary supplier price book.");
            }
            if (settlement.Subtotal != settlement.Items.Sum(x => x.NetAmount)
                || settlement.VatAmount != settlement.Items.Sum(x => x.VatAmount))
            {
                throw new BusinessException("Settlement item totals do not reconcile.");
            }
            var chargeTotal = settlement.Charges.Sum(x => x.Amount);
            if (settlement.GrandTotal != settlement.Subtotal + settlement.VatAmount + chargeTotal)
            {
                throw new BusinessException("Settlement charge totals do not reconcile.");
            }
            if (settlement.GrandTotal != settlement.Allocations.Sum(x => x.GrossAmount))
            {
                throw new BusinessException("Settlement allocations do not reconcile to the grand total.");
            }
        }

        private static SettlementRevisionResDTO MapRevision(Settlement entity)
            => new()
            {
                Id = entity.Id,
                PeriodId = entity.PeriodId,
                Year = entity.Year,
                Month = entity.Month,
                RevisionNumber = entity.RevisionNumber,
                IsCurrentRevision = entity.IsCurrentRevision,
                IsCorrection = entity.IsCorrection,
                SupersedesSettlementId = entity.SupersedesSettlementId,
                CorrectionReason = entity.CorrectionReason,
                PrimarySupplierId = entity.PrimarySupplierId,
                PrimarySupplierName = entity.PrimarySupplierName,
                PriceListId = entity.PriceListId,
                PriceListName = entity.PriceListName,
                PriceListVersion = entity.PriceListVersion,
                PriceAsOfUtc = entity.PriceAsOfUtc,
                CalculationVersion = entity.CalculationVersion,
                InputHash = entity.InputHash,
                CurrencyCode = entity.CurrencyCode,
                Subtotal = entity.Subtotal,
                DiscountAmount = entity.DiscountAmount,
                RebateAmount = entity.RebateAmount,
                FeeAmount = entity.FeeAmount,
                ShippingAmount = entity.ShippingAmount,
                VatAmount = entity.VatAmount,
                RoundingAdjustment = entity.RoundingAdjustment,
                GrandTotal = entity.GrandTotal,
                ConfirmedAtUtc = entity.ConfirmedAtUtc,
                ConfirmedByUserId = entity.ConfirmedByUserId,
                HasExternalProcurementImpact = entity.HasExternalProcurementImpact,
                RowVersion = entity.RowVersion,
                ItemCount = entity.Items.Count,
                AllocationCount = entity.Allocations.Count,
                Items = entity.Items
                    .OrderBy(item => item.VppName)
                    .ThenBy(item => item.VppCode)
                    .Select(item => new SettlementFinancialItemResDTO
                    {
                        VppId = item.VppId,
                        VppCode = item.VppCode,
                        VppName = item.VppName,
                        UomName = item.UomName,
                        Quantity = item.Quantity,
                        NetUnitPrice = item.NetUnitPrice,
                        VatRate = item.VatRate,
                        NetAmount = item.NetAmount,
                        VatAmount = item.VatAmount,
                        GrossAmount = item.GrossAmount
                    })
                    .ToList(),
                Allocations = entity.Allocations
                    .OrderBy(allocation => allocation.RequestHeaderId)
                    .ThenBy(allocation => allocation.RequestDetailId)
                    .Select(allocation => new SettlementFinancialAllocationResDTO
                    {
                        RequestHeaderId = allocation.RequestHeaderId,
                        RequestDetailId = allocation.RequestDetailId,
                        VppId = entity.Items
                            .Where(item => item.Id == allocation.SettlementItemId)
                            .Select(item => item.VppId)
                            .FirstOrDefault(),
                        DepartmentCode = allocation.DepartmentCode,
                        RequesterUserId = allocation.RequesterUserId,
                        Quantity = allocation.Quantity,
                        NetAmount = allocation.NetAmount,
                        VatAmount = allocation.VatAmount,
                        CommercialAdjustmentAmount = allocation.CommercialAdjustmentAmount
                            + allocation.RoundingAdjustment,
                        GrossAmount = allocation.GrossAmount
                    })
                    .ToList()
            };

        private static void ValidateConfirmRequest(SettlementConfirmReqDTO req)
        {
            ValidatePeriod(req.Year, req.Month);
            if (req.PrimarySupplierId == Guid.Empty || req.PriceListId == Guid.Empty)
            {
                throw new BusinessException("Primary supplier and price book are required.");
            }
            if (string.IsNullOrWhiteSpace(req.InputHash) || req.InputHash.Trim().Length != 64)
            {
                throw new BusinessException("A valid preview input hash is required.");
            }
            if (string.IsNullOrWhiteSpace(req.IdempotencyKey)
                || req.IdempotencyKey.Trim().Length is < 8 or > 128)
            {
                throw new BusinessException("Idempotency key must contain between 8 and 128 characters.");
            }
        }

        private static string ValidateReason(string? reason, string label)
        {
            var value = reason?.Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Length is < 5 or > 500)
            {
                throw new BusinessException($"{label} cần từ 5 đến 500 ký tự.");
            }
            return value;
        }

        private static void EnsureRowVersion(
            byte[]? actual,
            byte[]? expected,
            string aggregateName)
        {
            if (expected is not { Length: > 0 })
            {
                throw new BusinessException($"{aggregateName} RowVersion is required. Reload and try again.");
            }
            if (actual is null || !actual.AsSpan().SequenceEqual(expected))
            {
                throw new ConflictException($"The {aggregateName} changed. Reload and try again.");
            }
        }

        private static string ComputeCommandPayloadHash(
            SettlementConfirmReqDTO req,
            Guid? correctionSettlementId,
            string? correctionReason,
            DateTime asOfUtc)
        {
            var canonical = JsonSerializer.Serialize(new
            {
                Command = correctionSettlementId.HasValue ? "correct" : "confirm",
                correctionSettlementId,
                CorrectionReason = correctionReason,
                req.Year,
                req.Month,
                PriceAsOfUtc = asOfUtc,
                InputHash = req.InputHash.Trim().ToUpperInvariant(),
                req.PrimarySupplierId,
                req.PriceListId,
                Exceptions = (req.Exceptions ?? []).OrderBy(x => x.VppId).ThenBy(x => x.SupplierId)
                    .Select(x => new { x.VppId, x.SupplierId, x.PriceListId, Reason = x.Reason?.Trim() })
            });
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(canonical)));
        }

        private sealed record SnapshotPriceEvidence(
            Guid SupplierId,
            Guid PriceListId,
            Guid PriceBookItemId,
            decimal NetUnitPrice,
            decimal VatRate,
            decimal MinimumOrderQuantity,
            int LeadTimeDays);

        private sealed record SettlementExportLookups(
            IReadOnlyDictionary<Guid, string> SupplierNames,
            IReadOnlyDictionary<Guid, string> PriceListNames);

        private async Task<PriceList?> ResolvePriceListAsync(Guid? priceListId)
        {
            var query = _scopedUow.VPPContext.Set<PriceList>()
                .Where(x => !x.IsDeleted);

            return priceListId.HasValue
                ? await query.FirstOrDefaultAsync(x => x.Id == priceListId.Value)
                : await query.FirstOrDefaultAsync(x => x.IsDefault);
        }

        private async Task PopulateNamesAsync(IEnumerable<PeriodSettlementResDTO> rows)
        {
            var rowList = rows.ToList();
            var userIds = rowList
                .Select(x => x.SettledByUserId)
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();
            var listIds = rowList
                .Select(x => x.PriceListId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            var users = userIds.Length == 0
                ? new Dictionary<int, string?>()
                : await _scopedUow.VPPContext.Set<v_Users>()
                    .AsNoTracking()
                    .Where(x => userIds.Contains(x.UserID))
                    .Select(x => new { x.UserID, x.FullName })
                    .ToDictionaryAsync(x => x.UserID, x => x.FullName);

            var priceLists = listIds.Length == 0
                ? new Dictionary<Guid, string?>()
                : await _scopedUow.VPPContext.Set<PriceList>()
                    .AsNoTracking()
                    .Where(x => listIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.PriceListName })
                    .ToDictionaryAsync(x => x.Id, x => x.PriceListName);

            foreach (var row in rowList)
            {
                if (row.SettledByUserId.HasValue && users.TryGetValue(row.SettledByUserId.Value, out var userName))
                {
                    row.SettledByUserName = userName;
                }

                if (row.PriceListId.HasValue && priceLists.TryGetValue(row.PriceListId.Value, out var priceListName))
                {
                    row.PriceListName = priceListName;
                }
            }
        }

        private static void ValidatePeriod(int y, int m)
        {
            if (y < 1900 || y > 9999)
            {
                throw new BusinessException($"Invalid year {y}.");
            }

            if (m < 1 || m > 12)
            {
                throw new BusinessException($"Invalid month {m}.");
            }
        }

        private static string ComputeInputHash(
            SettlementPreviewReqDTO req,
            DateTime asOfUtc,
            IReadOnlyDictionary<Guid, decimal> totals,
            Guid? effectiveSupplierId,
            Guid? effectivePriceListId)
        {
            var canonical = JsonSerializer.Serialize(new
            {
                req.Year,
                req.Month,
                PriceAsOfUtc = asOfUtc,
                PriceListId = effectivePriceListId ?? req.PriceListId,
                PrimarySupplierId = effectiveSupplierId ?? req.PrimarySupplierId,
                Items = totals.OrderBy(x => x.Key).Select(x => new { VppId = x.Key, Quantity = x.Value }),
                Exceptions = (req.Exceptions ?? []).OrderBy(x => x.VppId).ThenBy(x => x.SupplierId)
                    .Select(x => new { x.VppId, x.SupplierId, x.PriceListId, Reason = x.Reason?.Trim() })
            });
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(canonical)));
        }

        private static DateTime NormalizeUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
