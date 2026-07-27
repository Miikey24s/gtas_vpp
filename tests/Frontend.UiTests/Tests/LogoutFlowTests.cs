using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class LogoutFlowTests : TestBase
{
    [Fact]
    public async Task LogoutProcess_ReturnsToLoginWithoutLeavingABlankPage()
    {
        await Page.GotoAsync($"{BaseUrl}logoutprocess", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await Page.WaitForURLAsync(
            new System.Text.RegularExpressions.Regex(
                ".*/Account/Login.*",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });

        await Page.Locator(".vpp-login-form").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        (await Page.TitleAsync()).Should().Contain("GTAS VPP");
    }
}
