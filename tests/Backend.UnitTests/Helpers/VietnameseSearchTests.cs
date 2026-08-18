using gtas_vpp_be.Service.Helpers;
using Xunit;

namespace gtas_vpp_be.Tests.Helpers;

public sealed class VietnameseSearchTests
{
    [Theory]
    [InlineData("Giấy Định mức", "giay dinh muc")]
    [InlineData("VPP Gia Định", "vpp gia dinh")]
    [InlineData("Bút-lông / PM_04", "but long pm 04")]
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

    [Fact]
    public void MatchesAllTerms_RejectsRowsThatOnlyMatchAFilterField()
    {
        var terms = VietnameseSearch.Tokenize("bút lông");

        Assert.True(VietnameseSearch.MatchesAllTerms(terms, "Bút lông dầu", "VPP-PM04"));
        Assert.False(VietnameseSearch.MatchesAllTerms(terms, "Bảng 0,8m x 1,2m", "Bút viết"));
    }

    [Fact]
    public void MatchesAllTerms_AllowsAWordPairWrittenWithoutSpaces()
    {
        Assert.True(VietnameseSearch.MatchesAllTerms(VietnameseSearch.Tokenize("butlong"), "Bút lông dầu"));
    }
}
