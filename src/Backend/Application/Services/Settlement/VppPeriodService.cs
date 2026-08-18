using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Quản lý vòng đời kỳ đặt hàng và tự bù đủ số kỳ đang mở theo cấu hình.
/// Cấu hình chỉ áp dụng khi tạo kỳ mới; kỳ đã tồn tại luôn giữ nguyên lịch riêng.
/// </summary>
public sealed class VppPeriodService : IVppPeriodService
{
    private const string DefaultTimeZone = "Asia/Ho_Chi_Minh";
    private const int AutomaticOpenPeriodCount = 1;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PeriodScheduleCalculator _scheduleCalculator;
    private readonly ILogger<VppPeriodService> _logger;

    public VppPeriodService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        PeriodScheduleCalculator scheduleCalculator,
        ILogger<VppPeriodService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _scheduleCalculator = scheduleCalculator ?? throw new ArgumentNullException(nameof(scheduleCalculator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VppPeriod> EnsureCurrentAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        // Bước 1: cập nhật kỳ đã đến hạn; bước 2: bù đủ rolling horizon; bước 3: lấy kỳ neo hiện tại.
        // Kỳ đã tồn tại giữ nguyên timestamp, nên settings mới chỉ áp dụng cho kỳ tạo sau đó.
        var company = NormalizeCompanyCode(memberCompanyCode);
        await AdvanceDuePeriodsAsync(company, cancellationToken);
        await TopUpOpenHorizonAsync(company, cancellationToken: cancellationToken);
        var settings = await ResolveSettingsAsync(company, CurrentUtc(), cancellationToken);
        var anchor = ResolveAnchor(settings, CurrentLocal(settings));
        return await GetAsync(company, anchor, cancellationToken)
            ?? await EnsureAsync(company, anchor, cancellationToken);
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
        var existing = await FindTrackedAsync(company, period, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var settings = await ResolveSettingsAsync(
            company,
            new DateTime(period.Year, period.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            cancellationToken);
        var schedule = _scheduleCalculator.Build(period, period, settings);
        var state = ResolveInitialState(schedule, CurrentUtc());
        return await CreatePersistedAsync(
            company,
            period,
            settings,
            schedule,
            state,
            actorUserId: 0,
            reason: "compatibility-ensure",
            cancellationToken);
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

    public async Task<VppPeriod?> GetAsync(
        Guid periodId,
        CancellationToken cancellationToken = default)
        => await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.Id == periodId,
                cancellationToken);

    public async Task<IReadOnlyList<VppPeriod>> GetOpenPeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        await AdvanceDuePeriodsAsync(company, cancellationToken);
        var nowUtc = CurrentUtc();
        return await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.MemberCompanyCode == company
                && x.State == VppPeriodState.Open
                && x.StartAtUtc <= nowUtc
                && x.SubmissionDeadlineUtc > nowUtc)
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);
    }

