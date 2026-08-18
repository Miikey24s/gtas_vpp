using gtas_vpp_be.Service.Helpers;
using Xunit;

namespace gtas_vpp_be.Tests.Helpers;

public sealed class VietnameseSearchTests
{
    [Theory]
    [InlineData("Giấy Định mức", "giay dinh muc")]
    [InlineData("VPP Gia Định", "vpp gia dinh")]
    public void Normalize_RemovesVietnameseAccents(string source, string expected)
    {
        Assert.Equal(expected, VietnameseSearch.Normalize(source));
    }

    [Fact]
    public void BuildContainsPattern_EscapesSqlLikeMetacharacters()
    {
        Assert.Equal("%50\\% \\_ \\[test%", VietnameseSearch.BuildContainsPattern("50% _ [test"));
    }

    [Fact]
    public void Contains_FindsAccentedValueFromPlainVietnameseQuery()
    {
        Assert.True(VietnameseSearch.Contains("Nguyễn Đình Nam", VietnameseSearch.Normalize("nguyen dinh")));
    }
}
