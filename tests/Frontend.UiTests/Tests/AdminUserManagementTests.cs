using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AdminUserManagementTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task UserAdministration_UsesFullWidthCollectionAndBoundedResponsiveLayout()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}permission?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var surface = Page.Locator("[data-testid='permission-users-data-surface']");
        await surface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-user-grid tbody tr').length > 1");

        (await Page.Locator(".vpp-record-inspector").CountAsync()).Should().Be(0);
        var firstUserRow = Page.Locator(".permission-user-grid tbody tr").First;
        var assignmentSelects = firstUserRow.Locator(".vpp-admin-inline-select");
        (await assignmentSelects.CountAsync()).Should().Be(2,
            "group and department assignments belong in their own columns");
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse("the grid owns horizontal overflow instead of the document");

        var pageSizeSelect = surface.Locator(".rz-paginator .rz-dropdown, .rz-pager .rz-dropdown").First;
        var filterSelect = surface.Locator(".vpp-filter-select-trigger").First;
        var inlineSelect = surface.Locator(".vpp-admin-inline-select:not(.rz-state-disabled)").First;
        await inlineSelect.WaitForAsync();
        var selectChrome = await Page.EvaluateAsync<string>("""
            () => {
                const pageSize = document.querySelector('[data-testid="permission-users-data-surface"] .rz-paginator .rz-dropdown, [data-testid="permission-users-data-surface"] .rz-pager .rz-dropdown');
                const filter = document.querySelector('[data-testid="permission-users-data-surface"] .vpp-filter-select-trigger');
                const inline = document.querySelector('[data-testid="permission-users-data-surface"] .vpp-admin-inline-select:not(.rz-state-disabled)');
                const inlineLabel = inline?.querySelector('.rz-dropdown-label');
                if (!pageSize || !filter || !inline || !inlineLabel) return 'missing';
                const pageStyle = getComputedStyle(pageSize);
                const filterStyle = getComputedStyle(filter);
                const inlineStyle = getComputedStyle(inline);
                const inlineLabelStyle = getComputedStyle(inlineLabel);
                const sameGeometry = pageStyle.height === filterStyle.height
                    && pageStyle.borderRadius === filterStyle.borderRadius
                    && inlineStyle.height === filterStyle.height
                    && inlineStyle.borderRadius === filterStyle.borderRadius;
                const sameSurface = inlineStyle.backgroundColor === filterStyle.backgroundColor
                    && inlineLabelStyle.backgroundColor === 'rgba(0, 0, 0, 0)';
                return `${sameGeometry && sameSurface}`
                    + `|height=${pageStyle.height}/${filterStyle.height}/${inlineStyle.height}`
                    + `|radius=${pageStyle.borderRadius}/${filterStyle.borderRadius}/${inlineStyle.borderRadius}`
                    + `|surface=${filterStyle.backgroundColor}/${inlineStyle.backgroundColor}/${inlineLabelStyle.backgroundColor}`;
            }
        """);
        selectChrome.Should().StartWith("true", "page-size, filter and inline decision selects share one control motif");

        await inlineSelect.FocusAsync();
        var inlineFocusChrome = await inlineSelect.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                const canonical = style.outlineStyle === 'none'
                    && style.outlineWidth === '0px'
                    && style.boxShadow.includes('inset')
                    && !style.boxShadow.includes('0px 0px 0px 3px');
                return `${canonical}|${style.outlineStyle}|${style.outlineWidth}|${style.boxShadow}`;
            }
        """);
        inlineFocusChrome.Should().StartWith("true|",
            "inline selects use the canonical inset focus instead of Radzen's external blue ring");
        await pageSizeSelect.ClickAsync();
        var pageSizePopup = Page.Locator(".rz-dropdown-panel:visible").Last;
        await pageSizePopup.WaitForAsync();
        var selectedOption = pageSizePopup.Locator(".rz-state-highlight").First;
        var selectedMarker = await selectedOption.EvaluateAsync<string>(
            "option => getComputedStyle(option, '::before').content");
        selectedMarker.Should().BeOneOf("none", "normal", "\"\"",
            "selected options use a clean background without the retired vertical marker");
        await Page.Locator(".vpp-collection-header").ClickAsync();
        await pageSizePopup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var invitationButton = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Thêm người dùng",
            Exact = true
        });
        await invitationButton.WaitForAsync();
        if (!await invitationButton.IsEnabledAsync())
        {
            (await invitationButton.GetAttributeAsync("title")).Should().NotBeNullOrWhiteSpace();
        }

        await CaptureIfRequestedAsync("aa5-users-1366x768.png");

        (await Page.Locator("[data-testid='user-membership-editor']").CountAsync()).Should().Be(0,
            "the retired membership dialog must not compete with inline assignments");

        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{BaseUrl}permission?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await surface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-user-grid tbody tr').length > 1");
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('.permission-user-grid')?.classList.contains('rz-datatable-loading')");
        (await Page.Locator(".permission-user-grid tbody tr").First.Locator(".vpp-admin-inline-select").CountAsync())
            .Should().Be(2);
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse("the user administration route must remain bounded on mobile");
        await CaptureIfRequestedAsync("aa5-users-mobile-390x844.png");
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        await WaitForRenderSettleAsync();
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
