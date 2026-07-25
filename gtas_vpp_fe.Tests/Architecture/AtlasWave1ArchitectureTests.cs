using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class AtlasWave1ArchitectureTests
{
    [Theory]
    [InlineData("Components/Pages/Lib/Component_ShareGrid.razor")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor")]
    public void AdministrativeCollections_ExposeAResponsiveInspector(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("vpp-atlas-admin-workspace", source, StringComparison.Ordinal);
        Assert.Contains("Component_RecordInspector", source, StringComparison.Ordinal);
        Assert.Contains("DataGridSelectionMode.Single", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemGrid_KeepsLocalizationAndPricingEvidenceInTheInspectorByDefault()
    {
        var source = ReadFrontendSource("Components/Pages/Lib/Component_ShareGrid.razor.cs");

        Assert.Contains("IsInspectorFirstProperty", source, StringComparison.Ordinal);
        Assert.Contains("OriginalLanguageCode", source, StringComparison.Ordinal);
        Assert.Contains("DefaultSupplierName", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PeriodOperations_UsePreviewBeforeConfirmation()
    {
        var host = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor");
        var review = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor");
        var settlement = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");

        Assert.Contains("<PeriodSettlementPanel", host, StringComparison.Ordinal);
        Assert.Contains("Xếp hạng nguồn cung", settlement, StringComparison.Ordinal);
        Assert.Contains("preview.Blockers", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=\"@SettleAsync\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_ShowSettlementEvidenceOnlyWhenTheApiProvidesIt()
    {
        var report = ReadFrontendSource("Components/Pages/Report.razor");

        Assert.Contains("Summary.SettlementId.HasValue", report, StringComparison.Ordinal);
        Assert.Contains("vpp-report-settlement-evidence", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductCatalog_LoadsItsFirstPageBeforeTheGridDependsOnItsOwnCount()
    {
        var catalog = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs");

        Assert.Contains("await LoadProductsAsync(new LoadDataArgs", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("productGrid.Reload()", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Aspire_KeepsThePausedReactPreviewOptIn()
    {
        var appHost = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "MyAspire.AppHost", "AppHost.cs"));

        Assert.Contains("Frontend:EnableReactPreview", appHost, StringComparison.Ordinal);
        Assert.Contains("if (reactPreviewEnabled)", appHost, StringComparison.Ordinal);
        Assert.Contains("AddViteApp", appHost, StringComparison.Ordinal);
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe");
        return File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
