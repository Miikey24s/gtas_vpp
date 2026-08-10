using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using Xunit;

namespace gtas_vpp_be.Tests.Domain;

public sealed class OrderPeriodScheduleCalculatorTests
{
    private readonly PeriodScheduleCalculator _calculator = new();

    [Theory]
    [InlineData(2027, 2, 31, 28)]
    [InlineData(2028, 2, 31, 29)]
    [InlineData(2027, 4, 31, 30)]
    [InlineData(2027, 5, 31, 31)]
    public void Boundary_clamps_day_to_real_month_end(
        int year,
        int month,
        int requestedDay,
        int expectedDay)
    {
        var result = _calculator.BoundaryLocal(
            year,
            month,
            requestedDay,
            TimeSpan.Zero);

        Assert.Equal(new DateTime(year, month, expectedDay), result);
    }

    [Fact]
    public void Future_target_period_keeps_longer_independent_deadline()
    {
        var settings = Settings();

        var schedule = _calculator.Build(
            new Period(2026, 10),
            new Period(2026, 8),
            settings);

        Assert.Equal(new DateTime(2026, 8, 5), schedule.StartAtLocal);
        Assert.Equal(new DateTime(2026, 11, 5), schedule.SubmissionDeadlineLocal);
        Assert.Equal(new DateTime(2026, 11, 7), schedule.SupplementApprovalDeadlineLocal);
    }

    [Fact]
    public void Exact_schedule_rejects_close_before_open()
    {
        Assert.Throws<ArgumentException>(() => _calculator.BuildExact(
            new DateTime(2026, 8, 10),
            new DateTime(2026, 8, 5),
            new DateTime(2026, 8, 7),
            "Asia/Ho_Chi_Minh"));
    }

    private static VppOrderPeriodSettingsVersion Settings() => new()
    {
        MemberCompanyCode = "ACME",
        VersionNumber = 1,
        Name = "Default",
        DefaultOpenPeriodCount = 3,
        DefaultNewPeriodOpenDay = 5,
        DefaultPeriodCloseDay = 5,
        LocalTimeOfDay = TimeSpan.Zero,
        TimeZoneId = "Asia/Ho_Chi_Minh",
        SupplementApprovalGraceDays = 2,
        EffectiveFromYear = 2026,
        EffectiveFromMonth = 8
    };
}
