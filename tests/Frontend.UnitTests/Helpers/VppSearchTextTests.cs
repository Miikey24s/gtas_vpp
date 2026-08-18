using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class VppSearchTextTests
{
    [Theory]
    [InlineData("Giấy Định mức", "giay dinh muc")]
    [InlineData("VPP Gia Định", "vpp gia dinh")]
    [InlineData("  BÚT BI  ", "but bi")]
    public void Normalize_RemovesVietnameseAccentsAndIgnoresCase(string source, string expected)
    {
        Assert.Equal(expected, VppSearchText.Normalize(source));
    }

    [Fact]
    public void MatchesAny_FindsAccentedValueFromPlainVietnameseQuery()
    {
        var search = VppSearchText.Normalize("gia dinh");

        Assert.True(VppSearchText.MatchesAny(search, "DEFAULT", "VPP Gia Định"));
    }
}
