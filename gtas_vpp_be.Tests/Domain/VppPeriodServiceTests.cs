using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
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
            new DateTime(2026, 8, 8, 9, 0, 0));
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

    private static VppPeriodService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context).Object;
        return new VppPeriodService(
            unitOfWork,
            new FakeDateTimeProvider(now),
            new PeriodCalculator(),
            new VppRequestPolicy(),
            NullLogger<VppPeriodService>.Instance);
    }
}
