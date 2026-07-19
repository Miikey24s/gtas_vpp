using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AccountPasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdefg1!x")]
    [InlineData("GTAS-vpp-2026!")]
    public void IsValid_AcceptsPasswordsMatchingIdentityPolicy(string password)
        => Assert.True(AccountPasswordPolicy.IsValid(password));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Short1!")]
    [InlineData("lowercase1!")]
    [InlineData("UPPERCASE1!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSymbolHere1")]
    public void IsValid_RejectsPasswordsMissingAnyRequiredClass(string? password)
        => Assert.False(AccountPasswordPolicy.IsValid(password));
}
