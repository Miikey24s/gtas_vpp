using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace gtas_vpp_fe.UITests.Tests.Library;

public sealed class LookupManagementMutationTests : TestBase, IMutatingUiTest
{
    [Fact]
    public async Task LookupCategory_CanBeDeactivatedThenPermanentlyDeleted()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=0", new() { WaitUntil = WaitUntilState.Load });

        var code = $"QA_{Guid.NewGuid():N}"[..11].ToUpperInvariant();
        var surface = Page.Locator("[data-testid='lookup-categories-data-surface']");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await surface.Locator(".vpp-library-primary-action").ClickAsync();

        var editor = Page.Locator(".rz-dialog.vpp-admin-dialog--compact:visible");
        await editor.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var inputs = editor.Locator("input:not([type='hidden'])");
        await inputs.Nth(0).FillAsync(code);
        await inputs.Nth(1).FillAsync($"QA {code}");
        await inputs.Nth(2).FillAsync("QA");
        await editor.Locator(".vpp-adaptive-dialog-actions .rz-primary").ClickAsync();
        await editor.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var search = surface.Locator(".vpp-filter-search input");
        await search.FillAsync(code);
        var row = surface.Locator("tbody tr").Filter(new() { HasText = code });
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var actions = row.Locator(".vpp-admin-actions");
        var hardDelete = actions.Locator("button:has(.rzi:text-is('delete_forever'))");
        (await hardDelete.IsDisabledAsync()).Should().BeTrue(
            "permanent deletion is locked until the row is soft-deactivated");

        await actions.Locator(".rz-switch").ClickAsync();
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (await hardDelete.IsDisabledAsync() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        (await hardDelete.IsEnabledAsync()).Should().BeTrue();
        await hardDelete.ClickAsync();

        var confirmation = Page.Locator(".rz-dialog:visible").Last;
        await confirmation.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await confirmation.GetByRole(
                AriaRole.Button,
                new() { NameRegex = new Regex("^(Có|Yes)$", RegexOptions.IgnoreCase) })
            .ClickAsync();
        await confirmation.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        await row.WaitForAsync(new()
        {
            State = WaitForSelectorState.Detached,
            Timeout = 15_000
        });
    }
}
