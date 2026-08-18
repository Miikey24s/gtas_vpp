using gtas_vpp_fe.Features.Requests.State;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderPeriodSelectionPolicyTests
{
    [Fact]
    public void Defaults_regular_to_anchor_and_supplement_to_latest_closed_period()
    {
        var july = Period(2026, 7, "Pricing");
        var august = Period(2026, 8, "Open", isAnchor: true);
        var september = Period(2026, 9, "Open");

        var result = OrderPeriodSelectionPolicy.Select(
            [september, july, august],
            requestedRegularPeriodId: null,
            requestedSupplementPeriodId: null);

        Assert.Equal(august.PeriodId, result.RegularPeriodId);
        Assert.Equal(july.PeriodId, result.SupplementPeriodId);
    }

    [Fact]
    public void Keeps_latest_settled_period_visible_for_supplement_history()
    {
        var june = Period(2026, 6, "Settled");
        var july = Period(2026, 7, "Settled");
        var august = Period(2026, 8, "Open", isAnchor: true);

        var result = OrderPeriodSelectionPolicy.Select(
            [june, august, july],
            requestedRegularPeriodId: null,
            requestedSupplementPeriodId: null);

        Assert.Equal(july.PeriodId, result.SupplementPeriodId);
    }

    [Fact]
    public void Keeps_each_requested_period_only_in_its_matching_order_type()
    {
        var june = Period(2026, 6, "SubmissionClosed");
        var july = Period(2026, 7, "SubmissionClosed");
        var august = Period(2026, 8, "Open", isAnchor: true);

        var result = OrderPeriodSelectionPolicy.Select(
            [june, july, august],
            requestedRegularPeriodId: july.PeriodId,
            requestedSupplementPeriodId: june.PeriodId);

        Assert.Equal(august.PeriodId, result.RegularPeriodId);
        Assert.Equal(june.PeriodId, result.SupplementPeriodId);
    }

    private static VppOpenPeriodOptionResDTO Period(
        int year,
        int month,
        string state,
        bool isAnchor = false) => new()
        {
            PeriodId = Guid.NewGuid(),
            Year = year,
            Month = month,
            State = state,
            IsAnchor = isAnchor
        };
}
