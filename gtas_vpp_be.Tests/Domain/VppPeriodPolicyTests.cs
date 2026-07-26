using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Services;
using Xunit;

namespace gtas_vpp_be.Tests.Domain;

public sealed class VppPeriodPolicyTests
{
    [Fact]
    public void Default_policy_matches_thesis_decisions()
    {
        var policy = new VppRequestPolicy();

        Assert.Equal(5, policy.DeadlineDay);
        Assert.Equal(2, policy.SupplementApprovalGraceDays);
        Assert.Equal(3, policy.MaxApprovedSupplements);
        Assert.Equal(6, policy.MaxSupplementAttempts);
    }

    [Theory]
    [InlineData(0, 2, 3, 6)]
    [InlineData(29, 2, 3, 6)]
    [InlineData(5, -1, 3, 6)]
    [InlineData(5, 2, 0, 6)]
    [InlineData(5, 2, 3, 0)]
    public void Policy_rejects_invalid_limits(
        int deadlineDay,
        int graceDays,
        int maxApproved,
        int maxAttempts)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VppRequestPolicy(
            deadlineDay, graceDays, maxApproved, maxAttempts));
    }

    [Fact]
    public void Boundary_helpers_use_the_fifth_to_fifth_interval()
    {
        var calculator = new PeriodCalculator();
        var period = new Period(2026, 7);

        var startLocal = calculator.StartAtLocal(period);
        var submissionLocal = calculator.SubmissionDeadlineLocal(period);

        Assert.Equal(new DateTime(2026, 7, 5), startLocal);
        Assert.Equal(new DateTime(2026, 8, 5), submissionLocal);
        Assert.Equal(
            calculator.StartAtUtc(period),
            calculator.SubmissionDeadlineUtc(new Period(2026, 6)));
    }

    [Theory]
    [InlineData(2024, 2, 4, 23, 59, 2024, 1)]
    [InlineData(2024, 2, 5, 0, 0, 2024, 2)]
    [InlineData(2026, 1, 4, 23, 59, 2025, 12)]
    [InlineData(2026, 1, 5, 0, 0, 2026, 1)]
    [InlineData(2026, 12, 31, 23, 59, 2026, 12)]
    public void Current_period_handles_leap_and_year_boundaries(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int expectedYear,
        int expectedMonth)
    {
        var calculator = new PeriodCalculator(deadlineDay: 5);

        var period = calculator.Current(new DateTime(year, month, day, hour, minute, 0));

        Assert.Equal(new Period(expectedYear, expectedMonth), period);
    }

    [Fact]
    public void Supplement_deadline_is_two_days_after_submission()
    {
        var calculator = new PeriodCalculator();
        var period = new Period(2026, 7);

        var submission = calculator.SubmissionDeadlineUtc(period);
        var supplement = calculator.SupplementApprovalDeadlineUtc(
            period, TimeSpan.FromDays(2));

        Assert.Equal(TimeSpan.FromDays(2), supplement - submission);
    }

    [Theory]
    [InlineData(VppPeriodState.Open, VppPeriodState.SubmissionClosed, true)]
    [InlineData(VppPeriodState.SubmissionClosed, VppPeriodState.Pricing, true)]
    [InlineData(VppPeriodState.Pricing, VppPeriodState.Settled, true)]
    [InlineData(VppPeriodState.Open, VppPeriodState.Pricing, false)]
    [InlineData(VppPeriodState.Settled, VppPeriodState.Open, false)]
    public void State_machine_allows_only_adjacent_forward_transitions(
        VppPeriodState current,
        VppPeriodState target,
        bool expected)
    {
        Assert.Equal(expected, VppPeriodService.IsLegalTransition(current, target));
    }
}
