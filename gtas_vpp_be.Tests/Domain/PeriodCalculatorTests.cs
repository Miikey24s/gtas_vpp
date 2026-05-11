using System;
using gtas_vpp_be.Service.Domain;
using Xunit;

namespace gtas_vpp_be.Tests.Domain;

public class PeriodCalculatorTests
{
    [Theory]
    // Just before deadline → still in previous month's period
    [InlineData("2026-04-04 23:59:59", 2026, 3, 2026, 2)]
    // Exactly at deadline midnight → enters current month's period
    [InlineData("2026-04-05 00:00:00", 2026, 4, 2026, 3)]
    [InlineData("2026-04-05 00:00:01", 2026, 4, 2026, 3)]
    // Mid-month → still current month's period
    [InlineData("2026-04-15 12:00:00", 2026, 4, 2026, 3)]
    // Last second of period N (April) → still period N
    [InlineData("2026-05-04 23:59:59", 2026, 4, 2026, 3)]
    // Year rollover before deadline → December of previous year
    [InlineData("2026-01-04 12:00:00", 2025, 12, 2025, 11)]
    // Year rollover at deadline → January of current year
    [InlineData("2026-01-05 00:00:00", 2026, 1, 2025, 12)]
    public void Current_And_Previous_Match_BusinessRule(
        string nowIso,
        int expectedCurrentYear, int expectedCurrentMonth,
        int expectedPreviousYear, int expectedPreviousMonth)
    {
        var calc = new PeriodCalculator(deadlineDay: 5);
        var now = DateTime.Parse(nowIso);

        var current = calc.Current(now);
        var previous = calc.Previous(now);

        Assert.Equal(new Period(expectedCurrentYear, expectedCurrentMonth), current);
        Assert.Equal(new Period(expectedPreviousYear, expectedPreviousMonth), previous);
    }

    [Theory]
    // Deadline for period (2026, 3) is 2026-04-05 00:00:00.
    [InlineData("2026-04-04 23:59:59", 2026, 3, false)]
    [InlineData("2026-04-05 00:00:00", 2026, 3, true)]
    [InlineData("2026-04-05 00:00:01", 2026, 3, true)]
    // Far past period — always passed.
    [InlineData("2026-04-15 12:00:00", 2025, 12, true)]
    // Far future period — never passed.
    [InlineData("2026-04-15 12:00:00", 2026, 12, false)]
    public void IsDeadlinePassed_FollowsBusinessRule(string nowIso, int year, int month, bool expected)
    {
        var calc = new PeriodCalculator(deadlineDay: 5);
        var now = DateTime.Parse(nowIso);

        var actual = calc.IsDeadlinePassed(now, new Period(year, month));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(29)]
    [InlineData(31)]
    public void Constructor_RejectsInvalidDeadlineDay(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PeriodCalculator(day));
    }

    [Fact]
    public void DeadlineFor_ReturnsAnchorOfFollowingMonth()
    {
        var calc = new PeriodCalculator(deadlineDay: 5);

        var deadline = calc.DeadlineFor(new Period(2026, 3));

        Assert.Equal(new DateTime(2026, 4, 5), deadline);
    }

    [Fact]
    public void Period_ToString_IsHumanReadable()
    {
        var period = new Period(2026, 3);

        Assert.Equal("2026-03", period.ToString());
    }
}
