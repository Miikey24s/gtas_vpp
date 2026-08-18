using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gtas_vpp_be.Tests.Domain;

public sealed class VppPeriodServiceTests
{
    [Fact]
    public async Task Ensure_is_idempotent_for_company_and_label()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(
            context,
            new DateTime(2026, 7, 15, 9, 0, 0));

        var first = await service.EnsureAsync("ACME", new Period(2026, 7));
        var second = await service.EnsureAsync("ACME", new Period(2026, 7));

        Assert.Equal(first.Id, second.Id);
        Assert.Single(context.Periods);
        Assert.Equal(VppPeriodState.Open, first.State);
        Assert.Equal("Asia/Ho_Chi_Minh", first.TimeZoneId);
    }

    [Fact]
    public async Task Recovery_catches_up_both_automatic_states_in_one_run()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(
            context,
            new DateTime(2026, 8, 11, 9, 0, 0));
        var period = await service.EnsureAsync("ACME", new Period(2026, 7));

        await service.AdvanceDuePeriodsAsync("ACME");

        var recovered = await service.GetAsync(period.Id);
        Assert.NotNull(recovered);
        Assert.Equal(VppPeriodState.Pricing, recovered.State);
        Assert.NotNull(recovered.LastTransitionAtUtc);

        var secondRun = await service.AdvanceDuePeriodsAsync();
        Assert.Empty(secondRun);
    }

    [Fact]
    public async Task Transition_before_authoritative_deadline_returns_conflict()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(
            context,
            new DateTime(2026, 7, 15, 9, 0, 0));
        var period = await service.EnsureAsync("ACME", new Period(2026, 7));

        await Assert.ThrowsAsync<ConflictException>(() => service.TransitionAsync(
            period.Id,
            VppPeriodState.SubmissionClosed,
            actorUserId: 1,
            reason: "manual"));
    }

    [Fact]
    public async Task Recovery_on_empty_database_is_a_stable_no_op()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(
            context,
            new DateTime(2026, 7, 15, 9, 0, 0));

        var first = await service.AdvanceDuePeriodsAsync();
        var second = await service.AdvanceDuePeriodsAsync();

        Assert.Empty(first);
        Assert.Empty(second);
        Assert.Empty(context.Periods);
    }

    [Fact]
    public async Task Top_up_keeps_one_automatic_period()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(
            context,
            new DateTime(2026, 8, 26, 9, 0, 0));

        var created = await service.TopUpOpenHorizonAsync("ACME");

        var period = Assert.Single(created);
        Assert.Equal((2026, 8), (period.Year, period.Month));
        Assert.Equal(new DateTime(2026, 9, 5), period.CloseAtLocal);
    }

    [Fact]
    public async Task Default_settings_use_five_day_supplement_approval_grace()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));

        var settings = await service.GetSettingsAsync("ACME");
        var created = await service.TopUpOpenHorizonAsync("ACME");

        Assert.Equal(5, settings.SupplementApprovalGraceDays);
        Assert.Equal(10, settings.PostCloseAdjustmentDays);
        Assert.All(created, period =>
        {
            Assert.Equal(period.CloseAtLocal.AddDays(5), period.SupplementApprovalDeadlineLocal);
            Assert.Equal(period.CloseAtLocal.AddDays(10), period.PostCloseAdjustmentDeadlineLocal);
        });
    }

    [Fact]
    public async Task Settings_history_keeps_versions_and_accepts_a_custom_grace_period()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));

        await service.SaveSettingsAsync("ACME", 5615, new VppOrderPeriodSettingsReqDTO
        {
            Name = "Mặc định 5 ngày",
            SupplementApprovalGraceDays = 5,
            EffectiveFromYear = 2026,
            EffectiveFromMonth = 8
        });
        await service.SaveSettingsAsync("ACME", 5616, new VppOrderPeriodSettingsReqDTO
        {
            Name = "Ngoại lệ 2 ngày",
            SupplementApprovalGraceDays = 2,
            EffectiveFromYear = 2026,
            EffectiveFromMonth = 9
        });

        var history = await service.ListSettingsVersionsAsync("ACME");

        Assert.Collection(
            history,
            latest =>
            {
                Assert.Equal(2, latest.VersionNumber);
                Assert.Equal(2, latest.SupplementApprovalGraceDays);
                Assert.Equal(5616, latest.CreatedByUserId);
            },
            previous =>
            {
                Assert.Equal(1, previous.VersionNumber);
                Assert.Equal(5, previous.SupplementApprovalGraceDays);
                Assert.Equal(5615, previous.CreatedByUserId);
            });
    }

    [Fact]
    public async Task Rolling_transition_closes_due_period_and_opens_next_month()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var clock = new FakeDateTimeProvider(new DateTime(2026, 8, 26, 9, 0, 0));
        var service = CreateService(context, clock);
        _ = await service.TopUpOpenHorizonAsync("ACME");

        clock.Now = new DateTime(2026, 9, 5, 1, 0, 0);
        await service.AdvanceDuePeriodsAsync("ACME");
        _ = await service.TopUpOpenHorizonAsync("ACME");

        var open = await service.GetOpenPeriodsAsync("ACME");
        Assert.Equal(
            [(2026, 9)],
            open.Select(x => (x.Year, x.Month)).ToArray());
        Assert.Equal(
            VppPeriodState.SubmissionClosed,
            context.Periods.Single(x => x.Year == 2026 && x.Month == 8).State);
    }

    [Fact]
    public async Task Future_settings_remain_visible_but_do_not_apply_before_effective_month()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        await service.SaveSettingsAsync("ACME", 5615, new VppOrderPeriodSettingsReqDTO
        {
            Name = "Tạm dừng từ tháng 09",
            DefaultOpenPeriodCount = 0,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            EffectiveFromYear = 2026,
            EffectiveFromMonth = 9
        });

        var displayed = await service.GetSettingsAsync("ACME");
        var effective = await service.GetEffectiveSettingsAsync("ACME");
        var created = await service.TopUpOpenHorizonAsync("ACME");

        Assert.Equal(1, displayed.DefaultOpenPeriodCount);
        Assert.Equal(9, displayed.EffectiveFromMonth);
        Assert.Equal(1, effective.DefaultOpenPeriodCount);
        Assert.Equal(8, effective.EffectiveFromMonth);
        Assert.Single(created);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12)]
    public async Task Generator_ignores_legacy_horizon_count_and_keeps_one_period(int horizonCount)
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        await SaveCurrentSettingsAsync(service, horizonCount, 2026, 8);

        var created = await service.TopUpOpenHorizonAsync("ACME");

        Assert.Single(created);
        Assert.Single(await service.GetOpenPeriodsAsync("ACME"));
    }

    [Fact]
    public async Task Generator_rolls_over_year_without_invalid_months()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 12, 26, 9, 0, 0));

        var created = await service.TopUpOpenHorizonAsync("ACME");

        Assert.Equal(
            [(2026, 12)],
            created.Select(x => (x.Year, x.Month)).ToArray());
    }

    [Fact]
    public async Task Sparse_manual_period_does_not_replace_missing_default_horizon_months()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        _ = await service.CreateManualAsync("ACME", 5615, new VppOrderPeriodManualCreateReqDTO
        {
            Year = 2026,
            Month = 11,
            OpenAtLocal = new DateTime(2026, 8, 26, 8, 0, 0),
            CloseAtLocal = new DateTime(2026, 12, 5),
            SupplementApprovalDeadlineLocal = new DateTime(2026, 12, 7),
            Reason = "Mở riêng kỳ tháng 11 theo kế hoạch mua sắm."
        });

        var created = await service.TopUpOpenHorizonAsync("ACME");
        var open = await service.GetOpenPeriodsAsync("ACME");

        Assert.Equal(
            [(2026, 8)],
            created.Select(x => (x.Year, x.Month)).ToArray());
        Assert.Equal(
            [(2026, 8), (2026, 11)],
            open.Select(x => (x.Year, x.Month)).ToArray());
    }

    [Fact]
    public async Task Manual_period_rejects_duplicate_company_year_and_month()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        var request = new VppOrderPeriodManualCreateReqDTO
        {
            Year = 2026,
            Month = 11,
            OpenAtLocal = new DateTime(2026, 8, 26, 8, 0, 0),
            CloseAtLocal = new DateTime(2026, 12, 5),
            SupplementApprovalDeadlineLocal = new DateTime(2026, 12, 7),
            Reason = "Mở riêng kỳ tháng 11."
        };
        _ = await service.CreateManualAsync("ACME", 5615, request);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateManualAsync("ACME", 5615, request));
    }

    [Fact]
    public async Task Manual_period_rejects_a_past_business_month_even_with_future_deadlines()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        // Provider dùng giờ nghiệp vụ Việt Nam; 01:00 ngày 01/08 tương ứng 18:00 UTC ngày 31/07.
        var service = CreateService(context, new DateTime(2026, 8, 1, 1, 0, 0));
        var request = new VppOrderPeriodManualCreateReqDTO
        {
            Year = 2026,
            Month = 7,
            OpenAtLocal = new DateTime(2026, 8, 1, 8, 0, 0),
            CloseAtLocal = new DateTime(2026, 9, 5),
            SupplementApprovalDeadlineLocal = new DateTime(2026, 9, 10),
            Reason = "Không được tạo bù kỳ đã qua."
        };

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateManualAsync("ACME", 5615, request));

        Assert.Equal("Không thể tạo kỳ đặt hàng trong quá khứ.", exception.Message);
        Assert.Empty(context.Periods);
    }

    [Fact]
    public async Task Manual_period_can_recreate_a_soft_deleted_legacy_period()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        var request = new VppOrderPeriodManualCreateReqDTO
        {
            Year = 2026,
            Month = 9,
            OpenAtLocal = new DateTime(2026, 8, 26, 8, 0, 0),
            CloseAtLocal = new DateTime(2026, 10, 5),
            SupplementApprovalDeadlineLocal = new DateTime(2026, 10, 10),
            Reason = "Thêm lại kỳ tháng 09 theo kế hoạch."
        };
        var legacy = await service.CreateManualAsync("ACME", 5615, request);

        // Kỳ rolling cũ chỉ được lưu để truy vết; nó không chặn kỳ thủ công mới cùng tháng.
        var legacyEntity = context.Periods.Single(x => x.Id == legacy.Id);
        legacyEntity.IsDeleted = true;
        legacyEntity.LastTransitionReason = "legacy-rolling-horizon-retired";
        await context.SaveChangesAsync();

        var recreated = await service.CreateManualAsync("ACME", 5615, request);

        Assert.NotEqual(legacy.Id, recreated.Id);
        Assert.Equal(2, context.Periods.Count(x => x.Year == 2026 && x.Month == 9));
        Assert.Single(context.Periods.Where(x => x.Year == 2026 && x.Month == 9 && !x.IsDeleted));
    }

    [Fact]
    public async Task Locked_period_can_reopen_with_new_deadlines_and_a_reason()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        var period = (await service.TopUpOpenHorizonAsync("ACME")).First();
        context.Periods.Single(x => x.Id == period.Id).RowVersion = [1];
        await context.SaveChangesAsync();
        period = (await service.ListManagedAsync("ACME")).Single(x => x.Id == period.Id);

        var locked = await service.CloseSubmissionsAsync(
            period.Id,
            5615,
            new VppOrderPeriodCommandReqDTO
            {
                Reason = "Tạm khóa để rà soát dữ liệu.",
                RowVersion = period.RowVersion
            });
        var reopened = await service.ReopenSubmissionsAsync(
            period.Id,
            5615,
            new VppOrderPeriodReopenSubmissionsReqDTO
            {
                CloseAtLocal = new DateTime(2026, 8, 28, 9, 0, 0),
                SupplementApprovalDeadlineLocal = new DateTime(2026, 9, 2, 9, 0, 0),
                Reason = "Đã rà soát xong, mở lại để nhân viên hoàn tất đơn.",
                RowVersion = locked.RowVersion
            });

        Assert.Equal(nameof(VppPeriodState.Open), reopened.State);
        Assert.Equal(new DateTime(2026, 8, 28, 9, 0, 0), reopened.CloseAtLocal);
        Assert.False(reopened.CanReopenSubmissions);
        Assert.True(reopened.CanCloseSubmissions);
        Assert.Contains("mở lại", reopened.LastTransitionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Open_period_with_orders_only_allows_deadline_extension_or_close_command()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(
            $"period-{Guid.NewGuid():N}");
        var service = CreateService(context, new DateTime(2026, 8, 26, 9, 0, 0));
        var period = (await service.TopUpOpenHorizonAsync("ACME")).First();
        context.Periods.Single(x => x.Id == period.Id).RowVersion = [1];
        await context.SaveChangesAsync();
        period = (await service.ListManagedAsync("ACME")).Single(x => x.Id == period.Id);
        context.Requests.Add(new VppRequest
        {
            Id = Guid.NewGuid(),
            PeriodId = period.Id,
            Year = period.Year,
            Month = period.Month,
            MemberCompanyCode = "ACME",
            RequestSeriesId = Guid.NewGuid(),
            IsCurrentRevision = true
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateScheduleAsync(
            period.Id,
            5615,
            new VppOrderPeriodUpdateReqDTO
            {
                OpenAtLocal = period.OpenAtLocal,
                CloseAtLocal = period.CloseAtLocal.AddDays(1),
                SupplementApprovalDeadlineLocal = period.SupplementApprovalDeadlineLocal.AddDays(1),
                Reason = "Thử sửa trực tiếp kỳ đã có đơn.",
                RowVersion = period.RowVersion
            }));

        var extended = await service.ExtendDeadlineAsync(
            period.Id,
            5615,
            new VppOrderPeriodExtendDeadlineReqDTO
            {
                CloseAtLocal = period.CloseAtLocal.AddDays(1),
                SupplementApprovalDeadlineLocal = period.SupplementApprovalDeadlineLocal.AddDays(1),
                Reason = "Gia hạn để phòng ban hoàn tất nhu cầu.",
                RowVersion = period.RowVersion
            });

        Assert.Equal(period.CloseAtLocal.AddDays(1), extended.CloseAtLocal);
    }

    private static Task SaveCurrentSettingsAsync(
        VppPeriodService service,
        int horizonCount,
        int year,
        int month)
        => service.SaveSettingsAsync("ACME", 5615, new VppOrderPeriodSettingsReqDTO
        {
            Name = $"Mặc định {horizonCount} kỳ",
            DefaultOpenPeriodCount = horizonCount,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            SupplementApprovalGraceDays = 2,
            EffectiveFromYear = year,
            EffectiveFromMonth = month
        });

    private static VppPeriodService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        DateTime now)
        => CreateService(context, new FakeDateTimeProvider(now));

    private static VppPeriodService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        FakeDateTimeProvider clock)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context).Object;
        return new VppPeriodService(
            unitOfWork,
            clock,
            new PeriodScheduleCalculator(),
            NullLogger<VppPeriodService>.Instance);
    }
}
