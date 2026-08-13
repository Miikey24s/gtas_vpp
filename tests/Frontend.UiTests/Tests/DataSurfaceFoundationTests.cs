using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class DataSurfaceFoundationTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Ds4LibraryApi_ReturnsSeededRowsForEveryAdminCollection()
    {
        BackendBaseUrl.Should().NotBeNullOrWhiteSpace();
        using var client = new HttpClient { BaseAddress = new Uri(BackendBaseUrl!) };
        var cancellationToken = TestContext.Current.CancellationToken;
        using var loginResponse = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new AuthenticationLoginRequest(TestAccounts.SystemAdmin.Username, TestAccounts.SystemAdmin.Password),
            cancellationToken);
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResultDTO>(cancellationToken);
        login?.AccessToken.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        foreach (var endpoint in new[]
                 {
                     "/api/Library/vpp-categories?showDeleted=true&skip=0&top=50",
                     "/api/catalog/items?showDeleted=true&skip=0&top=50",
                     "/api/Library/suppliers?showDeleted=true&skip=0&top=50",
                     "/api/Library/departments?showDeleted=true&skip=0&top=50",
                     "/api/vpppricelist?showDeleted=true&skip=0&top=50"
                 })
        {
            using var response = await client.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            response.Headers.TryGetValues("X-Total-Count", out var values).Should().BeTrue(endpoint);
            int.Parse(values!.Single()).Should().BeGreaterThan(0, endpoint);
        }
    }

    [Fact]
    public async Task Ds4LibraryRoutes_RenderSeededRowsAfterInteractiveTabSelection()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        foreach (var route in new[]
                 {
                     (Path: "library?tab=0", TestId: "lookup-categories-data-surface", Label: "lookup"),
                     (Path: "library?tab=0", TestId: "lookup-values-data-surface", Label: "lookup-values"),
                     (Path: "library?tab=1", TestId: "category-admin-data-surface", Label: "categories"),
                     (Path: "library?tab=2", TestId: "item-admin-data-surface", Label: "items"),
                     (Path: "library?tab=3", TestId: "supplier-admin-data-surface", Label: "suppliers"),
                     (Path: "library?tab=5", TestId: "department-admin-data-surface", Label: "departments"),
                     (Path: "library?tab=6&pricingTab=price-lists", TestId: "price-lists-data-surface", Label: "price-lists"),
                     (Path: "library?tab=6&pricingTab=prices", TestId: "prices-data-surface", Label: "prices")
                 })
        {
            TestContext.Current.SendDiagnosticMessage($"Checking seeded rows for {route.Path}");
            await Page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.Load });
            var surface = Page.GetByTestId(route.TestId);
            await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await Page.WaitForFunctionAsync(
                """
                testId => {
                    const surface = document.querySelector(`[data-testid="${testId}"]`);
                    return !!surface && [...surface.querySelectorAll('tbody > tr')].some(row =>
                        !row.classList.contains('rz-datatable-emptymessage')
                        && row.querySelectorAll('td').length > 1);
                }
                """,
                route.TestId,
                new() { Timeout = 60_000 });
            (await surface.Locator("tbody > tr td").CountAsync()).Should().BeGreaterThan(1, route.Path);
            await CaptureAsync($"ds4-live-{route.Label}-1366x768.png");
        }
    }

    [Fact]
    public async Task ServerAndClientPagedConsumers_ExposeTypedFoundationWithoutScrollRequests()
    {
        await LoginAsAsync(TestAccounts.Employee);

        foreach (var viewport in new[]
                 {
                     (Width: 1920, Height: 1080),
                     (Width: 1366, Height: 768),
                     (Width: 768, Height: 1024),
                     (Width: 390, Height: 844)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");
            await WaitForGridRowsAsync(".vpp-history-grid");

            var pagedSurface = Page.GetByTestId("history-orders-data-surface");
            await pagedSurface.WaitForAsync();
            (await pagedSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            (await pagedSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
            (await pagedSurface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1,
                "DS2 moves the canonical History toolbar into the shared data frame");
            (await pagedSurface.Locator(".vpp-filter-search").CountAsync()).Should().Be(1);
            (await pagedSurface.Locator(".vpp-filter-select").CountAsync()).Should().Be(2);
            (await pagedSurface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds2-history-paged-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width >= 1000)
            {
                await AssertDesktopRhythmAsync();
                var filterTrigger = pagedSurface.Locator(".vpp-filter-select-trigger").First;
                await filterTrigger.ClickAsync();
                var filterPanel = pagedSurface.Locator(".vpp-filter-select-popover:popover-open").First;
                await filterPanel.WaitForAsync();
                await CaptureAsync($"ds2-history-filter-popup-{viewport.Width}x{viewport.Height}.png");
                await Page.Keyboard.PressAsync("Escape");

                var codeTrigger = pagedSurface.Locator(".vpp-cell-value-popover-code .vpp-cell-value-popover-trigger").First;
                await codeTrigger.ClickAsync();
                var panel = pagedSurface.Locator(".vpp-cell-value-popover-panel").First;
                await panel.WaitForAsync();
                (await panel.GetAttributeAsync("class")).Should().Contain("vpp-transient-surface");
                await CaptureAsync($"ds2-history-cell-popover-{viewport.Width}x{viewport.Height}.png");
                await Page.Keyboard.PressAsync("Escape");
            }

            if (viewport.Width < 1000)
            {
                continue;
            }

            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
            var clientSurface = Page.GetByTestId("current-order-panel-data-surface");
            await clientSurface.WaitForAsync(new() { Timeout = 60_000 });
            await WaitForGridRowsAsync(".vpp-order-items-grid");
            (await clientSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().BeOneOf("static", "client-snapshot-paged");
            (await clientSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("rich-two-line");

            var renderedRows = await clientSurface.Locator("tbody tr").CountAsync();
            renderedRows.Should().BeLessThanOrEqualTo(100, "client paging must keep the mounted DOM bounded");

            var apiRequests = new ConcurrentQueue<string>();
            void ObserveRequest(object? _, IRequest request)
            {
                if (request.Url.Contains("/api/VPPRequest/", StringComparison.OrdinalIgnoreCase))
                {
                    apiRequests.Enqueue(request.Url);
                }
            }

            Page.Request += ObserveRequest;
            await clientSurface.Locator(".rz-data-grid-data").EvaluateAsync("element => element.scrollTop = element.scrollHeight");
            await WaitForRenderSettleAsync();
            Page.Request -= ObserveRequest;

            apiRequests.Should().BeEmpty("client snapshot paging must not fetch another window while scrolling");
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds1-order-items-bounded-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width == 1366)
            {
                await SetDarkModeAsync(true);
                await CaptureAsync("ds1-order-items-bounded-dark-1366x768.png");
                await SetDarkModeAsync(false);
            }
        }
    }

    [Fact]
    public async Task Ds4AdminRoutes_ExposeOneCanonicalToolbarAndServerPagedSurface()
    {
        await LoginAsDefaultUserAsync();
        var routes = new[]
        {
            (Path: "library?tab=0", MinimumSurfaces: 2, CreateActions: 2, Label: "lookup"),
            (Path: "library?tab=1", MinimumSurfaces: 1, CreateActions: 1, Label: "categories"),
            (Path: "library?tab=2", MinimumSurfaces: 1, CreateActions: 1, Label: "items"),
            (Path: "library?tab=3", MinimumSurfaces: 1, CreateActions: 1, Label: "suppliers"),
            (Path: "library?tab=5", MinimumSurfaces: 1, CreateActions: 1, Label: "departments"),
            (Path: "library?tab=6&pricingTab=price-lists", MinimumSurfaces: 1, CreateActions: 1, Label: "price-lists"),
            (Path: "library?tab=6&pricingTab=prices", MinimumSurfaces: 1, CreateActions: 0, Label: "prices"),
            (Path: "permission?tab=0", MinimumSurfaces: 1, CreateActions: 1, Label: "users"),
            (Path: "permission?tab=1", MinimumSurfaces: 1, CreateActions: 0, Label: "permissions")
        };

        foreach (var viewport in new[]
                 {
                     (Width: 1920, Height: 1080),
                     (Width: 768, Height: 1024)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            foreach (var route in routes)
            {
                await Page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.Load });
                await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new()
                {
                    State = WaitForSelectorState.Attached
                });
                var surfaces = Page.Locator("[data-vpp-data-surface='true']");
                await Page.WaitForFunctionAsync(
                    $"() => document.querySelectorAll('[data-vpp-data-surface=\"true\"]').length >= {route.MinimumSurfaces}",
                    null,
                    new() { Timeout = 60_000 });
                await WaitForDataGridsToSettleAsync();

                (await surfaces.CountAsync()).Should().BeGreaterThanOrEqualTo(route.MinimumSurfaces);
                for (var index = 0; index < route.MinimumSurfaces; index++)
                {
                    var surface = surfaces.Nth(index);
                    (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
                    (await surface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
                    (await surface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1);
                    (await surface.Locator(".vpp-data-toolbar .vpp-collection-header-add").CountAsync()).Should().Be(0,
                        "admin toolbars contain query/display controls only");
                    (await surface.Locator(".vpp-data-grid").CountAsync()).Should().BeGreaterThanOrEqualTo(1);
                }

                var visibleCreateActions = await Page.Locator(".vpp-collection-header .vpp-collection-header-add:visible").CountAsync();
                visibleCreateActions.Should().Be(route.CreateActions,
                    $"{route.Label}: authorized collection create actions must keep a stable visible footprint");
                (await Page.Locator("th.rz-col-actions .vpp-collection-header-add").CountAsync())
                    .Should().Be(0, "row-action headers remain plain table headers");

                if (viewport.Width == 768 && route.Label is "price-lists" or "users")
                {
                    var responsiveWorkspace = Page.Locator("[data-vpp-workspace-pattern='collection']").First;
                    await responsiveWorkspace.WaitForAsync();
                    var bounds = await responsiveWorkspace.BoundingBoxAsync();
                    bounds.Should().NotBeNull();
                    bounds!.Width.Should().BeLessThanOrEqualTo(viewport.Width,
                        "collection workspaces stay inside the tablet viewport instead of inheriting a stale list/detail layout");
                }

                await AssertNoDocumentOverflowAsync(viewport.Width);
                await CaptureAsync($"ds4-{route.Label}-{viewport.Width}x{viewport.Height}.png");
            }
        }

        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}report", new() { WaitUntil = WaitUntilState.Load });
        await Page.Locator(".vpp-report-page:not([aria-busy='true'])").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.Locator(".vpp-report-header").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.vpp-report-page .vpp-skeleton-page').length === 0",
            null,
            new() { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.vpp-report-card .rz-chart path')].some(path => {
                const fill = getComputedStyle(path).fill;
                const box = path.getBBox();
                return box.width > 0 && box.height > 0
                    && fill !== 'none'
                    && fill !== 'rgba(0, 0, 0, 0)';
            })
            """,
            null,
            new() { Timeout = 60_000 });
        await WaitForRenderSettleAsync();
        var reportGrids = Page.Locator(".vpp-report-page .rz-data-grid");
        var canonicalReportGrids = Page.Locator(".vpp-report-page .vpp-data-grid.vpp-data-density-compact");
        (await canonicalReportGrids.CountAsync()).Should().Be(await reportGrids.CountAsync(),
            "every rendered static report grid uses the compact contract without pretending to be a paged surface");
        await AssertNoDocumentOverflowAsync(1366);
        await CaptureAsync("ds4-report-static-1366x768.png");
    }

    [Fact]
    public async Task PagedFooters_KeepSummaryLeftAndNavigationRightAcrossWorkspacePatterns()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        foreach (var route in new[]
                 {
                     (Path: "library?tab=0", Label: "lookup-split"),
                     (Path: "library?tab=1", Label: "category"),
                     (Path: "permission?tab=0", Label: "users")
                 })
        {
            await Page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.Load });
            await WaitForDataGridsToSettleAsync();

            var pagers = Page.Locator("[data-vpp-data-surface='true'] .rz-pager:visible, [data-vpp-data-surface='true'] .rz-paginator:visible");
            await pagers.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            (await pagers.CountAsync()).Should().BeGreaterThan(0, route.Path);

            for (var index = 0; index < await pagers.CountAsync(); index++)
            {
                var pager = pagers.Nth(index);
                (await pager.GetAttributeAsync("class")).Should().Contain("rz-align-right",
                    "mọi data footer có paging phải dùng cùng một contract căn lề");

                var geometry = await pager.EvaluateAsync<double[]>("""
                    element => {
                        const summary = element.querySelector('.rz-pager-summary');
                        const pages = element.querySelector('.rz-pager-pages');
                        const pageSize = element.querySelector('.rz-dropdown');
                        if (!summary || !pages || !pageSize) return [];
                        const summaryRect = summary.getBoundingClientRect();
                        const pagesRect = pages.getBoundingClientRect();
                        const pageSizeRect = pageSize.getBoundingClientRect();
                        return [summaryRect.left, summaryRect.right, pagesRect.left, pagesRect.right, pageSizeRect.left];
                    }
                    """);

                if (geometry.Length > 0)
                {
                    geometry[0].Should().BeLessThan(geometry[2], "summary thuộc vùng trái của footer");
                    geometry[3].Should().BeLessThanOrEqualTo(geometry[4] + 1, "page-size đứng sau cụm điều hướng ở vùng phải");
                }
            }

            var pageSizeSelect = pagers.First.Locator(".rz-dropdown");
            await pageSizeSelect.ClickAsync();
            var pageSizePopup = Page.Locator(".rz-dropdown-panel:visible").Last;
            await pageSizePopup.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Page.Mouse.ClickAsync(300, 70);
            await pageSizePopup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

            await CaptureAsync($"data-footer-canonical-{route.Label}-1366x768.png");
        }
    }

    [Fact]
    public async Task CanonicalDataSurfaces_HaveOneOuterChromeAndOneFooterSeam()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        foreach (var route in new[]
                 {
                     (Path: "library?tab=1", TestId: "category-admin-data-surface"),
                     (Path: "library?tab=6&pricingTab=price-lists", TestId: "price-lists-data-surface"),
                     (Path: "dashboard?tab=5&periodTab=pending", TestId: "pending-approval-list"),
                     (Path: "dashboard?tab=5&periodTab=periods", TestId: "order-period-management-table")
                 })
        {
            await Page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.Load });
            var surface = Page.GetByTestId(route.TestId);
            await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await WaitForDataGridsToSettleAsync();

            var seam = await surface.EvaluateAsync<string>("""
                element => {
                    const grid = element.querySelector('.vpp-data-grid');
                    const scroller = element.querySelector('.vpp-data-grid .rz-data-grid-data');
                    const pager = element.querySelector('.vpp-data-grid :is(.rz-paginator, .rz-pager)');
                    const lastCells = [...element.querySelectorAll('.vpp-data-grid tbody > tr:last-child > td')];
                    if (!grid || !scroller || !pager) return 'missing';

                    const gridStyle = getComputedStyle(grid);
                    const scrollerStyle = getComputedStyle(scroller);
                    const pagerStyle = getComputedStyle(pager);
                    const flatGrid = Number.parseFloat(gridStyle.borderTopWidth) === 0
                        && Number.parseFloat(gridStyle.borderTopLeftRadius) === 0
                        && gridStyle.boxShadow === 'none';
                    const flatScroller = Number.parseFloat(scrollerStyle.borderTopLeftRadius) === 0
                        && scrollerStyle.boxShadow === 'none';
                    const oneFooterSeam = Number.parseFloat(pagerStyle.borderTopWidth) === 1;
                    const lastRowKeepsItsSeparator = lastCells.length === 0 || lastCells.every(cell => {
                        const style = getComputedStyle(cell);
                        return Number.parseFloat(style.borderBottomWidth) === 1
                            && style.borderBottomColor !== 'rgba(0, 0, 0, 0)'
                            && style.borderBottomColor !== 'transparent';
                    });
                    const scrollerRect = scroller.getBoundingClientRect();
                    const pagerRect = pager.getBoundingClientRect();
                    const footerDoesNotOverlapBody = Math.abs(pagerRect.top - scrollerRect.bottom) <= 1;
                    return `${flatGrid && flatScroller && oneFooterSeam && lastRowKeepsItsSeparator && footerDoesNotOverlapBody}`
                        + `|grid=${gridStyle.borderTopWidth}/${gridStyle.borderTopLeftRadius}/${gridStyle.boxShadow}`
                        + `|scroller=${scrollerStyle.borderTopLeftRadius}/${scrollerStyle.boxShadow}`
                        + `|pager=${pagerStyle.borderTopWidth}`
                        + `|gap=${pagerRect.top - scrollerRect.bottom}`;
                }
                """);

            seam.Should().StartWith("true", $"{route.Path}: frame owns outer chrome, data rows keep separators and pager does not overlap the body");
        }
    }

    [Fact]
    public async Task PendingApprovalEmptyQueue_UsesTheFullWorkspaceWidth()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=pending", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.GetByTestId("pending-approval-list");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await WaitForDataGridsToSettleAsync();
        if (await surface.GetAttributeAsync("data-vpp-surface-state") is not ("empty" or "filtered-empty"))
        {
            return;
        }

        var geometry = await Page.Locator(".vpp-approval-workspace").EvaluateAsync<double[]>("""
            workspace => {
                const list = workspace.querySelector('.vpp-list-detail-workspace-list');
                const detail = workspace.querySelector('.vpp-list-detail-workspace-detail');
                const pager = workspace.querySelector('.rz-paginator, .rz-pager');
                const emptyState = workspace.querySelector('.vpp-data-grid-empty-state');
                const workspaceRect = workspace.getBoundingClientRect();
                const listRect = list?.getBoundingClientRect();
                const pagerRect = pager?.getBoundingClientRect();
                const emptyStyle = emptyState ? getComputedStyle(emptyState) : null;
                return [
                    detail ? 1 : 0,
                    listRect?.width ?? 0,
                    workspaceRect.width,
                    Math.abs((listRect?.left ?? 0) - workspaceRect.left),
                    Math.abs((listRect?.right ?? 0) - workspaceRect.right),
                    Math.abs((pagerRect?.bottom ?? 0) - workspaceRect.bottom),
                    Number.parseFloat(emptyStyle?.borderTopWidth ?? '-1'),
                    Number.parseFloat(emptyStyle?.borderTopLeftRadius ?? '-1'),
                    emptyStyle?.boxShadow === 'none' ? 1 : 0
                ];
            }
            """);

        geometry[0].Should().Be(0, "empty queue has no meaningless detail placeholder");
        geometry[1].Should().BeApproximately(geometry[2] - 2, 1,
            "the preserved list shell fills the workspace inside its one-pixel outer border");
        geometry[3].Should().BeLessThanOrEqualTo(1);
        geometry[4].Should().BeLessThanOrEqualTo(1);
        geometry[5].Should().BeLessThanOrEqualTo(1, "the empty pager remains anchored to the workspace bottom");
        geometry[6].Should().Be(0, "grid empty content must not draw a second border");
        geometry[7].Should().Be(0, "grid empty content must not draw nested rounded corners");
        geometry[8].Should().Be(1, "grid empty content must not draw a nested shadow");
        await CaptureAsync("pending-approval-empty-full-width-1366x768.png");
    }

    private async Task AssertDesktopRhythmAsync()
    {
        var rhythm = await Page.EvaluateAsync<double[]>("""
            () => {
                const toolbar = document.querySelector('.vpp-history-orders-toolbar');
                const header = document.querySelector('.vpp-history-grid thead th');
                const row = document.querySelector('.vpp-history-grid tbody tr');
                const footer = document.querySelector('.vpp-history-grid .rz-paginator, .vpp-history-grid .rz-pager');
                return [toolbar, header, row, footer].map(element => element?.getBoundingClientRect().height ?? 0);
            }
            """);

        rhythm[0].Should().BeInRange(38, 42);
        rhythm[1].Should().BeInRange(38, 40);
        rhythm[2].Should().BeInRange(38, 40);
        rhythm[3].Should().BeInRange(38, 42);
    }

    private async Task AssertNoDocumentOverflowAsync(int viewportWidth)
    {
        var geometry = await Page.EvaluateAsync<double[]>("""
            () => [document.documentElement.scrollWidth, document.documentElement.clientWidth]
            """);
        geometry[0].Should().BeLessThanOrEqualTo(geometry[1] + 1,
            $"the {viewportWidth}px viewport must not gain document-level horizontal scroll");
    }

    private async Task WaitForGridRowsAsync(string selector)
    {
        await Page.WaitForFunctionAsync(
            $"() => document.querySelectorAll('{selector} tbody tr td').length > 1",
            null,
            new() { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.rz-datatable-loading')].every(element => {
                const style = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return style.display === 'none'
                    || style.visibility === 'hidden'
                    || Number.parseFloat(style.opacity || '1') === 0
                    || rect.width === 0
                    || rect.height === 0;
            })
            """,
            null,
            new() { Timeout = 60_000 });
    }

    private async Task WaitForDataGridsToSettleAsync()
    {
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.rz-datatable-loading')].every(element => {
                const style = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return style.display === 'none'
                    || style.visibility === 'hidden'
                    || Number.parseFloat(style.opacity || '1') === 0
                    || rect.width === 0
                    || rect.height === 0;
            })
            """,
            null,
            new() { Timeout = 60_000 });
        await WaitForRenderSettleAsync();
    }

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        var dropdown = Page.Locator("#user-menu-dropdown");
        if (!await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-menu-trigger").First.ClickAsync();
        }
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        if (await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-dropdown-backdrop").ClickAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await WaitForRenderSettleAsync();
    }

    private async Task CaptureAsync(string fileName)
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(evidenceDirectory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