    public async Task<VppOrderPeriodSettingsResDTO> GetSettingsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        var settings = await _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.MemberCompanyCode == company)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await ResolveSettingsAsync(company, CurrentUtc(), cancellationToken);
        return MapSettings(settings);
    }

    public async Task<VppOrderPeriodSettingsResDTO> GetEffectiveSettingsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        return MapSettings(await ResolveSettingsAsync(
            company,
            CurrentUtc(),
            cancellationToken));
    }

    public async Task<IReadOnlyList<VppOrderPeriodSettingsResDTO>> ListSettingsVersionsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        var versions = await _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.MemberCompanyCode == company)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(cancellationToken);
        return versions.Select(MapSettings).ToArray();
    }

    public async Task<VppOrderPeriodSettingsResDTO> SaveSettingsAsync(
        string memberCompanyCode,
        int actorUserId,
        VppOrderPeriodSettingsReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var company = NormalizeCompanyCode(memberCompanyCode);
        var nowUtc = CurrentUtc();
        var entity = new VppOrderPeriodSettingsVersion
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = company,
            VersionNumber = await NextSettingsVersionAsync(company, cancellationToken),
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? $"Rolling horizon {request.EffectiveFromMonth:D2}/{request.EffectiveFromYear:D4}"
                : request.Name.Trim(),
            // Hệ thống chỉ tự duy trì một kỳ; các kỳ khác do quản lý chủ động thêm.
            DefaultOpenPeriodCount = AutomaticOpenPeriodCount,
            DefaultNewPeriodOpenDay = request.DefaultNewPeriodOpenDay,
            DefaultPeriodCloseDay = request.DefaultPeriodCloseDay,
            LocalTimeOfDay = request.LocalTimeOfDay,
            TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId)
                ? DefaultTimeZone
                : request.TimeZoneId.Trim(),
            SupplementApprovalGraceDays = request.SupplementApprovalGraceDays,
            PostCloseAdjustmentDays = request.PostCloseAdjustmentDays,
            EffectiveFromYear = request.EffectiveFromYear,
            EffectiveFromMonth = request.EffectiveFromMonth,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            IsDeleted = false
        };
        PeriodScheduleCalculator.ValidateSettings(entity);
        _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>().Add(entity);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapSettings(entity);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException(
                "Không thể lưu cấu hình kỳ do xung đột phiên bản.", exception);
        }
    }

    public async Task<VppPeriodHorizonPreviewResDTO> PreviewHorizonAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        var nowUtc = CurrentUtc();
        var settings = await ResolveSettingsAsync(company, nowUtc, cancellationToken);
        var open = await GetOpenPeriodsAsync(company, cancellationToken);
        var proposed = BuildProposedPeriods(company, settings, open, nowUtc);
        return new VppPeriodHorizonPreviewResDTO
        {
            Settings = MapSettings(settings),
            CurrentOpenCount = open.Count,
            MissingCount = proposed.Count,
            ExistingOpenPeriods = await MapPeriodsAsync(open, cancellationToken),
            ProposedPeriods = proposed.Select(x => MapProposed(x.period, x.schedule)).ToArray()
        };
    }

    public async Task<IReadOnlyList<VppManagedPeriodResDTO>> TopUpOpenHorizonAsync(
        string memberCompanyCode,
        int actorUserId = 0,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        await AdvanceDuePeriodsAsync(company, cancellationToken);
        var nowUtc = CurrentUtc();
        var settings = await ResolveSettingsAsync(company, nowUtc, cancellationToken);
        var localNow = CurrentLocal(settings);
        var monthlyOpenBoundary = _scheduleCalculator.BoundaryLocal(
            localNow.Year,
            localNow.Month,
            settings.DefaultNewPeriodOpenDay,
            settings.LocalTimeOfDay);
        if (localNow < monthlyOpenBoundary)
        {
            return [];
        }

        var open = await GetOpenPeriodsAsync(company, cancellationToken);
        var proposed = BuildProposedPeriods(company, settings, open, nowUtc);
        var created = new List<VppPeriod>();
        foreach (var item in proposed)
        {
            cancellationToken.ThrowIfCancellationRequested();
            created.Add(await CreatePersistedAsync(
                company,
                item.period,
                settings,
                item.schedule,
                VppPeriodState.Open,
                actorUserId,
                "rolling-horizon-top-up",
                cancellationToken));
        }

        return await MapPeriodsAsync(created, cancellationToken);
    }

    public async Task<IReadOnlyList<VppManagedPeriodResDTO>> ListManagedAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
    {
        var company = NormalizeCompanyCode(memberCompanyCode);
        await AdvanceDuePeriodsAsync(company, cancellationToken);
        var rows = await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .Where(x => x.MemberCompanyCode == company)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .Take(48)
            .ToListAsync(cancellationToken);
        return await MapPeriodsAsync(rows, cancellationToken);
    }

    public async Task<VppManagedPeriodResDTO> CreateManualAsync(
        string memberCompanyCode,
        int actorUserId,
        VppOrderPeriodManualCreateReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var company = NormalizeCompanyCode(memberCompanyCode);
        var period = new Period(request.Year, request.Month);
        ValidatePeriod(period);
        var nowUtc = CurrentUtc();
        var settings = await ResolveSettingsAsync(company, nowUtc, cancellationToken);
        var currentLocal = ToBusinessLocal(nowUtc, settings.TimeZoneId);
        ValidateNotPastPeriod(period, currentLocal);
        if (await FindTrackedAsync(company, period, cancellationToken) is not null)
        {
            throw new ConflictException(
                $"Kỳ {period.Month:00}/{period.Year} đã tồn tại và không thể tạo trùng.");
        }
        var supplementDays = request.SupplementApprovalGraceDays;
        var adjustmentDays = request.PostCloseAdjustmentDays ?? settings.PostCloseAdjustmentDays;
        ValidateWindowDays(supplementDays, adjustmentDays);
        var supplementLocal = supplementDays.HasValue
            ? request.CloseAtLocal.AddDays(supplementDays.Value)
            : request.SupplementApprovalDeadlineLocal;
        ValidatePeriodWindows(
            request.CloseAtLocal,
            supplementLocal,
            request.CloseAtLocal.AddDays(adjustmentDays));
        var schedule = _scheduleCalculator.BuildExact(
            request.OpenAtLocal,
            request.CloseAtLocal,
            supplementLocal,
            request.CloseAtLocal.AddDays(adjustmentDays),
            settings.TimeZoneId);
        if (schedule.StartAtUtc < nowUtc)
        {
            throw new BusinessException("Ngày mở không được nằm trong quá khứ.");
        }
        if (schedule.SubmissionDeadlineUtc <= nowUtc)
        {
            throw new BusinessException("Không thể tạo kỳ có hạn nhận đơn đã qua.");
        }

        var state = schedule.StartAtUtc > nowUtc
            ? VppPeriodState.Scheduled
            : VppPeriodState.Open;
        var created = await CreatePersistedAsync(
            company,
            period,
            settings,
            schedule,
            state,
            actorUserId,
            request.Reason,
            cancellationToken);
        return (await MapPeriodsAsync([created], cancellationToken))[0];
    }

    public async Task<VppManagedPeriodResDTO> UpdateScheduleAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodUpdateReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        var orderCount = await CountOrdersAsync(period.Id, cancellationToken);
        if (period.State is not (VppPeriodState.Draft or VppPeriodState.Scheduled)
            && !(period.State == VppPeriodState.Open && orderCount == 0))
        {
            throw new ConflictException("Kỳ hiện tại không cho phép sửa lịch trực tiếp.");
        }

        var currentAdjustmentDays = ResolveWindowDays(
            period.SubmissionDeadlineUtc,
            period.PostCloseAdjustmentDeadlineUtc
                ?? period.SubmissionDeadlineUtc.AddDays(10));
        var supplementLocal = request.SupplementApprovalGraceDays.HasValue
            ? request.CloseAtLocal.AddDays(request.SupplementApprovalGraceDays.Value)
            : request.SupplementApprovalDeadlineLocal;
        var adjustmentDays = request.PostCloseAdjustmentDays ?? currentAdjustmentDays;
        ValidateWindowDays(request.SupplementApprovalGraceDays, adjustmentDays);
        var schedule = _scheduleCalculator.BuildExact(
            request.OpenAtLocal,
            request.CloseAtLocal,
            supplementLocal,
            request.CloseAtLocal.AddDays(adjustmentDays),
            period.TimeZoneId);
        if (period.State == VppPeriodState.Open
            && schedule.StartAtUtc != period.StartAtUtc)
        {
            throw new ConflictException("Không thể đổi ngày mở của kỳ đã bắt đầu.");
        }
        if (period.State == VppPeriodState.Open
            && schedule.SubmissionDeadlineUtc < period.SubmissionDeadlineUtc
            && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessException("Rút ngắn hạn nhận đơn bắt buộc nhập lý do.");
        }

        period.StartAtUtc = schedule.StartAtUtc;
        period.SubmissionDeadlineUtc = schedule.SubmissionDeadlineUtc;
        period.SupplementApprovalDeadlineUtc = schedule.SupplementApprovalDeadlineUtc;
        period.PostCloseAdjustmentDeadlineUtc = schedule.PostCloseAdjustmentDeadlineUtc;
        if (period.State is VppPeriodState.Draft or VppPeriodState.Scheduled)
        {
            period.State = schedule.StartAtUtc <= CurrentUtc()
                ? VppPeriodState.Open
                : VppPeriodState.Scheduled;
        }
        ApplyTransitionAudit(period, actorUserId, request.Reason ?? "schedule-updated");
        await SavePeriodAsync(period, cancellationToken);
        return (await MapPeriodsAsync([period], cancellationToken))[0];
    }

    public async Task<VppManagedPeriodResDTO> ExtendDeadlineAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodExtendDeadlineReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (period.State != VppPeriodState.Open)
        {
            throw new ConflictException("Chỉ kỳ đang nhận đơn mới được gia hạn.");
        }
        var closeUtc = PeriodCalculator.ToUtc(request.CloseAtLocal, period.TimeZoneId);
        var supplementLocal = request.SupplementApprovalGraceDays.HasValue
            ? request.CloseAtLocal.AddDays(request.SupplementApprovalGraceDays.Value)
            : request.SupplementApprovalDeadlineLocal;
        var supplementUtc = PeriodCalculator.ToUtc(supplementLocal, period.TimeZoneId);
        if (closeUtc <= period.SubmissionDeadlineUtc)
        {
            throw new BusinessException("Deadline mới phải muộn hơn deadline hiện tại.");
        }
        if (supplementUtc < closeUtc)
        {
            throw new BusinessException("Hạn duyệt đơn bổ sung không được sớm hơn hạn nhận đơn.");
        }
        var adjustmentDays = request.PostCloseAdjustmentDays
            ?? ResolveWindowDays(
                period.SubmissionDeadlineUtc,
                period.PostCloseAdjustmentDeadlineUtc
                    ?? period.SubmissionDeadlineUtc.AddDays(10));
        ValidateWindowDays(request.SupplementApprovalGraceDays, adjustmentDays);
        var adjustmentUtc = PeriodCalculator.ToUtc(
            request.CloseAtLocal.AddDays(adjustmentDays),
            period.TimeZoneId);
        if (adjustmentUtc < supplementUtc)
        {
            throw new BusinessException(
                "Thời hạn chỉnh đơn không được sớm hơn hạn nhận đơn bổ sung.");
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessException("Gia hạn kỳ bắt buộc nhập lý do.");
        }

        period.SubmissionDeadlineUtc = closeUtc;
        period.SupplementApprovalDeadlineUtc = supplementUtc;
        period.PostCloseAdjustmentDeadlineUtc = adjustmentUtc;
        ApplyTransitionAudit(period, actorUserId, request.Reason);
        await SavePeriodAsync(period, cancellationToken);
        return (await MapPeriodsAsync([period], cancellationToken))[0];
    }

    public async Task<VppManagedPeriodResDTO> CloseSubmissionsAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (period.State != VppPeriodState.Open)
        {
            throw new ConflictException("Kỳ không còn ở trạng thái nhận đơn.");
        }
        var nowUtc = CurrentUtc();
        if (nowUtc < period.SubmissionDeadlineUtc && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessException("Đóng nhận đơn sớm bắt buộc nhập lý do.");
        }

        var supplementDays = ResolveWindowDays(
            period.SubmissionDeadlineUtc,
            period.SupplementApprovalDeadlineUtc);
        var adjustmentDays = ResolveWindowDays(
            period.SubmissionDeadlineUtc,
            period.PostCloseAdjustmentDeadlineUtc
                ?? period.SubmissionDeadlineUtc.AddDays(10));
        period.SubmissionDeadlineUtc = nowUtc;
        period.SupplementApprovalDeadlineUtc = nowUtc.AddDays(supplementDays);
        period.PostCloseAdjustmentDeadlineUtc = nowUtc.AddDays(adjustmentDays);
        period.State = VppPeriodState.SubmissionClosed;
        ApplyTransitionAudit(period, actorUserId, request.Reason ?? "submission-deadline-reached");
        await SavePeriodAsync(period, cancellationToken);
        return (await MapPeriodsAsync([period], cancellationToken))[0];
    }

    public async Task<VppManagedPeriodResDTO> ReopenSubmissionsAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodReopenSubmissionsReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (period.State != VppPeriodState.SubmissionClosed)
        {
            throw new ConflictException(
                "Chỉ kỳ đã khóa và chưa chuyển sang chốt giá mới được mở lại nhận đơn.");
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BusinessException("Mở lại nhận đơn bắt buộc nhập lý do.");
        }

        var nowUtc = CurrentUtc();
        var closeUtc = PeriodCalculator.ToUtc(request.CloseAtLocal, period.TimeZoneId);
        var supplementUtc = PeriodCalculator.ToUtc(
            request.SupplementApprovalDeadlineLocal,
            period.TimeZoneId);
        if (closeUtc <= nowUtc)
        {
            throw new BusinessException("Ngày đóng mới phải ở tương lai.");
        }
        if (supplementUtc < closeUtc)
        {
            throw new BusinessException(
                "Hạn duyệt đơn bổ sung không được sớm hơn ngày đóng mới.");
        }

        var adjustmentDays = ResolveWindowDays(
            period.SubmissionDeadlineUtc,
            period.PostCloseAdjustmentDeadlineUtc
                ?? period.SubmissionDeadlineUtc.AddDays(10));
        var adjustmentUtc = closeUtc.AddDays(adjustmentDays);
        if (adjustmentUtc < supplementUtc)
        {
            adjustmentUtc = supplementUtc;
        }

        period.SubmissionDeadlineUtc = closeUtc;
        period.SupplementApprovalDeadlineUtc = supplementUtc;
        period.PostCloseAdjustmentDeadlineUtc = adjustmentUtc;
        period.State = VppPeriodState.Open;
        ApplyTransitionAudit(period, actorUserId, request.Reason);
        await SavePeriodAsync(period, cancellationToken);
        return (await MapPeriodsAsync([period], cancellationToken))[0];
    }

    public async Task DeleteAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (await HasPeriodDependenciesAsync(period.Id, cancellationToken))
        {
            throw new ConflictException("Không thể vô hiệu hóa kỳ đã có đơn hoặc dữ liệu chốt.");
        }
        period.IsDeleted = true;
        ApplyTransitionAudit(period, actorUserId, request.Reason ?? "period-deactivated");
        await SavePeriodAsync(period, cancellationToken);
    }

    public async Task<VppManagedPeriodResDTO> RestoreAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredAnyTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (!period.IsDeleted)
        {
            throw new ConflictException("Kỳ đặt hàng đang hoạt động.");
        }
        if (await HasPeriodDependenciesAsync(period.Id, cancellationToken))
        {
            throw new ConflictException("Không thể khôi phục kỳ đã có dữ liệu liên quan.");
        }
        if (await FindTrackedAsync(
                period.MemberCompanyCode,
                new Period(period.Year, period.Month),
                cancellationToken) is not null)
        {
            throw new ConflictException(
                $"Kỳ {period.Month:00}/{period.Year} đang có một bản hoạt động khác.");
        }

        var nowUtc = CurrentUtc();
        ValidateNotPastPeriod(
            new Period(period.Year, period.Month),
            ToBusinessLocal(nowUtc, period.TimeZoneId));
        if (period.SubmissionDeadlineUtc <= nowUtc)
        {
            throw new ConflictException("Kỳ đã hết hạn nên không thể khôi phục.");
        }

        var adjustmentUtc = period.PostCloseAdjustmentDeadlineUtc
            ?? period.SubmissionDeadlineUtc.AddDays(10);
        var schedule = new PeriodSchedule(
            ToBusinessLocal(period.StartAtUtc, period.TimeZoneId),
            ToBusinessLocal(period.SubmissionDeadlineUtc, period.TimeZoneId),
            ToBusinessLocal(period.SupplementApprovalDeadlineUtc, period.TimeZoneId),
            ToBusinessLocal(adjustmentUtc, period.TimeZoneId),
            period.StartAtUtc,
            period.SubmissionDeadlineUtc,
            period.SupplementApprovalDeadlineUtc,
            adjustmentUtc);
        period.IsDeleted = false;
        period.State = ResolveInitialState(schedule, nowUtc);
        ApplyTransitionAudit(period, actorUserId, request.Reason ?? "period-restored");
        await SavePeriodAsync(period, cancellationToken);
        return (await MapPeriodsAsync([period], cancellationToken))[0];
    }

    public async Task HardDeleteAsync(
        Guid periodId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var period = await FindRequiredAnyTrackedAsync(periodId, cancellationToken);
        EnsureRowVersion(period.RowVersion, request.RowVersion);
        if (!period.IsDeleted)
        {
            throw new ConflictException("Hãy vô hiệu hóa kỳ trước khi xóa vĩnh viễn.");
        }
        if (await HasPeriodDependenciesAsync(period.Id, cancellationToken))
        {
            throw new ConflictException("Không thể xóa kỳ đã có đơn hoặc dữ liệu chốt.");
        }

        _unitOfWork.VPPContext.Set<VppPeriod>().Remove(period);
        await SavePeriodAsync(period, cancellationToken);
    }

    public async Task<IReadOnlyList<VppPeriod>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var companies = await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => x.MemberCompanyCode)
            .Concat(_unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.MemberCompanyCode))
            .Distinct()
            .ToListAsync(cancellationToken);
        var transitioned = new List<VppPeriod>();
        foreach (var company in companies)
        {
            transitioned.AddRange(await AdvanceCompanyAsync(company, cancellationToken));
        }
        return transitioned;
    }

    public async Task AdvanceDuePeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default)
        => _ = await AdvanceCompanyAsync(
            NormalizeCompanyCode(memberCompanyCode),
            cancellationToken);

    public async Task TransitionAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => _ = await TransitionWithResultAsync(
            periodId,
            targetState,
            actorUserId,
            reason,
            enforceDueBoundary: true,
            cancellationToken);

    public static bool IsLegalTransition(
        VppPeriodState currentState,
        VppPeriodState targetState)
        => (currentState, targetState) switch
        {
            (VppPeriodState.Draft, VppPeriodState.Scheduled) => true,
            (VppPeriodState.Draft, VppPeriodState.Open) => true,
            (VppPeriodState.Scheduled, VppPeriodState.Open) => true,
            (VppPeriodState.Open, VppPeriodState.SubmissionClosed) => true,
            (VppPeriodState.SubmissionClosed, VppPeriodState.Pricing) => true,
            (VppPeriodState.Pricing, VppPeriodState.Settled) => true,
            _ => false
        };

    private async Task<IReadOnlyList<VppPeriod>> AdvanceCompanyAsync(
        string company,
        CancellationToken cancellationToken)
    {
        var transitioned = new List<VppPeriod>();
        for (var pass = 0; pass < 3; pass++)
        {
            var nowUtc = CurrentUtc();
            var candidates = await _unitOfWork.VPPContext.Set<VppPeriod>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                    && x.MemberCompanyCode == company
                    && ((x.State == VppPeriodState.Scheduled && x.StartAtUtc <= nowUtc)
                        || (x.State == VppPeriodState.Open
                            && x.SubmissionDeadlineUtc <= nowUtc)
                        || (x.State == VppPeriodState.SubmissionClosed
                            && x.SupplementApprovalDeadlineUtc <= nowUtc)))
                .OrderBy(x => x.StartAtUtc)
                .Select(x => new { x.Id, x.State })
                .ToListAsync(cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var candidate in candidates)
            {
                var target = ResolveAutomaticTargetState(candidate.State);
                try
                {
                    transitioned.Add(await TransitionWithResultAsync(
                        candidate.Id,
                        target,
                        actorUserId: 0,
                        reason: "period-recovery",
                        enforceDueBoundary: true,
                        cancellationToken));
                }
                catch (ConflictException exception)
                {
                    _logger.LogDebug(
                        exception,
                        "Period {PeriodId} advanced concurrently.",
                        candidate.Id);
                }
            }
        }
        return transitioned;
    }

    private async Task<VppPeriod> TransitionWithResultAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason,
        bool enforceDueBoundary,
        CancellationToken cancellationToken)
    {
        var period = await FindRequiredTrackedAsync(periodId, cancellationToken);
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
        if (enforceDueBoundary && !HasReachedTransitionBoundary(period, targetState, nowUtc))
        {
            throw new ConflictException("Kỳ chưa đến mốc chuyển trạng thái.");
        }

        period.State = targetState;
        ApplyTransitionAudit(period, actorUserId, reason);
        await SavePeriodAsync(period, cancellationToken);
        return period;
    }

    private static VppPeriodState ResolveAutomaticTargetState(VppPeriodState currentState)
        => currentState switch
        {
            VppPeriodState.Scheduled => VppPeriodState.Open,
            VppPeriodState.Open => VppPeriodState.SubmissionClosed,
            VppPeriodState.SubmissionClosed => VppPeriodState.Pricing,
            _ => throw new ConflictException("Trạng thái kỳ không hỗ trợ chuyển tự động.")
        };

    private static bool HasReachedTransitionBoundary(
        VppPeriod period,
        VppPeriodState targetState,
        DateTime nowUtc)
    {
        // Mỗi đích có một mốc riêng; chuyển thủ công vẫn dùng cùng luật để không đi trước lịch kỳ.
        return targetState switch
        {
            VppPeriodState.Open => nowUtc >= period.StartAtUtc,
            VppPeriodState.SubmissionClosed => nowUtc >= period.SubmissionDeadlineUtc,
            VppPeriodState.Pricing => nowUtc >= period.SupplementApprovalDeadlineUtc,
            _ => true
        };
    }

    private List<(Period period, PeriodSchedule schedule)> BuildProposedPeriods(
        string company,
        VppOrderPeriodSettingsVersion settings,
        IReadOnlyList<VppPeriod> open,
        DateTime nowUtc)
    {
        var targetCount = AutomaticOpenPeriodCount;

        var localNow = CurrentLocal(settings);
        var anchor = ResolveAnchor(settings, localNow);
        var openedFromMonth = new Period(localNow.Year, localNow.Month);
        var openKeys = open
            .Select(x => (x.Year, x.Month))
            .ToHashSet();
        var existingKeys = _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.MemberCompanyCode == company)
            .Select(x => new { x.Year, x.Month })
            .AsEnumerable()
            .Select(x => (x.Year, x.Month))
            .ToHashSet();
        var proposed = new List<(Period period, PeriodSchedule schedule)>();
        var coveredCount = 0;
        for (var offset = 0; coveredCount < targetCount && offset <= 36; offset++)
        {
            var target = AddMonths(anchor, offset);
            var key = (target.Year, target.Month);
            if (openKeys.Contains(key))
            {
                coveredCount++;
                continue;
            }
            if (existingKeys.Contains(key))
            {
                continue;
            }

            var schedule = _scheduleCalculator.Build(target, openedFromMonth, settings);
            if (schedule.SubmissionDeadlineUtc <= nowUtc)
            {
                continue;
            }
            proposed.Add((target, schedule));
            coveredCount++;
        }
        return proposed;
    }

    private async Task<VppPeriod> CreatePersistedAsync(
        string company,
        Period period,
        VppOrderPeriodSettingsVersion settings,
        PeriodSchedule schedule,
        VppPeriodState state,
        int actorUserId,
        string? reason,
        CancellationToken cancellationToken)
    {
        var existing = await FindTrackedAsync(company, period, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var nowUtc = CurrentUtc();
        var entity = new VppPeriod
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = company,
            Year = period.Year,
            Month = period.Month,
            SettingsVersionId = settings.Id == Guid.Empty ? null : settings.Id,
            TimeZoneId = settings.TimeZoneId,
            StartAtUtc = schedule.StartAtUtc,
            SubmissionDeadlineUtc = schedule.SubmissionDeadlineUtc,
            SupplementApprovalDeadlineUtc = schedule.SupplementApprovalDeadlineUtc,
            PostCloseAdjustmentDeadlineUtc = schedule.PostCloseAdjustmentDeadlineUtc,
            State = state,
            LastTransitionUserId = actorUserId == 0 ? null : actorUserId,
            LastTransitionAtUtc = nowUtc,
            LastTransitionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
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
            _unitOfWork.VPPContext.Entry(entity).State = EntityState.Detached;
            var winner = await GetAsync(company, period, cancellationToken);
            if (winner is not null)
            {
                return winner;
            }
            throw new ConflictException("Không thể tạo kỳ do xung đột đồng thời.", exception);
        }
    }

    private async Task<VppOrderPeriodSettingsVersion> ResolveSettingsAsync(
        string company,
        DateTime atUtc,
        CancellationToken cancellationToken)
    {
        var local = ToBusinessLocal(atUtc, DefaultTimeZone);
        var effectiveKey = (local.Year * 100) + local.Month;
        return await _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.MemberCompanyCode == company
                && ((x.EffectiveFromYear * 100) + x.EffectiveFromMonth) <= effectiveKey)
            .OrderByDescending(x => x.EffectiveFromYear)
            .ThenByDescending(x => x.EffectiveFromMonth)
            .ThenByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? CreateDefaultSettings(company, local);
    }

    private static VppOrderPeriodSettingsVersion CreateDefaultSettings(
        string company,
        DateTime localNow)
        => new()
        {
            Id = Guid.Empty,
            MemberCompanyCode = company,
            VersionNumber = 0,
            Name = "Mặc định 1 kỳ · ngày 05",
            DefaultOpenPeriodCount = AutomaticOpenPeriodCount,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            LocalTimeOfDay = TimeSpan.Zero,
            TimeZoneId = DefaultTimeZone,
            SupplementApprovalGraceDays = 5,
            PostCloseAdjustmentDays = 10,
            SettlementReopenWindowDays = 3,
            EffectiveFromYear = localNow.Year,
            EffectiveFromMonth = localNow.Month
        };

    private async Task<int> NextSettingsVersionAsync(
        string company,
        CancellationToken cancellationToken)
        => (await _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
            .AsNoTracking()
            .Where(x => x.MemberCompanyCode == company)
            .MaxAsync(x => (int?)x.VersionNumber, cancellationToken) ?? 0) + 1;

    private async Task<IReadOnlyList<VppManagedPeriodResDTO>> MapPeriodsAsync(
        IReadOnlyCollection<VppPeriod> periods,
        CancellationToken cancellationToken)
    {
        if (periods.Count == 0)
        {
            return [];
        }
        var ids = periods.Select(x => x.Id).ToArray();
        var settingsIds = periods
            .Where(x => x.SettingsVersionId.HasValue)
            .Select(x => x.SettingsVersionId!.Value)
            .Distinct()
            .ToArray();
        var adjustmentDays = settingsIds.Length == 0
            ? new Dictionary<Guid, int>()
            : await _unitOfWork.VPPContext.Set<VppOrderPeriodSettingsVersion>()
                .AsNoTracking()
                .Where(x => settingsIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.PostCloseAdjustmentDays, cancellationToken);
        var counts = await _unitOfWork.VPPContext.Set<VppRequest>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsCurrentRevision
                && x.PeriodId.HasValue && ids.Contains(x.PeriodId.Value))
            .GroupBy(x => x.PeriodId!.Value)
            .Select(group => new { PeriodId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(x => x.PeriodId, x => x.Count, cancellationToken);
        var requestPeriodIds = await _unitOfWork.VPPContext.Set<VppRequest>()
            .AsNoTracking()
            .Where(x => x.PeriodId.HasValue && ids.Contains(x.PeriodId.Value))
            .Select(x => x.PeriodId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var settlementPeriodIds = await _unitOfWork.VPPContext.Set<Settlement>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.PeriodId))
            .Select(x => x.PeriodId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var correctionPeriodIds = await _unitOfWork.VPPContext.Set<PostSettlementOrderCorrection>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.PeriodId))
            .Select(x => x.PeriodId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var dependencyPeriodIds = requestPeriodIds
            .Concat(settlementPeriodIds)
            .Concat(correctionPeriodIds)
            .ToHashSet();
        var companies = periods.Select(x => x.MemberCompanyCode).Distinct().ToArray();
        var years = periods.Select(x => x.Year).Distinct().ToArray();
        var months = periods.Select(x => x.Month).Distinct().ToArray();
        var activeRows = await _unitOfWork.VPPContext.Set<VppPeriod>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && companies.Contains(x.MemberCompanyCode)
                && years.Contains(x.Year)
                && months.Contains(x.Month))
            .Select(x => new { x.Id, x.MemberCompanyCode, x.Year, x.Month })
            .ToListAsync(cancellationToken);
        var activePeriodKeys = activeRows
            .GroupBy(
                x => PeriodKey(x.MemberCompanyCode, x.Year, x.Month),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Id,
                StringComparer.OrdinalIgnoreCase);
        var nowUtc = CurrentUtc();
        return periods
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .Select(period => MapPeriod(
                period,
                counts.GetValueOrDefault(period.Id),
                period.SettingsVersionId.HasValue
                    ? adjustmentDays.GetValueOrDefault(period.SettingsVersionId.Value, 10)
                    : 10,
                dependencyPeriodIds.Contains(period.Id),
                activePeriodKeys.TryGetValue(
                    PeriodKey(period.MemberCompanyCode, period.Year, period.Month),
                    out var activeId) && activeId != period.Id,
                nowUtc))
            .ToArray();
    }

    private VppManagedPeriodResDTO MapPeriod(
        VppPeriod period,
        int orderCount,
        int postCloseAdjustmentDays,
        bool hasDependencies,
        bool hasActiveDuplicate,
        DateTime nowUtc)
    {
        var adjustmentDeadlineUtc = period.PostCloseAdjustmentDeadlineUtc
            ?? period.SubmissionDeadlineUtc.AddDays(postCloseAdjustmentDays);
        var currentLocal = ToBusinessLocal(nowUtc, period.TimeZoneId);
        var canRestore = period.IsDeleted
            && !hasDependencies
            && !hasActiveDuplicate
            && period.SubmissionDeadlineUtc > nowUtc
            && !IsPastPeriod(new Period(period.Year, period.Month), currentLocal);
        return new()
        {
            Id = period.Id,
            IsDeleted = period.IsDeleted,
            Year = period.Year,
            Month = period.Month,
            State = period.State.ToString(),
            DisplayState = period.State switch
            {
                VppPeriodState.Draft or VppPeriodState.Scheduled => "Upcoming",
                VppPeriodState.Open => "Open",
                VppPeriodState.SubmissionClosed or VppPeriodState.Pricing => "Closed",
                _ => "Settled"
            },
            OpenAtLocal = ToBusinessLocal(period.StartAtUtc, period.TimeZoneId),
            CloseAtLocal = ToBusinessLocal(period.SubmissionDeadlineUtc, period.TimeZoneId),
            SupplementApprovalDeadlineLocal = ToBusinessLocal(
                period.SupplementApprovalDeadlineUtc,
                period.TimeZoneId),
            PostCloseAdjustmentDeadlineLocal = ToBusinessLocal(
                adjustmentDeadlineUtc,
                period.TimeZoneId),
            HasOrders = orderCount > 0,
            OrderCount = orderCount,
            CanEditSchedule = !period.IsDeleted
                && (period.State is VppPeriodState.Draft or VppPeriodState.Scheduled
                    || (period.State == VppPeriodState.Open && orderCount == 0)),
            CanDeactivate = !period.IsDeleted && !hasDependencies,
            CanRestore = canRestore,
            CanHardDelete = period.IsDeleted && !hasDependencies,
            CanDelete = !period.IsDeleted && !hasDependencies,
            CanExtendDeadline = !period.IsDeleted && period.State == VppPeriodState.Open,
            CanCloseSubmissions = !period.IsDeleted && period.State == VppPeriodState.Open,
            CanReopenSubmissions = !period.IsDeleted && period.State == VppPeriodState.SubmissionClosed,
            CanAdjustOrders = !period.IsDeleted
                && (period.State is VppPeriodState.SubmissionClosed or VppPeriodState.Pricing)
                && nowUtc < adjustmentDeadlineUtc,
            CanCorrectSettledOrders = !period.IsDeleted && period.State == VppPeriodState.Settled,
            LastTransitionReason = period.LastTransitionReason,
            LastTransitionUserId = period.LastTransitionUserId,
            LastTransitionAtLocal = period.LastTransitionAtUtc.HasValue
                ? ToBusinessLocal(period.LastTransitionAtUtc.Value, period.TimeZoneId)
                : null,
            RowVersion = period.RowVersion
        };
    }

    private VppManagedPeriodResDTO MapProposed(Period period, PeriodSchedule schedule)
        => new()
        {
            Year = period.Year,
            Month = period.Month,
            State = VppPeriodState.Open.ToString(),
            DisplayState = "Open",
            OpenAtLocal = schedule.StartAtLocal,
            CloseAtLocal = schedule.SubmissionDeadlineLocal,
            SupplementApprovalDeadlineLocal = schedule.SupplementApprovalDeadlineLocal,
            PostCloseAdjustmentDeadlineLocal = schedule.PostCloseAdjustmentDeadlineLocal
        };

    private static VppOrderPeriodSettingsResDTO MapSettings(
        VppOrderPeriodSettingsVersion entity)
        => new()
        {
            Id = entity.Id == Guid.Empty ? null : entity.Id,
            VersionNumber = entity.VersionNumber,
            Name = entity.Name,
            DefaultOpenPeriodCount = AutomaticOpenPeriodCount,
            DefaultNewPeriodOpenDay = entity.DefaultNewPeriodOpenDay,
            DefaultPeriodCloseDay = entity.DefaultPeriodCloseDay,
            LocalTimeOfDay = entity.LocalTimeOfDay,
            TimeZoneId = entity.TimeZoneId,
            SupplementApprovalGraceDays = entity.SupplementApprovalGraceDays,
            PostCloseAdjustmentDays = entity.PostCloseAdjustmentDays,
            EffectiveFromYear = entity.EffectiveFromYear,
            EffectiveFromMonth = entity.EffectiveFromMonth,
            CreatedByUserId = entity.CreatedByUserId,
            CreatedAtUtc = entity.CreatedAtUtc,
            RowVersion = entity.RowVersion
        };

    private async Task<VppPeriod?> FindTrackedAsync(
        string company,
        Period period,
        CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<VppPeriod>()
            .FirstOrDefaultAsync(x => !x.IsDeleted
                && x.MemberCompanyCode == company
                && x.Year == period.Year
                && x.Month == period.Month,
                cancellationToken);

    private async Task<VppPeriod> FindRequiredTrackedAsync(
        Guid periodId,
        CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<VppPeriod>()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy kỳ đặt hàng.");

    private async Task<VppPeriod> FindRequiredAnyTrackedAsync(
        Guid periodId,
        CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<VppPeriod>()
            .FirstOrDefaultAsync(x => x.Id == periodId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy kỳ đặt hàng.");

    private async Task<int> CountOrdersAsync(Guid periodId, CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<VppRequest>()
            .AsNoTracking()
            .CountAsync(x => !x.IsDeleted && x.IsCurrentRevision && x.PeriodId == periodId,
                cancellationToken);

    private async Task<bool> HasPeriodDependenciesAsync(
        Guid periodId,
        CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<VppRequest>()
                .AsNoTracking()
                .AnyAsync(x => x.PeriodId == periodId, cancellationToken)
            || await _unitOfWork.VPPContext.Set<Settlement>()
                .AsNoTracking()
                .AnyAsync(x => x.PeriodId == periodId, cancellationToken)
            || await _unitOfWork.VPPContext.Set<PostSettlementOrderCorrection>()
                .AsNoTracking()
                .AnyAsync(x => x.PeriodId == periodId, cancellationToken);

    private async Task SavePeriodAsync(
        VppPeriod period,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(
                "Kỳ vừa được cập nhật bởi người dùng hoặc tiến trình khác.",
                exception);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException("Không thể cập nhật kỳ do xung đột dữ liệu.", exception);
        }
    }

    private void ApplyTransitionAudit(VppPeriod period, int actorUserId, string? reason)
    {
        var nowUtc = CurrentUtc();
        period.LastTransitionUserId = actorUserId == 0 ? null : actorUserId;
        period.LastTransitionAtUtc = nowUtc;
        period.LastTransitionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        period.UpdatedByUserId = actorUserId;
        period.UpdatedAtUtc = nowUtc;
    }

    private DateTime CurrentUtc()
        => PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);

    // Trạng thái ban đầu chỉ dựa trên mốc lịch; chuyển trạng thái sau đó do advance aggregate xử lý.
    private static VppPeriodState ResolveInitialState(
        PeriodSchedule schedule,
        DateTime nowUtc)
        => nowUtc < schedule.StartAtUtc
            ? VppPeriodState.Scheduled
            : nowUtc >= schedule.SupplementApprovalDeadlineUtc
                ? VppPeriodState.Pricing
                : nowUtc >= schedule.SubmissionDeadlineUtc
                    ? VppPeriodState.SubmissionClosed
                    : VppPeriodState.Open;

    private DateTime CurrentLocal(VppOrderPeriodSettingsVersion settings)
        => ToBusinessLocal(CurrentUtc(), settings.TimeZoneId);

    private static DateTime ToBusinessLocal(DateTime utc, string? timeZoneId)
    {
        var normalized = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(
                normalized,
                PeriodCalculator.ResolveTimeZone(
                    string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZone : timeZoneId)),
            DateTimeKind.Unspecified);
    }

    private static Period ResolveAnchor(
        VppOrderPeriodSettingsVersion settings,
        DateTime localNow)
        => new PeriodCalculator(settings.DefaultPeriodCloseDay).Current(localNow);

    private static Period AddMonths(Period period, int months)
    {
        var value = new DateTime(period.Year, period.Month, 1).AddMonths(months);
        return new Period(value.Year, value.Month);
    }

    private static void EnsureRowVersion(byte[]? actual, byte[]? expected)
    {
        if (expected is not { Length: > 0 })
        {
            throw new BusinessException("RowVersion is required. Refresh and try again.");
        }
        if (actual is null || !actual.AsSpan().SequenceEqual(expected))
        {
            throw new ConflictException("Kỳ đã thay đổi. Hãy tải lại trước khi thao tác.");
        }
    }

    private static string NormalizeCompanyCode(string memberCompanyCode)
    {
        if (string.IsNullOrWhiteSpace(memberCompanyCode))
        {
            throw new ArgumentException(
                "Member company code is required.",
                nameof(memberCompanyCode));
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
        if (period.Year is < 1 or > 9999 || period.Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(period));
        }
    }

    private static void ValidateNotPastPeriod(Period period, DateTime currentLocal)
    {
        // Kỳ là nhãn tháng nghiệp vụ: lịch mở/đóng tương lai không được dùng để tạo bù tháng đã qua.
        var requestedKey = (period.Year * 100) + period.Month;
        var currentKey = (currentLocal.Year * 100) + currentLocal.Month;
        if (requestedKey < currentKey)
        {
            throw new BusinessException("Không thể tạo kỳ đặt hàng trong quá khứ.");
        }
    }

    private static bool IsPastPeriod(Period period, DateTime currentLocal)
        => (period.Year * 100) + period.Month < (currentLocal.Year * 100) + currentLocal.Month;

    private static string PeriodKey(string company, int year, int month)
        => $"{company.Trim()}|{year:D4}|{month:D2}";

    private static int ResolveWindowDays(DateTime closeAtUtc, DateTime deadlineUtc)
        => Math.Max(0, (int)Math.Round(
            (deadlineUtc - closeAtUtc).TotalDays,
            MidpointRounding.AwayFromZero));

    private static void ValidateWindowDays(int? supplementDays, int adjustmentDays)
    {
        if (supplementDays is < 0 or > 31)
        {
            throw new BusinessException("Số ngày nhận đơn bổ sung phải từ 0 đến 31.");
        }
        if (adjustmentDays is < 0 or > 31)
        {
            throw new BusinessException("Số ngày chỉnh đơn phải từ 0 đến 31.");
        }
        if (supplementDays.HasValue && supplementDays.Value > adjustmentDays)
        {
            throw new BusinessException(
                "Số ngày chỉnh đơn phải bằng hoặc dài hơn thời gian nhận đơn bổ sung.");
        }
    }

    private static void ValidatePeriodWindows(
        DateTime closeAtLocal,
        DateTime supplementDeadlineLocal,
        DateTime adjustmentDeadlineLocal)
    {
        if (supplementDeadlineLocal < closeAtLocal)
        {
            throw new BusinessException(
                "Hạn nhận đơn bổ sung không được sớm hơn ngày đóng kỳ.");
        }
        if (adjustmentDeadlineLocal < supplementDeadlineLocal)
        {
            throw new BusinessException(
                "Thời hạn chỉnh đơn không được sớm hơn hạn nhận đơn bổ sung.");
        }
    }
}
