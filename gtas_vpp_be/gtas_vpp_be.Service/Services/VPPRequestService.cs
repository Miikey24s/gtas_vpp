using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Service.Services
{
    public interface IVPPRequestService
    {
        Task<List<VppRequestResDTO>> GetMyOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses);
        Task<List<VppRequestResDTO>> GetMyOrdersSummaryAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses);
        Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetMyOrdersSummaryPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top);
        Task<VppRequestResDTO?> GetOrderByIdAsync(Guid id);
        Task<VppRequestResDTO> CreateOrderAsync(VppRequestCreateReqDTO req, int createdByUserId, string departmentCode, string memberCompanyCode);
        Task<VppRequestResDTO> UpdateOrderAsync(VppRequestUpdateReqDTO req);
        Task CancelOrderAsync(Guid id, int userId, VppRequestCancelReqDTO req);
        Task<VppRequestResDTO?> GetPreviousOrderItemsAsync(int userId);
        Task<VppPeriodInfoResDTO> GetCurrentPeriodInfoAsync(int userId);
        Task<VppRequestHistoryResDTO?> GetOrderHistoryAsync(Guid id);
        Task<List<VppRequestResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode, string? memberCompanyCode = null);
        Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetAllOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top, string? memberCompanyCode = null);
        Task<List<VppRequestResDTO>> GetDepartmentOrdersAsync(int? year, int? month, int? status, string? departmentCode, string? memberCompanyCode = null);
        Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetDepartmentOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top, string? memberCompanyCode = null);
        Task<List<VppRequestResDTO>> GetPendingAdditionalOrdersAsync(
            string? memberCompanyCode = null,
            string? departmentCode = null,
            bool canViewAllDepartments = false);
        Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetPendingAdditionalOrdersPagedAsync(
            int? skip,
            int? top,
            string? memberCompanyCode = null,
            string? departmentCode = null,
            bool canViewAllDepartments = false);
        Task ApproveAdditionalOrderAsync(Guid id, int adminId, byte[] rowVersion, string? idempotencyKey, string? actorDepartmentCode, bool canApproveCrossDepartment, string? memberCompanyCode);
        Task RejectAdditionalOrderAsync(Guid id, int adminId, string? reason, byte[] rowVersion, string? idempotencyKey, string? actorDepartmentCode, bool canApproveCrossDepartment, string? memberCompanyCode);
    }
    
    public class VPPRequestService : BaseServices, IVPPRequestService
    {
        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly PeriodCalculator _periodCalculator;
        private readonly int _deadlineDay;
        private readonly VppRequestPolicy _policy;
        private readonly IVppPeriodService? _periodService;

        public VPPRequestService(
            IUnitOfWorkFactory uowFactory,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IConfiguration config,
            IEnvironmentResolver environmentResolver,
            IUserNameResolver userNameResolver,
            ILogger<BaseServices> baseLogger,
            IOptions<JiraSettings> jiraSettings,
            PeriodCalculator? periodCalculator = null,
            VppRequestPolicy? policy = null,
            IVppPeriodService? periodService = null)
            : base(uowFactory, httpContextAccessor, environmentResolver, userNameResolver, baseLogger, jiraSettings)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _deadlineDay = config.GetValue<int>("VPPDeadlineDay", 5);
            _periodCalculator = periodCalculator ?? new PeriodCalculator(_deadlineDay);
            _policy = policy ?? VppRequestPolicy.FromConfiguration(config);
            _periodService = periodService;
        }

        public async Task<List<VppRequestResDTO>> GetMyOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
        }

        public async Task<List<VppRequestResDTO>> GetMyOrdersSummaryAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
        }

        public async Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetMyOrdersSummaryPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top)
        {
            return await GetFilteredOrdersPagedAsync(userId, years, months, statuses, skip, top);
        }

        private async Task<List<VppRequestResDTO>> GetFilteredOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            var yearFilter = years?.Distinct().ToArray();
            var monthFilter = months?.Distinct().ToArray();
            var statusFilter = statuses?.Distinct().ToArray();

            var query = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => x.CreatedByUserId == userId && !x.IsDeleted && x.IsCurrentRevision);

            if (yearFilter is { Length: > 0 }) query = query.Where(x => yearFilter.Contains(x.Year));
            if (monthFilter is { Length: > 0 }) query = query.Where(x => monthFilter.Contains(x.Month));
            if (statusFilter is { Length: > 0 }) query = query.Where(x => statusFilter.Contains(x.Status));

            var result = await query
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.SubmittedDate ?? x.UpdatedAtUtc)
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        private async Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetFilteredOrdersPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top)
        {
            var yearFilter = years?.Distinct().ToArray();
            var monthFilter = months?.Distinct().ToArray();
            var statusFilter = statuses?.Distinct().ToArray();

            var query = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => x.CreatedByUserId == userId && !x.IsDeleted && x.IsCurrentRevision);

            if (yearFilter is { Length: > 0 }) query = query.Where(x => yearFilter.Contains(x.Year));
            if (monthFilter is { Length: > 0 }) query = query.Where(x => monthFilter.Contains(x.Month));
            if (statusFilter is { Length: > 0 }) query = query.Where(x => statusFilter.Contains(x.Status));

            // Aggregate stats in a single DB query
            var stats = await query.Select(x => new
            {
                Lines = x.RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var totalCount = stats?.TotalCount ?? 0;
            var totalLines = stats?.TotalLines ?? 0;
            var totalQty = stats?.TotalQty ?? 0;

            var orderedQuery = query
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.SubmittedDate ?? x.UpdatedAtUtc);

            IQueryable<VppRequest> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, totalCount, totalLines, totalQty);
        }

        public async Task<VppRequestResDTO?> GetOrderByIdAsync(Guid id)
        {
            var data = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (data is not null)
            {
                await ApplyRequesterNamesAsync(new List<VppRequestResDTO> { data });
                ApplyPeriodFlags(new[] { data });
            }

            return data;
        }

        public async Task<VppRequestResDTO> CreateOrderAsync(VppRequestCreateReqDTO req, int createdByUserId, string departmentCode, string memberCompanyCode)
        {
            ValidateItems(req.Items);
            ValidateRequestedPeriod(req);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var now = _dateTimeProvider.Now;
                var requestedPeriod = new Period(req.Year, req.Month);
                var currentPeriod = _periodCalculator.Current(now);
                if (requestedPeriod != currentPeriod)
                {
                    var kind = req.IsAdditionalOrder ? "Additional" : "Regular";
                    throw new BusinessException(
                        $"{kind} orders can only target the current period {currentPeriod.Year:D4}-{currentPeriod.Month:D2}.");
                }

                var period = await EnsurePeriodAsync(memberCompanyCode, requestedPeriod);
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                if (period.State != VppPeriodState.Open || nowUtc >= period.SubmissionDeadlineUtc)
                    throw new BusinessException("The submission window for this period is closed.");

                var requestSet = _scopedUow.VPPContext.Set<VppRequest>();
                // Keep the write guard compatible with pre-period rows while the
                // additive migration/backfill is rolling out. A settled company
                // period must win over user-level duplicate/base validation so the
                // caller receives the real immutable-period conflict.
                var settled = await requestSet.AsNoTracking().AnyAsync(x =>
                    x.MemberCompanyCode == memberCompanyCode
                    && (x.PeriodId == period.Id
                        || (x.PeriodId == null && x.Year == req.Year && x.Month == req.Month))
                    && !x.IsDeleted && x.SettledAt != null);
                if (settled || period.State is VppPeriodState.Pricing or VppPeriodState.Settled)
                    throw new BusinessException("This period has entered pricing/settlement and cannot accept new requests.");

                var payloadHash = ComputePayloadHash(req);
                var createKey = NormalizeIdempotencyKey(req.IdempotencyKey);
                if (!string.IsNullOrWhiteSpace(createKey))
                {
                    var replay = await requestSet.AsNoTracking()
                        .Where(x => x.CreatedByUserId == createdByUserId
                                 && x.MemberCompanyCode == memberCompanyCode
                                 && x.IdempotencyKey == createKey
                                 && !x.IsDeleted)
                        .OrderByDescending(x => x.RevisionNumber)
                        .FirstOrDefaultAsync();
                    if (replay is not null)
                    {
                        if (!string.Equals(replay.CommandPayloadHash, payloadHash, StringComparison.Ordinal))
                            throw new ConflictException("The idempotency key was already used for a different request.");

                        await _scopedUow.RollbackAsync();
                        return (await GetOrderByIdAsync(replay.Id))!;
                    }
                }

                VppRequest? baseRequest = null;
                var approvedSupplementCount = 0;
                var supplementAttemptCount = 0;
                if (req.IsAdditionalOrder)
                {
                    if (string.IsNullOrWhiteSpace(req.SupplementReason)
                        || req.SupplementReason.Trim().Length < 5
                        || req.SupplementReason.Trim().Length > 500)
                    {
                        throw new BusinessException("A supplement reason of 5 to 500 characters is required.");
                    }

                baseRequest = await requestSet
                        .FirstOrDefaultAsync(x => x.CreatedByUserId == createdByUserId
                            && x.MemberCompanyCode == memberCompanyCode
                            && x.Year == req.Year && x.Month == req.Month
                            && !x.IsAdditionalOrder && !x.IsDeleted
                            && x.IsCurrentRevision
                            && (req.BaseRequestId == null || x.Id == req.BaseRequestId));
                    if (baseRequest is null)
                        throw new BusinessException("A current regular order is required before creating a supplement.");
                    if (baseRequest.Status is not ((int)VPPStatus.Submitted) and not ((int)VPPStatus.Approved))
                        throw new BusinessException("The regular order is not eligible as a supplement base.");

                    approvedSupplementCount = await requestSet.AsNoTracking()
                        .CountAsync(x => x.CreatedByUserId == createdByUserId
                            && x.MemberCompanyCode == memberCompanyCode
                            && (x.PeriodId == period.Id
                                || (x.PeriodId == null && x.Year == req.Year && x.Month == req.Month))
                            && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                                || x.BaseRequestSeriesId == null)
                            && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted
                            && x.Status == (int)VPPStatus.Approved);
                    if (approvedSupplementCount >= _policy.MaxApprovedSupplements)
                        throw new BusinessException("The approved supplement quota for this regular order is full.");

                    supplementAttemptCount = await requestSet.AsNoTracking()
                        .CountAsync(x => x.CreatedByUserId == createdByUserId
                            && x.MemberCompanyCode == memberCompanyCode
                            && (x.PeriodId == period.Id
                                || (x.PeriodId == null && x.Year == req.Year && x.Month == req.Month))
                            && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                                || x.BaseRequestSeriesId == null)
                            && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted);
                    if (supplementAttemptCount >= _policy.MaxSupplementAttempts)
                        throw new BusinessException("The supplement attempt limit for this regular order is full.");

                    var hasPending = await requestSet.AsNoTracking().AnyAsync(x =>
                        x.CreatedByUserId == createdByUserId
                        && x.MemberCompanyCode == memberCompanyCode
                        && (x.PeriodId == period.Id
                            || (x.PeriodId == null && x.Year == req.Year && x.Month == req.Month))
                        && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                            || x.BaseRequestSeriesId == null)
                        && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted
                        && x.Status == (int)VPPStatus.Pending);
                    if (hasPending)
                        throw new BusinessException("Resolve the existing pending supplement before creating another one.");
                }
                else
                {
                    var hasExisting = await requestSet.AsNoTracking().AnyAsync(x =>
                        x.CreatedByUserId == createdByUserId
                        && x.MemberCompanyCode == memberCompanyCode
                        && (x.PeriodId == period.Id
                            || (x.PeriodId == null && x.Year == req.Year && x.Month == req.Month))
                        && !x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted);
                    if (hasExisting)
                        throw new ConflictException("Bạn đã có đơn cho kỳ này.");
                }

                var header = new VppRequest
                {
                    Id = Guid.NewGuid(),
                    Year = req.Year,
                    Month = req.Month,
                    PeriodId = period.Id,
                    RequestSeriesId = Guid.NewGuid(),
                    RevisionNumber = 1,
                    IsCurrentRevision = true,
                    VppCode = GenerateVppCode(req.Year, req.Month),
                    Status = req.IsAdditionalOrder ? (int)VPPStatus.Pending : (int)VPPStatus.Submitted,
                    Description = req.IsAdditionalOrder ? req.SupplementReason?.Trim() : req.Description,
                    IsAdditionalOrder = req.IsAdditionalOrder,
                    BaseRequestId = baseRequest?.Id,
                    BaseRequestSeriesId = baseRequest?.RequestSeriesId,
                    SupplementAttemptNumber = req.IsAdditionalOrder ? supplementAttemptCount + 1 : null,
                    SupplementSequence = req.IsAdditionalOrder ? supplementAttemptCount + 1 : null,
                    SupplementReason = req.IsAdditionalOrder ? req.SupplementReason?.Trim() : null,
                    IdempotencyKey = string.IsNullOrWhiteSpace(createKey) ? null : createKey,
                    CommandPayloadHash = payloadHash,
                    DepartmentCode = departmentCode,
                    MemberCompanyCode = memberCompanyCode,
                    CreatedByUserId = createdByUserId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = createdByUserId,
                    UpdatedAtUtc = now,
                    SubmittedDate = now
                };

                await ValidateActiveProductsAsync(req.Items);
                header.RequestDetails = await BuildRequestDetailsAsync(req.Items, createdByUserId, header.Id, now);

                requestSet.Add(header);
                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "CREATE",
                    Action = "CREATE",
                    ActorUserId = createdByUserId,
                    MemberCompanyCode = memberCompanyCode,
                    RevisionNumber = header.RevisionNumber,
                    Reason = header.SupplementReason,
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.RequestDetails))
                });
                await _scopedUow.CommitAsync();

                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(
                    req.IsAdditionalOrder
                        ? "A concurrent supplement or quota change won this request."
                        : "Bạn đã có đơn cho kỳ này.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<VppRequestResDTO> UpdateOrderAsync(VppRequestUpdateReqDTO req)
            => await UpdateOrderRevisionAsync(req);

        private async Task<VppRequestResDTO> UpdateOrderRevisionAsync(VppRequestUpdateReqDTO req)
        {
            ValidateItems(req.Items);
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var requestSet = _scopedUow.VPPContext.Set<VppRequest>();
                var payloadHash = ComputePayloadHash(req);
                var updateKey = NormalizeIdempotencyKey(req.IdempotencyKey);
                if (!string.IsNullOrWhiteSpace(updateKey))
                {
                    var replay = await requestSet.AsNoTracking()
                        .Where(x => x.CreatedByUserId == req.UpdatedByUserId
                                 && x.IdempotencyKey == updateKey
                                 && !x.IsDeleted)
                        .OrderByDescending(x => x.RevisionNumber)
                        .FirstOrDefaultAsync();
                    if (replay is not null)
                    {
                        if (!string.Equals(replay.CommandPayloadHash, payloadHash, StringComparison.Ordinal))
                            throw new ConflictException("The idempotency key was already used for a different update.");
                        await _scopedUow.RollbackAsync();
                        return (await GetOrderByIdAsync(replay.Id))!;
                    }
                }

                var header = await requestSet
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted && x.IsCurrentRevision);
                if (header is null)
                {
                    var superseded = await requestSet.AsNoTracking()
                        .AnyAsync(x => x.Id == req.Id && !x.IsDeleted);
                    if (superseded)
                        throw new ConflictException(
                            "The request changed while you were editing it. Refresh and try again.");

                    throw new KeyNotFoundException("Order not found.");
                }
                if (header.CreatedByUserId != req.UpdatedByUserId)
                    throw new UnauthorizedAccessException("Cannot update another user's order.");
                EnsureExpectedRowVersion(header.RowVersion, req.RowVersion);

                var period = await EnsurePeriodAsync(
                    header.MemberCompanyCode ?? string.Empty, new Period(header.Year, header.Month));
                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                var deadline = header.IsAdditionalOrder
                    ? period.SupplementApprovalDeadlineUtc
                    : period.SubmissionDeadlineUtc;
                var stateBlocksMutation = header.IsAdditionalOrder
                    ? period.State is VppPeriodState.Pricing or VppPeriodState.Settled
                    : period.State != VppPeriodState.Open;
                if (stateBlocksMutation
                    || nowUtc >= deadline || header.SettledAt is not null)
                    throw new ConflictException("This request is immutable because its period is closed or settled.");

                var replacingCancelledRegular = !header.IsAdditionalOrder
                    && header.Status == (int)VPPStatus.Cancelled;
                if (!replacingCancelledRegular
                    && (header.IsAdditionalOrder
                        ? header.Status != (int)VPPStatus.Pending
                        : header.Status != (int)VPPStatus.Submitted))
                    throw new BusinessException("This request cannot be edited in its current status.");

                var supplementReason = header.IsAdditionalOrder
                    ? (req.SupplementReason ?? req.Description)?.Trim()
                    : null;
                if (header.IsAdditionalOrder
                    && (string.IsNullOrWhiteSpace(supplementReason)
                        || supplementReason.Length < 5 || supplementReason.Length > 500))
                    throw new BusinessException("A supplement reason of 5 to 500 characters is required.");

                var replacement = new VppRequest
                {
                    Id = Guid.NewGuid(),
                    Year = header.Year,
                    Month = header.Month,
                    PeriodId = header.PeriodId ?? period.Id,
                    RequestSeriesId = header.RequestSeriesId == Guid.Empty
                        ? Guid.NewGuid() : header.RequestSeriesId,
                    RevisionNumber = header.RevisionNumber + 1,
                    IsCurrentRevision = true,
                    SupersedesRequestId = header.Id,
                    VppCode = GenerateVppCode(header.Year, header.Month),
                    Status = header.IsAdditionalOrder
                        ? (int)VPPStatus.Pending : (int)VPPStatus.Submitted,
                    Description = header.IsAdditionalOrder ? supplementReason : req.Description,
                    IsAdditionalOrder = header.IsAdditionalOrder,
                    BaseRequestId = header.BaseRequestId,
                    BaseRequestSeriesId = header.BaseRequestSeriesId,
                    SupplementSequence = header.SupplementSequence,
                    SupplementAttemptNumber = header.SupplementAttemptNumber,
                    SupplementReason = supplementReason,
                    DepartmentCode = header.DepartmentCode,
                    MemberCompanyCode = header.MemberCompanyCode,
                    CreatedByUserId = header.CreatedByUserId,
                    CreatedAtUtc = header.CreatedAtUtc,
                    UpdatedByUserId = req.UpdatedByUserId,
                    UpdatedAtUtc = now,
                    SubmittedDate = now,
                    IdempotencyKey = string.IsNullOrWhiteSpace(updateKey)
                        ? null : updateKey,
                    CommandPayloadHash = payloadHash
                };

                header.IsCurrentRevision = false;
                header.SupersededByRequestId = replacement.Id;
                header.UpdatedByUserId = req.UpdatedByUserId;
                header.UpdatedAtUtc = now;

                await ValidateActiveProductsAsync(req.Items);
                replacement.RequestDetails = await BuildRequestDetailsAsync(
                    req.Items, req.UpdatedByUserId, replacement.Id, now);
                requestSet.Add(replacement);
                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = replacement.Id,
                    LogTitle = replacingCancelledRegular ? "REPLACE" : "UPDATE",
                    Action = replacingCancelledRegular ? "REPLACE" : "UPDATE",
                    ActorUserId = req.UpdatedByUserId,
                    MemberCompanyCode = replacement.MemberCompanyCode,
                    RevisionNumber = replacement.RevisionNumber,
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(
                        replacement, replacement.RequestDetails))
                });
                await _scopedUow.CommitAsync();
                return (await GetOrderByIdAsync(replacement.Id))!;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(
                    "The request changed while you were editing it. Refresh and try again.", ex);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("Another revision won this update.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private async Task<VppRequestResDTO> UpdateOrderLegacyAsync(VppRequestUpdateReqDTO req)
        {
            ValidateItems(req.Items);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VppRequest>()
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreatedByUserId != req.UpdatedByUserId) throw new UnauthorizedAccessException("Cannot update another user's order.");
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Year, header.Month))
                    throw new InvalidOperationException("Deadline has passed.");

                var now = _dateTimeProvider.Now;

                header.Status = TransitionStatus(header.Status, OrderAction.Update, "Cannot edit order in this status.");
                header.Description = req.Description;
                header.UpdatedByUserId = req.UpdatedByUserId;
                header.UpdatedAtUtc = now;
                header.SubmittedDate ??= now;

                await _scopedUow.VPPContext.Set<VppRequestDetail>()
                    .Where(x => x.RequestId == header.Id && !x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdatedByUserId, req.UpdatedByUserId)
                        .SetProperty(x => x.UpdatedAtUtc, now));

                await ValidateActiveProductsAsync(req.Items);
                var newDetails = await BuildRequestDetailsAsync(req.Items, req.UpdatedByUserId, header.Id, now);

                _scopedUow.VPPContext.Set<VppRequestDetail>().AddRange(newDetails);
                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "UPDATE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, newDetails))
                });

                await _scopedUow.CommitAsync();
                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task CancelOrderAsync(Guid id, int userId, VppRequestCancelReqDTO req)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var requestSet = _scopedUow.VPPContext.Set<VppRequest>();
                var key = NormalizeIdempotencyKey(req.IdempotencyKey);
                var cancellationHash = ComputeCancellationHash(req);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    var replay = await requestSet.AsNoTracking()
                        .Where(x => x.CreatedByUserId == userId
                                 && x.IdempotencyKey == key && !x.IsDeleted)
                        .OrderByDescending(x => x.RevisionNumber)
                        .FirstOrDefaultAsync();
                    if (replay is not null)
                    {
                        if (replay.Status != (int)VPPStatus.Cancelled
                            || !string.Equals(replay.CommandPayloadHash, cancellationHash, StringComparison.Ordinal))
                            throw new ConflictException("The idempotency key was already used for a different command.");
                        await _scopedUow.RollbackAsync();
                        return;
                    }
                }

                var header = await requestSet
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsCurrentRevision);
                if (header is null)
                    throw new KeyNotFoundException("Order not found or no longer current.");
                if (header.CreatedByUserId != userId)
                    throw new UnauthorizedAccessException("Cannot cancel another user's order.");
                EnsureExpectedRowVersion(header.RowVersion, req.RowVersion);

                var period = await EnsurePeriodAsync(
                    header.MemberCompanyCode ?? string.Empty, new Period(header.Year, header.Month));
                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                var deadline = header.IsAdditionalOrder
                    ? period.SupplementApprovalDeadlineUtc : period.SubmissionDeadlineUtc;
                var stateBlocksMutation = header.IsAdditionalOrder
                    ? period.State is VppPeriodState.Pricing or VppPeriodState.Settled
                    : period.State != VppPeriodState.Open;
                if (stateBlocksMutation
                    || nowUtc >= deadline || header.SettledAt is not null)
                    throw new ConflictException("This request is immutable because its period is closed or settled.");
                if (header.Status is not ((int)VPPStatus.Submitted) and not ((int)VPPStatus.Pending))
                    throw new BusinessException("Only submitted or pending requests can be cancelled.");

                var cancellation = CloneHeaderForLifecycle(
                    header, Guid.NewGuid(), now, userId, (int)VPPStatus.Cancelled);
                cancellation.CancelledById = userId;
                cancellation.CancelledAt = now;
                cancellation.CancelReason = string.IsNullOrWhiteSpace(req.Reason)
                    ? null : req.Reason.Trim();
                cancellation.IdempotencyKey = key;
                cancellation.CommandPayloadHash = cancellationHash;

                header.IsCurrentRevision = false;
                header.SupersededByRequestId = cancellation.Id;
                header.UpdatedByUserId = userId;
                header.UpdatedAtUtc = now;
                cancellation.RequestDetails = CloneDetails(
                    header.RequestDetails, cancellation.Id, userId, now);
                requestSet.Add(cancellation);
                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = cancellation.Id,
                    LogTitle = "CANCEL",
                    Action = "CANCEL",
                    ActorUserId = userId,
                    MemberCompanyCode = cancellation.MemberCompanyCode,
                    RevisionNumber = cancellation.RevisionNumber,
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(
                        cancellation, cancellation.RequestDetails))
                });
                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("The request changed while you were cancelling it.", ex);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("Another lifecycle command won this request.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private Task CancelOrderLegacyAsync(Guid id, int userId)
            => Task.CompletedTask;

        public async Task<VppRequestResDTO?> GetPreviousOrderItemsAsync(int userId)
        {
            var previous = _periodCalculator.Previous(_dateTimeProvider.Now);
            var prevHeader = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Include(x => x.RequestDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(x => x.CreatedByUserId == userId
                    && x.Year == previous.Year && x.Month == previous.Month
                    && !x.IsDeleted && x.IsCurrentRevision && !x.IsAdditionalOrder
                    && x.Status != (int)VPPStatus.Cancelled);
            if (prevHeader is null)
                return null;

            var activeVppIds = await _scopedUow.VPPContext.Set<VppItem>()
                .AsNoTracking().Where(x => !x.IsDeleted).Select(x => x.Id).ToListAsync();
            var result = prevHeader.Adapt<VppRequestResDTO>();
            result.Items = result.Items.Where(i => activeVppIds.Contains(i.VppId)).ToList();
            await ApplyRequesterNamesAsync(new List<VppRequestResDTO> { result });
            ApplyPeriodFlags(new[] { result });
            return result;
        }

        public async Task<VppRequestHistoryResDTO?> GetOrderHistoryAsync(Guid id)
        {
            var requestSet = _scopedUow.VPPContext.Set<VppRequest>();
            var seed = await requestSet.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (seed is null)
                return null;
            var seriesId = seed.RequestSeriesId == Guid.Empty ? seed.Id : seed.RequestSeriesId;
            var revisions = await requestSet.AsNoTracking()
                .Where(x => !x.IsDeleted && (x.RequestSeriesId == seriesId || x.Id == seed.Id))
                .OrderBy(x => x.RevisionNumber).ThenBy(x => x.CreatedAtUtc)
                .ProjectToType<VppRequestResDTO>().AsSplitQuery().ToListAsync();
            await ApplyRequesterNamesAsync(revisions);
            ApplyPeriodFlags(revisions);
            var requestIds = revisions.Select(x => x.Id).ToArray();
            var logs = await _scopedUow.VPPContext.Set<RequestLog>().AsNoTracking()
                .Where(x => requestIds.Contains(x.RequestId))
                .OrderBy(x => x.LogDate).ToListAsync();
            var actorIds = logs.Where(x => x.ActorUserId.HasValue)
                .Select(x => x.ActorUserId!.Value).Distinct().ToArray();
            var actors = actorIds.Length == 0 ? new Dictionary<int, string?>()
                : await _scopedUow.VPPContext.Set<v_Users>().AsNoTracking()
                    .Where(x => actorIds.Contains(x.UserID))
                    .Select(x => new { x.UserID, x.FullName })
                    .ToDictionaryAsync(x => x.UserID, x => x.FullName);
            return new VppRequestHistoryResDTO
            {
                RequestSeriesId = seriesId,
                CurrentRequestId = revisions.FirstOrDefault(x => x.IsCurrentRevision)?.Id
                    ?? revisions.LastOrDefault()?.Id ?? id,
                Revisions = revisions,
                Timeline = logs.Select(x => new VppRequestTimelineEventResDTO
                {
                    Id = x.Id,
                    RequestId = x.RequestId,
                    Action = x.Action ?? x.LogTitle ?? "EVENT",
                    OccurredAt = x.LogDate,
                    ActorUserId = x.ActorUserId,
                    ActorName = x.ActorUserId.HasValue
                        && actors.TryGetValue(x.ActorUserId.Value, out var name) ? name : null,
                    RevisionNumber = x.RevisionNumber,
                    Reason = x.Reason
                }).ToList()
            };
        }

        public async Task<List<VppRequestResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode, string? memberCompanyCode = null)
        {
            var result = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                     && (year == null || x.Year == year)
                     && (month == null || x.Month == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode)
                     && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode))
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty, long TotalAmount)> GetAllOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top, string? memberCompanyCode = null)
        {
            var query = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                     && (year == null || x.Year == year)
                     && (month == null || x.Month == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode)
                     && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode));

            var stats = await query.Select(x => new
            {
                Lines = x.RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0,
                Amount = x.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (long?)(d.Qty * d.CurrentSinglePrice)) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty),
                TotalAmount = g.Sum(x => x.Amount)
            }).FirstOrDefaultAsync();

            var orderedQuery = query
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ThenByDescending(x => x.UpdatedAtUtc);
            IQueryable<VppRequest> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();
            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, stats?.TotalCount ?? 0, stats?.TotalLines ?? 0,
                stats?.TotalQty ?? 0, stats?.TotalAmount ?? 0);
        }

        public async Task<List<VppRequestResDTO>> GetDepartmentOrdersAsync(int? year, int? month, int? status, string? departmentCode, string? memberCompanyCode = null)
        {
            var result = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                     && (year == null || x.Year == year)
                     && (month == null || x.Month == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode)
                     && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode))
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();
            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetDepartmentOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top, string? memberCompanyCode = null)
        {
            var query = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                     && (year == null || x.Year == year)
                     && (month == null || x.Month == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode)
                     && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode));
            var stats = await query.Select(x => new
            {
                Lines = x.RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(), TotalLines = g.Sum(x => x.Lines), TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();
            var orderedQuery = query.OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month).ThenByDescending(x => x.UpdatedAtUtc);
            IQueryable<VppRequest> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);
            var result = await pagedQuery.ProjectToType<VppRequestResDTO>()
                .AsSplitQuery().ToListAsync();
            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, stats?.TotalCount ?? 0, stats?.TotalLines ?? 0, stats?.TotalQty ?? 0);
        }

        public async Task<List<VppRequestResDTO>> GetPendingAdditionalOrdersAsync(
            string? memberCompanyCode = null,
            string? departmentCode = null,
            bool canViewAllDepartments = false)
        {
            var result = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                     && x.IsAdditionalOrder
                    && x.Status == (int)VPPStatus.Pending
                    && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode)
                    && (canViewAllDepartments
                        || (!string.IsNullOrEmpty(departmentCode)
                            && x.DepartmentCode == departmentCode)))
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VppRequestResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetPendingAdditionalOrdersPagedAsync(
            int? skip,
            int? top,
            string? memberCompanyCode = null,
            string? departmentCode = null,
            bool canViewAllDepartments = false)
        {
            var query = _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsCurrentRevision
                    && x.IsAdditionalOrder
                    && x.Status == (int)VPPStatus.Pending
                    && (string.IsNullOrEmpty(memberCompanyCode) || x.MemberCompanyCode == memberCompanyCode)
                    && (canViewAllDepartments
                        || (!string.IsNullOrEmpty(departmentCode)
                            && x.DepartmentCode == departmentCode)));

            var stats = await query.Select(x => new
            {
                Lines = x.RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var orderedQuery = query.OrderByDescending(x => x.UpdatedAtUtc);

            IQueryable<VppRequest> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VppRequestResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, stats?.TotalCount ?? 0, stats?.TotalLines ?? 0, stats?.TotalQty ?? 0);
        }

        public async Task ApproveAdditionalOrderAsync(
            Guid id,
            int adminId,
            byte[] rowVersion,
            string? idempotencyKey,
            string? actorDepartmentCode,
            bool canApproveCrossDepartment,
            string? memberCompanyCode)
            => await ApproveAdditionalOrderCoreAsync(
                id, adminId, rowVersion, idempotencyKey, actorDepartmentCode,
                canApproveCrossDepartment, memberCompanyCode);

        private async Task ApproveAdditionalOrderCoreAsync(
            Guid id,
            int adminId,
            byte[]? rowVersion,
            string? idempotencyKey,
            string? actorDepartmentCode,
            bool canApproveCrossDepartment,
            string? memberCompanyCode)
        {
            var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var context = _scopedUow.VPPContext;
                if (context.Database.IsSqlServer())
                    await context.Database.ExecuteSqlRawAsync(
                        "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");

                var requestSet = context.Set<VppRequest>();
                var header = await requestSet
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted
                        && x.IsCurrentRevision && x.IsAdditionalOrder);
                if (header is null)
                    throw new KeyNotFoundException("Order not found or not an additional order.");
                EnsureDecisionScope(header, adminId, actorDepartmentCode,
                    canApproveCrossDepartment, memberCompanyCode);

                var replay = await HasDecisionReplayAsync(id, "APPROVE", normalizedKey, null);
                if (replay)
                {
                    await _scopedUow.RollbackAsync();
                    return;
                }
                if (header.Status == (int)VPPStatus.Approved)
                {
                    await _scopedUow.RollbackAsync();
                    return; // safe retry without a command key
                }
                EnsureExpectedRowVersion(header.RowVersion, rowVersion);
                if (header.Status != (int)VPPStatus.Pending)
                    throw new BusinessException("The supplement is no longer pending.");

                var period = await EnsurePeriodAsync(
                    header.MemberCompanyCode ?? string.Empty, new Period(header.Year, header.Month));
                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                if (period.State is VppPeriodState.Pricing or VppPeriodState.Settled
                    || header.SettledAt is not null
                    || nowUtc >= period.SupplementApprovalDeadlineUtc)
                    throw new ConflictException("The supplement approval deadline has passed.");

                var approvedCount = await requestSet.AsNoTracking().CountAsync(x =>
                    x.CreatedByUserId == header.CreatedByUserId
                    && x.MemberCompanyCode == header.MemberCompanyCode
                    && (x.PeriodId == period.Id
                        || (x.PeriodId == null && x.Year == header.Year && x.Month == header.Month))
                    && (x.BaseRequestSeriesId == header.BaseRequestSeriesId
                        || x.BaseRequestSeriesId == null)
                    && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted
                    && x.Status == (int)VPPStatus.Approved);
                if (approvedCount >= _policy.MaxApprovedSupplements)
                    throw new ConflictException("The approved supplement quota is full.");

                header.Status = (int)VPPStatus.Approved;
                header.ApprovedById = adminId;
                header.ApprovedAt = now;
                header.RejectReason = null;
                header.UpdatedByUserId = adminId;
                header.UpdatedAtUtc = now;
                context.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "APPROVE",
                    Action = "APPROVE",
                    ActorUserId = adminId,
                    MemberCompanyCode = header.MemberCompanyCode,
                    RevisionNumber = header.RevisionNumber,
                    CorrelationId = normalizedKey,
                    Reason = header.SupplementReason,
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(
                        header, header.RequestDetails ?? new List<VppRequestDetail>()))
                });
                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("The supplement changed while it was being approved.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private async Task RejectAdditionalOrderCoreAsync(
            Guid id,
            int adminId,
            string? reason,
            byte[]? rowVersion,
            string? idempotencyKey,
            string? actorDepartmentCode,
            bool canApproveCrossDepartment,
            string? memberCompanyCode)
        {
            var normalizedReason = reason?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length < 5)
                throw new BusinessException("A rejection reason of at least 5 characters is required.");
            var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var context = _scopedUow.VPPContext;
                if (context.Database.IsSqlServer())
                    await context.Database.ExecuteSqlRawAsync(
                        "SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");
                var requestSet = context.Set<VppRequest>();
                var header = await requestSet
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted
                        && x.IsCurrentRevision && x.IsAdditionalOrder);
                if (header is null)
                    throw new KeyNotFoundException("Order not found or not an additional order.");
                EnsureDecisionScope(header, adminId, actorDepartmentCode,
                    canApproveCrossDepartment, memberCompanyCode);
                if (await HasDecisionReplayAsync(
                        id, "REJECT", normalizedKey, normalizedReason))
                {
                    await _scopedUow.RollbackAsync();
                    return;
                }
                if (header.Status == (int)VPPStatus.Rejected)
                {
                    await _scopedUow.RollbackAsync();
                    return;
                }
                EnsureExpectedRowVersion(header.RowVersion, rowVersion);
                if (header.Status != (int)VPPStatus.Pending)
                    throw new BusinessException("The supplement is no longer pending.");

                var period = await EnsurePeriodAsync(
                    header.MemberCompanyCode ?? string.Empty, new Period(header.Year, header.Month));
                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                if (period.State is VppPeriodState.Pricing or VppPeriodState.Settled
                    || header.SettledAt is not null
                    || nowUtc >= period.SupplementApprovalDeadlineUtc)
                    throw new ConflictException("The supplement is immutable after its approval window or settlement.");

                header.Status = (int)VPPStatus.Rejected;
                header.RejectedById = adminId;
                header.RejectedAt = now;
                header.RejectReason = normalizedReason;
                header.UpdatedByUserId = adminId;
                header.UpdatedAtUtc = now;
                context.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "REJECT",
                    Action = "REJECT",
                    ActorUserId = adminId,
                    MemberCompanyCode = header.MemberCompanyCode,
                    RevisionNumber = header.RevisionNumber,
                    CorrelationId = normalizedKey,
                    Reason = normalizedReason,
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(
                        header, header.RequestDetails ?? new List<VppRequestDetail>()))
                });
                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("The supplement changed while it was being rejected.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> HasDecisionReplayAsync(
            Guid requestId,
            string action,
            string? key,
            string? reason)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var previous = await _scopedUow.VPPContext.Set<RequestLog>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.RequestId == requestId
                    && x.CorrelationId == key);
            if (previous is null)
                return false;

            if (!string.Equals(previous.Action, action, StringComparison.Ordinal)
                || (string.Equals(action, "REJECT", StringComparison.Ordinal)
                    && !string.Equals(previous.Reason, reason, StringComparison.Ordinal)))
            {
                throw new ConflictException(
                    "The idempotency key was already used for a different supplement decision.");
            }

            return true;
        }

        private static void EnsureDecisionScope(
            VppRequest header,
            int actorUserId,
            string? actorDepartmentCode,
            bool canApproveCrossDepartment,
            string? memberCompanyCode)
        {
            if (header.CreatedByUserId == actorUserId)
                throw new UnauthorizedAccessException("A requester cannot approve or reject their own supplement.");
            if (!string.IsNullOrWhiteSpace(memberCompanyCode)
                && !string.Equals(header.MemberCompanyCode, memberCompanyCode,
                    StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("The request is outside the active company scope.");
            if (!canApproveCrossDepartment
                && !string.IsNullOrWhiteSpace(actorDepartmentCode)
                && !string.Equals(header.DepartmentCode, actorDepartmentCode,
                    StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("The request is outside the approver department scope.");
        }

        private async Task ApproveAdditionalOrderLegacyAsync(Guid id, int adminId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VppRequest>()
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsAdditionalOrder);

                if (header == null) throw new KeyNotFoundException("Order not found or not an additional order.");

                var now = _dateTimeProvider.Now;
                header.Status = TransitionStatus(header.Status, OrderAction.Approve, "Order must be in Pending status.");
                header.ApprovedById = adminId;
                header.ApprovedAt = now;
                header.RejectReason = null;
                header.UpdatedByUserId = adminId;
                header.UpdatedAtUtc = now;

                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "APPROVE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.RequestDetails ?? new List<VppRequestDetail>()))
                });

                await _scopedUow.CommitAsync();
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task RejectAdditionalOrderAsync(
            Guid id,
            int adminId,
            string? reason,
            byte[] rowVersion,
            string? idempotencyKey,
            string? actorDepartmentCode,
            bool canApproveCrossDepartment,
            string? memberCompanyCode)
            => await RejectAdditionalOrderCoreAsync(
                id, adminId, reason, rowVersion, idempotencyKey, actorDepartmentCode,
                canApproveCrossDepartment, memberCompanyCode);

        private async Task RejectAdditionalOrderLegacyAsync(Guid id, int adminId, string? reason)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VppRequest>()
                    .Include(x => x.RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsAdditionalOrder);

                if (header == null) throw new KeyNotFoundException("Order not found or not an additional order.");

                var now = _dateTimeProvider.Now;
                header.Status = TransitionStatus(header.Status, OrderAction.Reject, "Order must be in Pending status.");
                header.RejectedById = adminId;
                header.RejectedAt = now;
                header.RejectReason = reason;
                header.UpdatedByUserId = adminId;
                header.UpdatedAtUtc = now;

                _scopedUow.VPPContext.Set<RequestLog>().Add(new RequestLog
                {
                    Id = Guid.NewGuid(),
                    RequestId = header.Id,
                    LogTitle = "REJECT",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.RequestDetails ?? new List<VppRequestDetail>()))
                });

                await _scopedUow.CommitAsync();
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task<VppPeriodInfoResDTO> GetCurrentPeriodInfoAsync(int userId)
        {
            var now = _dateTimeProvider.Now;
            var current = _periodCalculator.Current(now);
            var previous = _periodCalculator.Previous(now);
            var user = await _scopedUow.VPPContext.Set<AppUser>()
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            var company = user?.MemberCompanyCode.ToString() ?? string.Empty;
            var period = await EnsurePeriodAsync(company, current);
            var requestSet = _scopedUow.VPPContext.Set<VppRequest>();

            var baseRequest = await requestSet.AsNoTracking().FirstOrDefaultAsync(x =>
                x.CreatedByUserId == userId && x.MemberCompanyCode == company
                && (x.PeriodId == period.Id
                    || (x.PeriodId == null && x.Year == current.Year && x.Month == current.Month))
                && !x.IsAdditionalOrder
                && x.IsCurrentRevision && !x.IsDeleted);
            var approvedCount = baseRequest is null ? 0 : await requestSet.AsNoTracking().CountAsync(x =>
                x.CreatedByUserId == userId && x.MemberCompanyCode == company
                && (x.PeriodId == period.Id
                    || (x.PeriodId == null && x.Year == current.Year && x.Month == current.Month))
                && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                    || x.BaseRequestSeriesId == null)
                && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted
                && x.Status == (int)VPPStatus.Approved);
            var attemptCount = baseRequest is null ? 0 : await requestSet.AsNoTracking().CountAsync(x =>
                x.CreatedByUserId == userId && x.MemberCompanyCode == company
                && (x.PeriodId == period.Id
                    || (x.PeriodId == null && x.Year == current.Year && x.Month == current.Month))
                && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                    || x.BaseRequestSeriesId == null)
                && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted);
            var hasPending = baseRequest is not null && await requestSet.AsNoTracking().AnyAsync(x =>
                x.CreatedByUserId == userId && x.MemberCompanyCode == company
                && (x.PeriodId == period.Id
                    || (x.PeriodId == null && x.Year == current.Year && x.Month == current.Month))
                && (x.BaseRequestSeriesId == baseRequest.RequestSeriesId
                    || x.BaseRequestSeriesId == null)
                && x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted
                && x.Status == (int)VPPStatus.Pending);
            var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
            var submissionOpen = period.State == VppPeriodState.Open
                && nowUtc < period.SubmissionDeadlineUtc;
            var approvalOpen = nowUtc < period.SupplementApprovalDeadlineUtc
                && period.State != VppPeriodState.Settled;
            var baseEligibleForSupplement = baseRequest?.Status is
                (int)VPPStatus.Submitted or (int)VPPStatus.Approved;
            var hasPreviousOrder = await requestSet.AsNoTracking().AnyAsync(x =>
                x.CreatedByUserId == userId && x.Year == previous.Year && x.Month == previous.Month
                && !x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted);

            return new VppPeriodInfoResDTO
            {
                PeriodId = period.Id,
                PeriodState = period.State.ToString(),
                CurrentPeriodYear = current.Year,
                CurrentPeriodMonth = current.Month,
                PreviousPeriodYear = previous.Year,
                PreviousPeriodMonth = previous.Month,
                // Persisted boundaries are UTC; expose the fixed Vietnam
                // business wall-clock so a UTC-hosted server cannot shift the
                // date shown to Vietnamese users.
                StartDate = ToBusinessLocal(period.StartAtUtc),
                DeadlineDate = ToBusinessLocal(period.SubmissionDeadlineUtc),
                SupplementApprovalDeadlineDate = ToBusinessLocal(period.SupplementApprovalDeadlineUtc),
                IsDeadlinePassed = !submissionOpen,
                IsSubmissionOpen = submissionOpen,
                HasCurrentPeriodOrder = baseRequest is not null,
                BaseRequestId = baseRequest?.Id,
                BaseRequestCode = baseRequest?.VppCode,
                AdditionalOrderCount = approvedCount,
                MaxAdditionalOrders = _policy.MaxApprovedSupplements,
                ApprovedSupplementCount = approvedCount,
                SupplementAttemptCount = attemptCount,
                MaxSupplementAttempts = _policy.MaxSupplementAttempts,
                RemainingApprovedSupplementQuota = Math.Max(0, _policy.MaxApprovedSupplements - approvedCount),
                RemainingSupplementAttempts = Math.Max(0, _policy.MaxSupplementAttempts - attemptCount),
                HasPendingAdditional = hasPending,
                CanCreateOrder = submissionOpen && baseRequest is null,
                CanCreateOrderReason = submissionOpen
                    ? (baseRequest is null ? null : "You already have a regular order for this period.")
                    : "The regular submission window is closed.",
                CanCreateAdditional = submissionOpen && approvalOpen && baseEligibleForSupplement
                    && approvedCount < _policy.MaxApprovedSupplements
                    && attemptCount < _policy.MaxSupplementAttempts && !hasPending,
                CanCreateAdditionalReason = baseRequest is null
                    ? "Create and submit a regular order first."
                    : !baseEligibleForSupplement
                        ? "The regular request is cancelled or otherwise ineligible for supplements."
                    : hasPending ? "Resolve the pending supplement first."
                    : approvedCount >= _policy.MaxApprovedSupplements ? "The approved supplement quota is full."
                    : attemptCount >= _policy.MaxSupplementAttempts ? "The supplement attempt limit is full."
                    : submissionOpen ? null : "The supplement submission window is closed.",
                HasPreviousOrder = hasPreviousOrder,
                CanCopyPrevious = submissionOpen && baseRequest is null && hasPreviousOrder
            };
        }

        private async Task<VppPeriodInfoResDTO> GetCurrentPeriodInfoLegacyAsync(int userId)
        {
            var (curYear, curMonth, prevYear, prevMonth) = GetCurrentAndPreviousPeriod();

            var hasCurrentOrder = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .AnyAsync(x => x.CreatedByUserId == userId && x.Year == curYear && x.Month == curMonth && !x.IsAdditionalOrder && !x.IsDeleted);

            var additionalCount = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .CountAsync(x => x.CreatedByUserId == userId && x.Year == prevYear && x.Month == prevMonth && x.IsAdditionalOrder && !x.IsDeleted);

            var hasPendingAdditional = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .AnyAsync(x => x.CreatedByUserId == userId && x.Year == prevYear && x.Month == prevMonth && x.IsAdditionalOrder && !x.IsDeleted && x.Status == (int)VPPStatus.Pending);

            var hasPreviousOrder = await _scopedUow.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .AnyAsync(x => x.CreatedByUserId == userId && x.Year == prevYear && x.Month == prevMonth && !x.IsAdditionalOrder && !x.IsDeleted);

            var deadlinePassed = IsDeadlinePassed(curYear, curMonth);

            return new VppPeriodInfoResDTO
            {
                CurrentPeriodYear = curYear,
                CurrentPeriodMonth = curMonth,
                PreviousPeriodYear = prevYear,
                PreviousPeriodMonth = prevMonth,
                DeadlineDate = _periodCalculator.DeadlineFor(new Period(curYear, curMonth)),
                IsDeadlinePassed = deadlinePassed,
                HasCurrentPeriodOrder = hasCurrentOrder,
                AdditionalOrderCount = additionalCount,
                MaxAdditionalOrders = _policy.MaxApprovedSupplements,
                HasPendingAdditional = hasPendingAdditional,
                CanCreateOrder = !deadlinePassed && !hasCurrentOrder,
                CanCreateAdditional = additionalCount < _policy.MaxApprovedSupplements && !hasPendingAdditional,
                HasPreviousOrder = hasPreviousOrder,
                CanCopyPrevious = !deadlinePassed && !hasCurrentOrder && hasPreviousOrder
            };
        }

        // â”€â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private bool IsDeadlinePassed(int year, int month)
            => _periodCalculator.IsDeadlinePassed(_dateTimeProvider.Now, new Period(year, month));

        private async Task<VppPeriod> EnsurePeriodAsync(string memberCompanyCode, Period period)
        {
            if (_periodService is not null)
            {
                var ensured = await _periodService.EnsureAsync(memberCompanyCode, period);
                // A request may arrive after downtime. Advance the durable
                // aggregate before enforcing its write deadline.
                await _periodService.AdvanceDuePeriodsAsync(memberCompanyCode);
                return await _periodService.GetAsync(memberCompanyCode, period) ?? ensured;
            }

            // Unit-test/legacy fallback: keep the service usable when a test
            // constructs it manually without the DI period service.
            var company = (memberCompanyCode ?? string.Empty).Trim();
            var periods = _scopedUow.VPPContext.Set<VppPeriod>();
            var existing = await periods.FirstOrDefaultAsync(x =>
                !x.IsDeleted && x.MemberCompanyCode == company
                && x.Year == period.Year && x.Month == period.Month);
            if (existing is not null)
                return existing;

            var nowUtc = PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
            var entity = new VppPeriod
            {
                Id = Guid.NewGuid(),
                MemberCompanyCode = company,
                TimeZoneId = "Asia/Ho_Chi_Minh",
                Year = period.Year,
                Month = period.Month,
                StartAtUtc = _periodCalculator.StartAtUtc(period),
                SubmissionDeadlineUtc = _periodCalculator.SubmissionDeadlineUtc(period),
                SupplementApprovalDeadlineUtc = _periodCalculator
                    .SupplementApprovalDeadlineUtc(period, _policy.SupplementApprovalGrace),
                State = nowUtc >= _periodCalculator.SupplementApprovalDeadlineUtc(
                    period, _policy.SupplementApprovalGrace)
                    ? VppPeriodState.Pricing
                    : nowUtc >= _periodCalculator.SubmissionDeadlineUtc(period)
                        ? VppPeriodState.SubmissionClosed
                        : VppPeriodState.Open,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };
            periods.Add(entity);
            await _scopedUow.SaveChangesAsync();
            return entity;
        }

        private static void EnsureExpectedRowVersion(byte[]? actual, byte[]? expected)
        {
            if (expected is not { Length: > 0 })
                throw new BusinessException("RowVersion is required. Refresh the request and try again.");
            if (actual is null || !actual.AsSpan().SequenceEqual(expected))
                throw new ConflictException("The request changed while you were editing it. Refresh and try again.");
        }

        private static VppRequest CloneHeaderForLifecycle(
            VppRequest source,
            Guid id,
            DateTime now,
            int actorUserId,
            int status)
            => new()
            {
                Id = id,
                Year = source.Year,
                Month = source.Month,
                PeriodId = source.PeriodId,
                RequestSeriesId = source.RequestSeriesId == Guid.Empty
                    ? Guid.NewGuid() : source.RequestSeriesId,
                RevisionNumber = source.RevisionNumber + 1,
                IsCurrentRevision = true,
                SupersedesRequestId = source.Id,
                VppCode = $"VPP-{source.Year:D4}{source.Month:D2}-{Guid.NewGuid():N}",
                Status = status,
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
                CreatedByUserId = source.CreatedByUserId,
                CreatedAtUtc = source.CreatedAtUtc,
                UpdatedByUserId = actorUserId,
                UpdatedAtUtc = now
            };

        private static List<VppRequestDetail> CloneDetails(
            IEnumerable<VppRequestDetail>? source,
            Guid headerId,
            int userId,
            DateTime now)
            => (source ?? Enumerable.Empty<VppRequestDetail>())
                .Where(x => !x.IsDeleted)
                .Select(x => new VppRequestDetail
                {
                    Id = Guid.NewGuid(),
                    VppId = x.VppId,
                    Qty = x.Qty,
                    CurrentSinglePrice = x.CurrentSinglePrice,
                    Description = x.Description,
                    RequestId = headerId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = now
                })
                .ToList();

        private static string ComputePayloadHash(VppRequestCreateReqDTO request)
            => ComputeHash(new
            {
                request.Year,
                request.Month,
                request.IsAdditionalOrder,
                request.BaseRequestId,
                Reason = request.SupplementReason?.Trim(),
                request.Description,
                Items = request.Items.OrderBy(x => x.VppId).Select(x => new { x.VppId, x.Qty, x.Description })
            });

        private static string ComputePayloadHash(VppRequestUpdateReqDTO request)
            => ComputeHash(new
            {
                request.Id,
                request.IsAdditionalOrder,
                Reason = request.SupplementReason?.Trim(),
                request.Description,
                Items = request.Items.OrderBy(x => x.VppId).Select(x => new { x.VppId, x.Qty, x.Description })
            });

        private static string ComputeCancellationHash(VppRequestCancelReqDTO? request)
            => ComputeHash(new { Reason = request?.Reason?.Trim() });

        private static string? NormalizeIdempotencyKey(string? value)
        {
            var normalized = value?.Trim();
            if (string.IsNullOrEmpty(normalized))
                return null;
            if (normalized.Length > 128)
                throw new BusinessException("The idempotency key cannot exceed 128 characters.");
            return normalized;
        }

        private static string ComputeHash(object value)
        {
            var json = JsonSerializer.Serialize(value);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        }

        private static int TransitionStatus(int currentStatus, OrderAction action, string invalidMessage)
        {
            try
            {
                return OrderStateMachine.Transition(currentStatus, action);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(invalidMessage, ex);
            }
        }

        private string GenerateVppCode(int year, int month)
            => $"VPP-{year:D4}{month:D2}-{Guid.NewGuid():N}";

        private (int curYear, int curMonth, int prevYear, int prevMonth) GetCurrentAndPreviousPeriod()
        {
            var now = _dateTimeProvider.Now;
            var current = _periodCalculator.Current(now);
            var previous = _periodCalculator.Previous(now);
            return (current.Year, current.Month, previous.Year, previous.Month);
        }

        /// <summary>
        /// Reject obviously invalid Year/Month from the client (F-09). Order Year/Month must fall
        /// inside [current period - 12 months, current period + 1 month] â€” anything
        /// outside is a malformed request.
        /// </summary>
        private void ValidateRequestedPeriod(VppRequestCreateReqDTO req)
        {
            if (req.Year < 1900 || req.Year > 9999)
                throw new BusinessException($"Invalid year {req.Year}.");
            if (req.Month < 1 || req.Month > 12)
                throw new BusinessException($"Invalid month {req.Month}.");

            var requested = new Period(req.Year, req.Month);
            var current = _periodCalculator.Current(_dateTimeProvider.Now);
            var monthsDiff = ((requested.Year - current.Year) * 12) + (requested.Month - current.Month);
            if (monthsDiff < -12 || monthsDiff > 1)
            {
                throw new BusinessException(
                    $"Period {requested} is too far from the current period {current}.");
            }
        }

        /// <summary>
        /// Validates that all requested product IDs are active (not soft-deleted)
        /// and their categories are also active. Prevents orders with deleted products.
        /// </summary>
        private async Task ValidateActiveProductsAsync(IEnumerable<VppRequestDetailItemReqDTO> items)
        {
            var requestedIds = items.Select(x => x.VppId).Distinct().ToArray();
            if (requestedIds.Length == 0) return;

            var activeProducts = await _scopedUow.VPPContext.Set<VppItem>()
                .AsNoTracking()
                .Where(x => requestedIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.VppName,
                    CategoryDeleted = x.VppCategory != null && x.VppCategory.IsDeleted
                })
                .ToListAsync();

            // Check for missing/deleted products
            var activeIds = activeProducts.Select(x => x.Id).ToHashSet();
            var missingIds = requestedIds.Where(id => !activeIds.Contains(id)).ToArray();
            if (missingIds.Length > 0)
                throw new BusinessException(
                    $"The following products have been disabled and cannot be added to the order. Please remove them and try again.");

            // Check for products whose category has been deleted
            var categoryDeletedItems = activeProducts.Where(x => x.CategoryDeleted).ToArray();
            if (categoryDeletedItems.Length > 0)
            {
                var names = string.Join(", ", categoryDeletedItems.Select(x => x.VppName));
                throw new BusinessException(
                    $"The category for the following products has been disabled: {names}. Please remove them and try again.");
            }
        }

        private async Task<List<VppRequestDetail>> BuildRequestDetailsAsync(IEnumerable<VppRequestDetailItemReqDTO> items, int userId, Guid headerId, DateTime now)
        {
            var requestedItems = items.ToList();
            var currentPrices = await GetCurrentSinglePricesAsync(requestedItems.Select(x => x.VppId));

            return requestedItems.Select(item => new VppRequestDetail
            {
                Id = Guid.NewGuid(),
                VppId = item.VppId,
                Qty = item.Qty,
                CurrentSinglePrice = currentPrices.TryGetValue(item.VppId, out var price) ? price : 0,
                Description = item.Description,
                RequestId = headerId,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedByUserId = userId,
                UpdatedAtUtc = now
            }).ToList();
        }

        private async Task<Dictionary<Guid, long>> GetCurrentSinglePricesAsync(IEnumerable<Guid> vppIds, Guid? priceListId = null)
        {
            var distinctVppIds = vppIds.Distinct().ToArray();
            if (distinctVppIds.Length == 0)
            {
                return new Dictionary<Guid, long>();
            }

            var effectivePriceListId = priceListId
                ?? await _scopedUow.VPPContext.Set<PriceList>()
                    .AsNoTracking()
                    .Where(x => x.IsDefault && !x.IsDeleted)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync();

            if (!effectivePriceListId.HasValue)
            {
                return new Dictionary<Guid, long>();
            }

            var priceRows = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking()
                .Where(x => x.PriceListId == effectivePriceListId.Value
                         && distinctVppIds.Contains(x.VppItemId)
                         && !x.IsDeleted)
                .Select(x => new { VppId = x.VppItemId, x.Price, x.IsDefault })
                .ToListAsync();

            return priceRows
                .GroupBy(x => x.VppId)
                .ToDictionary(g => g.Key, g => (long)(g.FirstOrDefault(x => x.IsDefault)?.Price ?? g.First().Price));
        }

        /// <summary>
        /// P1: Materialize period-derived flags on response DTOs so FE never recomputes
        /// them with its own clock (was F-02 / F-33 root cause). Pure in-memory pass.
        /// </summary>
        private void ApplyPeriodFlags(IEnumerable<VppRequestResDTO> orders)
        {
            var now = _dateTimeProvider.Now;
            var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
            foreach (var order in orders)
            {
                var period = new Period(order.Year, order.Month);
                var regularDeadlineUtc = _periodCalculator.SubmissionDeadlineUtc(period);
                var supplementDeadlineUtc = _periodCalculator.SupplementApprovalDeadlineUtc(
                    period, _policy.SupplementApprovalGrace);
                var regularDeadlinePassed = nowUtc >= regularDeadlineUtc;
                var supplementDeadlinePassed = nowUtc >= supplementDeadlineUtc;

                // IsDeadlinePassed is the regular submission flag retained for
                // wire compatibility. Supplement actions use their separate
                // approval deadline below.
                order.IsDeadlinePassed = regularDeadlinePassed;
                order.CanEdit = order.IsCurrentRevision && (order.IsAdditionalOrder
                    ? order.Status == (int)VPPStatus.Pending
                        && !supplementDeadlinePassed
                        && order.SettledAt is null
                    : order.Status == (int)VPPStatus.Submitted
                        && !regularDeadlinePassed
                        && order.SettledAt is null);
                order.CanCancel = order.IsCurrentRevision && order.CanEdit;
                order.CanReplace = order.IsCurrentRevision && !order.IsAdditionalOrder
                    && order.Status == (int)VPPStatus.Cancelled
                    && !regularDeadlinePassed
                    && order.SettledAt is null;
            }
        }

        private static DateTime ToBusinessLocal(DateTime utc)
        {
            var normalized = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            return DateTime.SpecifyKind(
                TimeZoneInfo.ConvertTimeFromUtc(normalized, PeriodCalculator.BusinessTimeZone),
                DateTimeKind.Unspecified);
        }

        private async Task ApplyRequesterNamesAsync(List<VppRequestResDTO> orders)
        {
            var userIds = orders
                .SelectMany(x => new[] { (int?)x.CreatedByUserId, x.SettledByUserId })
                .Where(x => x.HasValue && x.Value > 0)
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

            var priceListIds = orders
                .Select(x => x.SettledByPriceListId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            var priceLists = priceListIds.Length == 0
                ? new Dictionary<Guid, string?>()
                : await _scopedUow.VPPContext.Set<PriceList>()
                    .AsNoTracking()
                    .Where(x => priceListIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.PriceListName })
                    .ToDictionaryAsync(x => x.Id, x => x.PriceListName);

            foreach (var order in orders)
            {
                order.RequesterName = users.TryGetValue(order.CreatedByUserId, out var fullName)
                    ? fullName
                    : null;
                order.SettledByUserName = order.SettledByUserId.HasValue
                    && users.TryGetValue(order.SettledByUserId.Value, out var settledByUserName)
                        ? settledByUserName
                        : null;
                if (order.SettledByPriceListId.HasValue
                    && priceLists.TryGetValue(order.SettledByPriceListId.Value, out var priceListName))
                {
                    order.SettledByPriceListName = priceListName;
                }
            }
        }

        private static object BuildLogPayload(VppRequest header, IEnumerable<VppRequestDetail> details)
            => new
            {
                header.Id,
                header.VppCode,
                header.Year,
                header.Month,
                header.Status,
                header.Description,
                header.RequestSeriesId,
                header.RevisionNumber,
                header.IsCurrentRevision,
                header.SupersedesRequestId,
                header.SupersededByRequestId,
                header.BaseRequestId,
                header.BaseRequestSeriesId,
                header.SupplementSequence,
                header.SupplementAttemptNumber,
                header.SupplementReason,
                header.RejectReason,
                header.CancelReason,
                header.DepartmentCode,
                header.MemberCompanyCode,
                header.CreatedByUserId,
                header.CreatedAtUtc,
                header.UpdatedByUserId,
                header.UpdatedAtUtc,
                header.SubmittedDate,
                Detail = details.Select(x => new
                {
                    x.Id,
                    x.VppId,
                    x.Qty,
                    x.CurrentSinglePrice,
                    x.Description,
                    x.CreatedByUserId,
                    x.CreatedAtUtc,
                    x.UpdatedByUserId,
                    x.UpdatedAtUtc,
                    x.IsDeleted,
                    x.RequestId
                }).ToList()
            };

        private static bool IsUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2601 || sqlException.Number == 2627);

        private static void ValidateItems(List<VppRequestDetailItemReqDTO>? items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Order must contain at least one item.");
            if (items.Any(i => i.Qty <= 0))
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            if (items.GroupBy(i => i.VppId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate product in order items is not allowed.");
        }
    }
}
