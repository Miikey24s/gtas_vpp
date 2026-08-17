using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiPerformanceArchitectureTests
{
    [Fact]
    public void ItemPriceRoute_DoesNotReloadReferenceDataAfterItsFirstInteractiveRender()
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "Pages",
            "Lib",
            "Tabs",
            "Tab_PriceLibrary.razor.cs"));

        Assert.Contains("await LoadLookupsAsync();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("interactiveLookupsRefreshed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("protected override async Task OnAfterRenderAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ColumnPicker_BuildsOneRenderSnapshotInsteadOfReenumeratingColumnsInMarkup()
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "DesignSystem",
            "Composites",
            "VppColumnPicker.razor"));

        Assert.Contains("var pickerState = BuildRenderState();", source, StringComparison.Ordinal);
        Assert.Contains("private PickerRenderState BuildRenderState()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private List<RadzenDataGridColumn<TItem>> PickableColumns =>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private List<RadzenDataGridColumn<TItem>> FilteredColumns =>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalMutationObserver_CoalescesDropdownPositioningIntoItsFrameFlush()
    {
        var source = File.ReadAllText(Path.Combine(GetFrontendRoot(), "wwwroot", "js", "vpp-interactions.js"));
        var observerStart = source.IndexOf("var interactionTreeObserver = new MutationObserver", StringComparison.Ordinal);
        var observerEnd = source.IndexOf("function startTabIndicators()", observerStart, StringComparison.Ordinal);

        Assert.True(observerStart >= 0 && observerEnd > observerStart);
        var observerSource = source[observerStart..observerEnd];
        Assert.DoesNotContain("scheduleRadzenDropdownDirection(node);", observerSource, StringComparison.Ordinal);
        Assert.Contains("scheduleInteractionTreeFlush();", observerSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/Lib/Tabs/Tab_CategoryLibrary.razor.cs", "private async Task LoadDataAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_SupplierLibrary.razor.cs", "private async Task LoadDataAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor.cs", "private async Task LoadDataAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_DepartmentLibrary.razor.cs", "private async Task LoadDataAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor.cs", "private async Task LoadDataAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "private async Task LoadPriceRowsAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor.cs", "protected async Task LoadCategories")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor.cs", "protected async Task LoadValues")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor.cs", "protected async Task LoadUsersAsync")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_SecurityAudit.razor.cs", "private async Task LoadAuditsAsync")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_PagePermission.razor.cs", "protected async Task LoadGroupsAsync")]
    [InlineData("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs", "protected async Task LoadProductsAsync")]
    public void GridLoadCallbacks_DoNotRequestAnExtraCompletionRender(string relativePath, string methodSignature)
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var methodSource = ReadMethodSource(source, methodSignature);
        var finallyIndex = methodSource.LastIndexOf("finally", StringComparison.Ordinal);

        Assert.True(finallyIndex >= 0, $"{methodSignature} should retain its loading-state cleanup.");
        Assert.DoesNotContain("StateHasChanged();", methodSource[finallyIndex..], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/VPPRequest/Component_VPPRequest.razor.cs")]
    [InlineData("Components/Pages/Permission/Component_Permission.razor.cs")]
    [InlineData("Components/Pages/Lib/Component_Library.razor.cs")]
    public void PrimaryNavigationContainers_CacheAuthorizedTabsUntilPermissionStateChanges(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains("RefreshAuthorizedTabs();", source, StringComparison.Ordinal);
        Assert.Contains("AuthorizedTabs => authorizedTabs;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("authorizedTabs.ToList().FindIndex", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Sidebar_CachesPermissionDerivedNavigationBetweenRouteOrPermissionChanges()
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "Layout",
            "LeftSidebar.razor.cs"));

        Assert.Contains("private IReadOnlyList<HeaderTab> HeaderTabs => headerTabs;", source, StringComparison.Ordinal);
        Assert.Contains("private void RefreshNavigationState()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private IReadOnlyList<HeaderTab> HeaderTabs\r\n        {", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_StartsIndependentRealtimeServicesWithoutSerializingTheirLatency()
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "Layout",
            "MainLayout.razor"));

        Assert.Contains("await Task.WhenAll(", source, StringComparison.Ordinal);
        Assert.Contains("StartPermissionRealtimeSafelyAsync()", source, StringComparison.Ordinal);
        Assert.Contains("StartNotificationInboxSafelyAsync()", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/Lib/Tabs/Tab_CategoryLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_SupplierLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_DepartmentLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "private async Task OnSearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor.cs", "private async Task OnCategorySearchInputAsync")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor.cs", "private async Task OnValueSearchInputAsync")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor.cs", "protected async Task SearchTextOnInput")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_SecurityAudit.razor.cs", "private async Task SearchTextOnInput")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_PagePermission.razor.cs", "private async Task OnGroupSearchInputAsync")]
    [InlineData("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs", "private async Task OnSearchInput")]
    public void ServerBackedSearchInputs_DebounceRapidKeystrokes(string relativePath, string methodSignature)
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var methodSource = ReadMethodSource(source, methodSignature);

        Assert.Contains("Task.Delay(", methodSource, StringComparison.Ordinal);
        Assert.Contains("OperationCanceledException", methodSource, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor.cs", "categoryOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor.cs", "uomOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_ItemLibrary.razor.cs", "supplierOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_DepartmentLibrary.razor.cs", "ParentDepartmentOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor.cs", "SupplierFilterOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "SupplierDecisionOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "PriceListDecisionOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "CategoryFilterOptions")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs", "UomFilterOptions")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor.cs", "GroupFilterOptions")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor.cs", "DepartmentFilterOptions")]
    public void ReferenceFilterOptions_AreStoredUntilTheirSourceDataChanges(string relativePath, string optionName)
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains($"{optionName} = [];", source, StringComparison.Ordinal);
        Assert.DoesNotContain($"{optionName} =>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoryWorkspace_OnlySynchronizesChartLabelsWhenChartDataChanges()
    {
        var source = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "Pages",
            "VPPRequest",
            "Tabs",
            "HistoryOrderWorkspaceTabBase.cs"));

        Assert.Contains("_renderedChartRevision != _chartRevision", source, StringComparison.Ordinal);
        Assert.Contains("_chartRevision++;", source, StringComparison.Ordinal);
        Assert.Contains("await Task.WhenAll(", source, StringComparison.Ordinal);
    }

    private static string ReadMethodSource(string source, string methodSignature)
    {
        var signatureIndex = source.IndexOf(methodSignature, StringComparison.Ordinal);
        Assert.True(signatureIndex >= 0, $"Missing method: {methodSignature}");

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        Assert.True(openBraceIndex >= 0, $"Missing method body: {methodSignature}");

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            depth += source[index] switch
            {
                '{' => 1,
                '}' => -1,
                _ => 0
            };
            if (depth == 0)
            {
                return source[signatureIndex..(index + 1)];
            }
        }

        throw new InvalidOperationException($"Unterminated method body: {methodSignature}");
    }

    private static string GetFrontendRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Frontend/Blazor"));
}
