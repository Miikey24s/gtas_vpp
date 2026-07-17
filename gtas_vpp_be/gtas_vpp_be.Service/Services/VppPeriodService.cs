using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Company-period aggregate service.  It intentionally retains the existing
/// PeriodCalculator as a compatibility boundary while persisted periods are
/// introduced additively.
/// </summary>
public sealed class VppPeriodService : IVppPeriodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PeriodCalculator _periodCalculator;
    private readonly VppRequestPolicy _policy;
    private readonly ILogger<VppPeriodService> _logger;

    public VppPeriodService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        PeriodCalculator periodCalculator,
        VppRequestPolicy policy,
        ILogger<VppPeriodService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _periodCalculator = periodCalculator ?? throw new ArgumentNullException(nameof(periodCalculator));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<VPP00_Period> EnsureCurrentAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var current = _periodCalculator.Current(_dateTimeProvider.Now);
        return EnsureAsync(memberCompanyCode, current, cancellationToken);
    }

    public Task<VPP00_Period> EnsureAsync(
        string memberCompanyCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
        => EnsureAsync(memberCompanyCode, new Period(year, month), cancellationToken);

    public async Task<VPP00_Period> EnsureAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        ValidatePeriod(period);

        var existing = await _unitOfWork.VPPContext.Set<VPP00_Period>()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Y == period.Year
                    && x.M == period.Month,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var createdAtUtc = CurrentUtc();
        var entity = new VPP00_Period
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = company,
            Y = period.Year,
            M = period.Month,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartAtUtc = _periodCalculator.StartAtUtc(period),
            SubmissionDeadlineUtc = _periodCalculator.SubmissionDeadlineUtc(period),
            SupplementApprovalDeadlineUtc = _periodCalculator
                .SupplementApprovalDeadlineUtc(period, _policy.SupplementApprovalGrace),
            State = VppPeriodState.Open,
            CreateDate = createdAtUtc,
            UpdateDate = createdAtUtc,
            IsDeleted = false
        };

        _unitOfWork.VPPContext.Set<VPP00_Period>().Add(entity);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException exception)
        {
            // The unique company/period index is the arbitration point when
            // two first requests arrive concurrently.  Detach our loser and
            // return the winner; callers never see a duplicate aggregate.
            _unitOfWork.VPPContext.Entry(entity).State = EntityState.Detached;
            var winner = await _unitOfWork.VPPContext.Set<VPP00_Period>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => !x.IsDeleted
                        && x.MemberCompanyCode == company
                        && x.Y == period.Year
                        && x.M == period.Month,
                    cancellationToken);
            if (winner is not null)
            {
                return winner;
            }

            throw new ConflictException(
                "Không thể tạo kỳ yêu cầu do xung đột đồng thời.", exception);
        }
    }

    public async Task<VPP00_Period?> GetAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        ValidatePeriod(period);

        return await _unitOfWork.VPPContext.Set<VPP00_Period>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Y == period.Year
                    && x.M == period.Month,
                cancellationToken);
    }

    public Task<VPP00_Period?> GetAsync(
        string memberCompanyCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
        => GetAsync(memberCompanyCode, new Period(year, month), cancellationToken);

    public async Task<VPP00_Period?> GetAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
        => await _unitOfWork.VPPContext.Set<VPP00_Period>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == periodId, cancellationToken);

    public async Task<IReadOnlyList<VPP00_Period>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var transitioned = new List<VPP00_Period>();
        // Two passes are enough to recover an old period through both due
        // automatic stages (Open -> SubmissionClosed -> Pricing) in one tick.
        // A later invocation remains a no-op, which makes retries idempotent.
        for (var pass = 0; pass < 2; pass++)
        {
            var nowUtc = CurrentUtc();
            var candidates = await _unitOfWork.VPPContext.Set<VPP00_Period>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                    && (x.State == VppPeriodState.Open
                        || x.State == VppPeriodState.SubmissionClosed)
                    && ((x.State == VppPeriodState.Open
                            && x.SubmissionDeadlineUtc <= nowUtc)
                        || (x.State == VppPeriodState.SubmissionClosed
                            && x.SupplementApprovalDeadlineUtc <= nowUtc)))
                .OrderBy(x => x.StartAtUtc)
                .ToListAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var target = candidate.State == VppPeriodState.Open
                    ? VppPeriodState.SubmissionClosed
                    : VppPeriodState.Pricing;
                try
                {
                    transitioned.Add(await TransitionWithResultAsync(
                        candidate.Id,
                        target,
                        cancellationToken: cancellationToken));
                }
                catch (ConflictException exception)
                {
                    // Another request/worker may have won the rowversion race.
                    // Recovery is best effort; leave the winner as source of
                    // truth and continue processing other periods.
                    _logger.LogDebug(
                        exception,
                        "Period {PeriodId} was advanced concurrently; continuing recovery.",
                        candidate.Id);
                }
            }
        }

        return transitioned;
    }

    public async Task AdvanceDuePeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        for (var pass = 0; pass < 2; pass++)
        {
            var nowUtc = CurrentUtc();
            var candidates = await _unitOfWork.VPPContext.Set<VPP00_Period>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && (x.State == VppPeriodState.Open
                        || x.State == VppPeriodState.SubmissionClosed)
                    && ((x.State == VppPeriodState.Open
                            && x.SubmissionDeadlineUtc <= nowUtc)
                        || (x.State == VppPeriodState.SubmissionClosed
                            && x.SupplementApprovalDeadlineUtc <= nowUtc)))
                .OrderBy(x => x.StartAtUtc)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var periodId in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = await _unitOfWork.VPPContext.Set<VPP00_Period>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => !x.IsDeleted && x.Id == periodId,
                        cancellationToken);
                if (current is null)
                {
                    continue;
                }

                var target = current.State == VppPeriodState.Open
                    ? VppPeriodState.SubmissionClosed
                    : VppPeriodState.Pricing;
                try
                {
                    await TransitionAsync(
                        periodId, target, 0, "period-recovery", cancellationToken);
                }
                catch (ConflictException exception)
                {
                    _logger.LogDebug(exception,
                        "Period {PeriodId} was advanced concurrently; continuing recovery.",
                        periodId);
                }
            }
        }
    }

    public async Task TransitionAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await TransitionWithResultAsync(
            periodId, targetState, actorUserId, reason, cancellationToken);
    }

    public async Task<VPP00_Period> TransitionWithResultAsync(
        Guid periodId,
        VppPeriodState targetState,
        int? actorUserId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(targetState))
        {
            throw new ArgumentOutOfRangeException(nameof(targetState));
        }

        var period = await _unitOfWork.VPPContext.Set<VPP00_Period>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == periodId, cancellationToken);
        if (period is null)
        {
            throw new KeyNotFoundException("Không tìm thấy kỳ yêu cầu.");
        }

        if (period.State == targetState)
        {
            return period;
        }

        if (!IsLegalTransition(period.State, targetState))
        {
            throw new ConflictException(
                $"Không thể chuyển kỳ từ {period.State} sang {targetState}.");
        }

        var nowUtc = CurrentUtc();
        if (targetState == VppPeriodState.SubmissionClosed
            && nowUtc < period.SubmissionDeadlineUtc)
        {
            throw new ConflictException("Kỳ chưa đến hạn đóng nhận đơn.");
        }

        if (targetState == VppPeriodState.Pricing
            && nowUtc < period.SupplementApprovalDeadlineUtc)
        {
            throw new ConflictException("Kỳ chưa hết hạn duyệt đơn bổ sung.");
        }

        period.State = targetState;
        period.LastTransitionUserId = actorUserId;
        period.LastTransitionAtUtc = nowUtc;
        period.LastTransitionReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();
        period.UpdateDate = nowUtc;

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return period;
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(
                "Kỳ vừa được cập nhật bởi người dùng hoặc tiến trình khác.", exception);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException(
                "Không thể cập nhật trạng thái kỳ do xung đột dữ liệu.", exception);
        }
    }

    public static bool IsLegalTransition(
        VppPeriodState currentState,
        VppPeriodState targetState)
        => (currentState, targetState) switch
        {
            (VppPeriodState.Open, VppPeriodState.SubmissionClosed) => true,
            (VppPeriodState.SubmissionClosed, VppPeriodState.Pricing) => true,
            (VppPeriodState.Pricing, VppPeriodState.Settled) => true,
            _ => false
        };

    private DateTime CurrentUtc()
        => PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);

    private static string NormalizeCompanyCode(string memberCompanyCode)
    {
        if (string.IsNullOrWhiteSpace(memberCompanyCode))
        {
            throw new ArgumentException(
                "Member company code is required.", nameof(memberCompanyCode));
        }

        var normalized = memberCompanyCode.Trim();
        if (normalized.Length > 50)
        {
            throw new ArgumentException(
                "Member company code cannot exceed 50 characters.",
                nameof(memberCompanyCode));
        }

        return normalized;
    }

    private static void ValidatePeriod(Period period)
    {
        if (period.Year is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(period), period.Year,
                "Period year must be between 1 and 9999.");
        }

        if (period.Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(period), period.Month,
                "Period month must be between 1 and 12.");
        }
    }

}
