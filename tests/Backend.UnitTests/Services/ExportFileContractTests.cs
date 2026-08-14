using gtas_vpp_be.Service.Services;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class ExportFileContractTests
{
    [Fact]
    public void FileNames_AreStableSafeAndFormatSpecific()
    {
        var id = Guid.Parse("7db20138-e50e-4bf6-a7ad-815aaee3a2b6");

        Assert.Equal("GTAS-VPP-Don-VPP-2026-07.pdf", ExportFileContract.Order(" VPP/2026 07 ", id, ".PDF"));
        Assert.Equal("GTAS-VPP-Chot-ky-2026-07-R2.xlsx", ExportFileContract.Settlement(2026, 7, 2, "xlsx"));
        Assert.Equal("GTAS-VPP-Bao-cao-department-2026-07.csv", ExportFileContract.Report("department", 2026, 7, "csv"));
        Assert.Equal("GTAS-VPP-Bang-gia-BG-2026-08.xlsx", ExportFileContract.PriceList("BG/2026 08", "Bảng giá", "xlsx"));
    }

    [Fact]
    public void FileNames_FallBackAndRejectUnknownExtensions()
    {
        var id = Guid.Parse("7db20138-e50e-4bf6-a7ad-815aaee3a2b6");

        Assert.Equal($"GTAS-VPP-Don-{id:N}.pdf", ExportFileContract.Order(" <>:/\\ ", id, "pdf"));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExportFileContract.Report("all", null, null, "zip"));
    }
}
