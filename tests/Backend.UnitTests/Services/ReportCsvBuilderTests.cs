using System.Text;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Model.VPP;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ReportCsvBuilderTests
{
    [Fact]
    public void Build_ReturnsBomAndFormulaSafeRows()
    {
        var content = ReportCsvBuilder.Build(
        [
            new ReportCsvRow(
                2026,
                7,
                "IT",
                "=ORDER",
                (int)VPPStatus.Submitted,
                false,
                "P-1",
                "Paper, A4",
                2,
                100)
        ]);

        Assert.True(content.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()));
        var csv = Encoding.UTF8.GetString(content);
        Assert.StartsWith("\uFEFFsep=,", csv, StringComparison.Ordinal);
        Assert.Contains("\"'=ORDER\"", csv, StringComparison.Ordinal);
        Assert.Contains("\"Paper, A4\"", csv, StringComparison.Ordinal);
        Assert.Contains(",2,100,200", csv, StringComparison.Ordinal);
    }
}
