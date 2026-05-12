using System;
using System.Globalization;
using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

/// <summary>
/// Tests for <see cref="DateFormatter"/> (F-29).
///
/// The helper is the single source of truth for date/time rendering in
/// the 6 order tabs + the wizard. These tests guard against:
///  - Drift between <see cref="DateFormatter.ShortDate"/>, <see cref="DateFormatter.LongDate"/>
///    and <see cref="DateFormatter.MonthYear"/> constants and what callers expect.
///  - Locale leakage if a developer accidentally swaps the pinned vi-VN culture.
///  - The "-" fallback contract for null values which several razor templates rely on.
/// </summary>
public class DateFormatterTests
{
    [Fact]
    public void Constants_Match_AuditSpec()
    {
        // F-29 audit pins these patterns; the rest of the FE depends on them.
        Assert.Equal("dd/MM/yyyy", DateFormatter.ShortDate);
        Assert.Equal("HH:mm dd/MM/yyyy", DateFormatter.LongDate);
        Assert.Equal("MM/yyyy", DateFormatter.MonthYear);
        Assert.Equal("HH:mm", DateFormatter.TimeOnly);
    }

    [Fact]
    public void Format_Nullable_Null_ReturnsDash()
    {
        var result = DateFormatter.Format((DateTime?)null, DateFormatter.ShortDate);
        Assert.Equal("-", result);
    }

    [Theory]
    [InlineData("2026-04-05", "05/04/2026")]
    [InlineData("2026-12-31", "31/12/2026")]
    [InlineData("2026-01-01", "01/01/2026")]
    public void Format_ShortDate_RendersDayMonthYear(string iso, string expected)
    {
        var input = DateTime.Parse(iso, CultureInfo.InvariantCulture);
        var result = DateFormatter.Format(input, DateFormatter.ShortDate);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-04-05T14:30:00", "14:30 05/04/2026")]
    [InlineData("2026-04-05T00:00:00", "00:00 05/04/2026")]
    [InlineData("2026-04-05T23:59:59", "23:59 05/04/2026")]
    public void Format_LongDate_RendersTimeAndDate(string iso, string expected)
    {
        var input = DateTime.Parse(iso, CultureInfo.InvariantCulture);
        var result = DateFormatter.Format(input, DateFormatter.LongDate);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-04-01", "04/2026")]
    [InlineData("2026-12-15", "12/2026")]
    public void Format_MonthYear_RendersMonthAndYear(string iso, string expected)
    {
        var input = DateTime.Parse(iso, CultureInfo.InvariantCulture);
        var result = DateFormatter.Format(input, DateFormatter.MonthYear);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2026-04-05T14:30:00", "14:30")]
    [InlineData("2026-04-05T00:05:00", "00:05")]
    public void Format_TimeOnly_RendersHourAndMinute(string iso, string expected)
    {
        var input = DateTime.Parse(iso, CultureInfo.InvariantCulture);
        var result = DateFormatter.Format(input, DateFormatter.TimeOnly);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_NonNull_OverloadMatchesNullable()
    {
        var input = new DateTime(2026, 4, 5, 14, 30, 0);
        var nullableResult = DateFormatter.Format((DateTime?)input, DateFormatter.LongDate);
        var nonNullableResult = DateFormatter.Format(input, DateFormatter.LongDate);
        Assert.Equal(nullableResult, nonNullableResult);
    }

    [Fact]
    public void Format_IsCultureStable_AcrossThreadCulture()
    {
        // Even if the calling thread switches to en-US (which would otherwise
        // render "4/5/2026" for ShortDate), DateFormatter must keep vi-VN
        // semantics so the UI stays consistent across user locales.
        var input = new DateTime(2026, 4, 5);
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var result = DateFormatter.Format(input, DateFormatter.ShortDate);
            Assert.Equal("05/04/2026", result);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
