using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class VppSearchTextTests
{
    [Theory]
    [InlineData("Giấy Định mức", "giay dinh muc")]
    [InlineData("VPP Gia Định", "vpp gia dinh")]
    [InlineData("  BÚT BI  ", "but bi")]
    [InlineData("Bút-lông / PM_04", "but long pm 04")]
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

    [Fact]
    public void MatchesAny_RequiresEverySearchTermButAllowsDifferentFields()
    {
        var search = VppSearchText.Normalize("but long");

        Assert.True(VppSearchText.MatchesAny(search, "Bút lông dầu", "VPP-PM04"));
        Assert.False(VppSearchText.MatchesAny(search, "Bảng 0,8m x 1,2m", "Bút viết"));
    }

    [Fact]
    public void Tokenize_CollapsesSeparatorsAndRemovesDuplicates()
    {
        Assert.Equal(["but", "long", "pm", "04"], VppSearchText.Tokenize(" Bút--lông / bút PM_04 "));
    }

    [Fact]
    public void MatchesAny_AllowsAWordPairWrittenWithoutSpaces()
    {
        Assert.True(VppSearchText.MatchesAny(VppSearchText.Normalize("butlong"), "Bút lông dầu"));
    }
}
