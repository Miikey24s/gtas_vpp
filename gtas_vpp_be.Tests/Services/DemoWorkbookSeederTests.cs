using System.Text.Json;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Services;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class DemoWorkbookSeederTests
{
    [Theory]
    [InlineData(3, 2026, 7)]
    [InlineData(2, 2026, 6)]
    [InlineData(1, 2026, 5)]
    [InlineData(12, 2026, 4)]
    [InlineData(4, 2025, 8)]
    public void MapSourceMonthToPeriod_UsesRollingTwelveMonthWindow(
        int sourceMonth,
        int expectedYear,
        int expectedMonth)
    {
        var actual = DemoWorkbookSeeder.MapSourceMonthToPeriod(
            sourceMonth,
            latestSourceMonth: 3,
            new Period(2026, 7));

        Assert.Equal(new Period(expectedYear, expectedMonth), actual);
    }

    [Fact]
    public void NormalizedDataset_HasReviewedCountsAndNoPersonalNotes()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Helpers", "Data", "Demo");
        var auditPath = Path.Combine(directory, "demo-source-audit.json");
        using var audit = JsonDocument.Parse(File.ReadAllText(auditPath));
        var root = audit.RootElement;

        Assert.Equal(547, root.GetProperty("canonical_catalog_items").GetInt32());
        Assert.Equal(52, root.GetProperty("canonical_departments").GetInt32());
        Assert.Equal(2828, root.GetProperty("normalized_order_lines").GetInt32());
        Assert.Equal(274, root.GetProperty("personal_note_rows_removed").GetInt32());
        Assert.Equal("LVTN/data/DANG KY VPP - CAC DON VI.xlsx", root.GetProperty("source").GetString());

        var catalogLines = File.ReadLines(Path.Combine(directory, "demo-catalog.tsv")).ToArray();
        var departmentLines = File.ReadLines(Path.Combine(directory, "demo-departments.tsv")).ToArray();
        var userLines = File.ReadLines(Path.Combine(directory, "demo-users.tsv")).ToArray();
        var orderLines = File.ReadLines(Path.Combine(directory, "demo-orders.tsv")).ToArray();

        Assert.Equal(548, catalogLines.Length);
        Assert.Equal(53, departmentLines.Length);
        Assert.Equal(53, userLines.Length);
        Assert.Equal(2829, orderLines.Length);
        Assert.DoesNotContain("Note", orderLines[0], StringComparison.OrdinalIgnoreCase);
        Assert.All(userLines.Skip(1), line => Assert.Contains("@demo.gtas.local", line));
    }
}
