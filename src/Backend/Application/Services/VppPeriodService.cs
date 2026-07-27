using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Aggregate service cho kỳ công ty. Chủ động giữ PeriodCalculator hiện có làm
/// boundary tương thích trong khi kỳ được lưu được bổ sung dần.
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

    public Task<VppPeriod> EnsureCurrentAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var current = _periodCalculator.Current(_dateTimeProvider.Now);
        return EnsureAsync(memberCompanyCode, current, cancellationToken);
    }

    public Task<VppPeriod> EnsureAsync(
        string memberCompanyCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
        => EnsureAsync(memberCompanyCode, new Period(year, month), cancellationToken);

    public async Task<VppPeriod> EnsureAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        ValidatePeriod(period);

        var existing = await _unitOfWork.VPPContext.Set<VppPeriod>()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Year == period.Year
                    && x.Month == period.Month,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var createdAtUtc = CurrentUtc();
        var entity = new VppPeriod
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = company,
            Year = period.Year,
            Month = period.Month,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartAtUtc = _periodCalculator.StartAtUtc(period),
            SubmissionDeadlineUtc = _periodCalculator.SubmissionDeadlineUtc(period),
            SupplementApprovalDeadlineUtc = _periodCalculator
                .SupplementApprovalDeadlineUtc(period, _policy.SupplementApprovalGrace),
            State = VppPeriodState.Open,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            IsDeleted = false
        };

        _unitOfWork.VPPContext.Set<VppPeriod>().Add(entity);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException exception)
        {
            // Unique index công ty/kỳ là điểm phân xử khi hai request đầu tiên đến
            // đồng thời. Detach bản thua và trả về bản thắng để caller không thấy
            // aggregate trùng.
            _unitOfWork.VPPContext.Entry(entity).State = EntityState.Detached;
            var winner = await _unitOfWork.VPPContext.Set<VppPeriod>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => !x.IsDeleted
                        && x.MemberCompanyCode == company
                        && x.Year == period.Year
                        && x.Month == period.Month,
                    cancellationToken);
            if (winner is not null)
            {
                return winner;
            }

            throw new ConflictException(
                "Không thể tạo kỳ yêu cầu do xung đột đồng thời.", exception);
        }
    }

    public async Task<VppPeriod?> GetAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        ValidatePeriod(period);

        return await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && x.Year == period.Year
                    && x.Month == period.Month,
                cancellationToken);
    }

    public Task<VppPeriod?> GetAsync(
        string memberCompanyCode,
        int year,
        int month,
        CancellationToken cancellationToken = default)
        => GetAsync(memberCompanyCode, new Period(year, month), cancellationToken);

    public async Task<VppPeriod?> GetAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
        => await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == periodId, cancellationToken);

    public async Task<IReadOnlyList<VppPeriod>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var transitioned = new List<VppPeriod>();
        // Hai lượt đủ để phục hồi kỳ cũ qua cả hai stage tự động đến hạn
        // (Open -> SubmissionClosed -> Pricing) trong một tick. Lần gọi sau là no-op,
        // nhờ đó retry vẫn idempotent.
        for (var pass = 0; pass < 2; pass++)
        {
            var nowUtc = CurrentUtc();
            var candidates = await _unitOfWork.VPPContext.Set<VppPeriod>()
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
                    // Request/worker khác có thể đã thắng cuộc đua rowversion.
                    // Recovery theo best-effort: giữ bản thắng làm source of truth
                    // và tiếp tục xử lý các kỳ khác.
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
            var candidates = await _unitOfWork.VPPContext.Set<VppPeriod>()
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
                var current = await _unitOfWork.VPPContext.Set<VppPeriod>()
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

    public async Task<VppPeriod> TransitionWithResultAsync(
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

        var period = await _unitOfWork.VPPContext.Set<VppPeriod>()
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
        period.UpdatedAtUtc = nowUtc;

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
