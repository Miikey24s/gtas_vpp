using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class CatalogPagingUiTests
{
    [Fact]
    public void OrderWizard_UsesFullClientSnapshotWithBoundedPaging()
    {
        var source = ReadSource("Components", "Pages", "VPPRequest", "OrderCreateStep2.razor");

        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", source, StringComparison.Ordinal);
        Assert.Contains("SnapshotBatchSize = 100", source, StringComparison.Ordinal);
        Assert.Contains("LoadProductSnapshotAsync", source, StringComparison.Ordinal);
        Assert.Contains("<RadzenPager", source, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.LargeWorkingSet.DefaultPageSize", source, StringComparison.Ordinal);
        Assert.Contains("VisibleProductOptions", source, StringComparison.Ordinal);
        Assert.Contains("vpp-order-builder-virtual-header", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenDataGrid", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<Virtualize", source, StringComparison.Ordinal);
        Assert.Contains("GetFromApiWithTotalCountAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadData=", source, StringComparison.Ordinal);
        Assert.DoesNotContain("products/lookup", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LibraryItemGrid_UsesTypedCatalogEndpointWithGuardedHardDelete()
    {
        var source = ReadSource("Components", "Pages", "Lib", "Component_Library.razor");
        var grid = ReadSource("Components", "Pages", "Lib", "Tabs", "Tab_ItemLibrary.razor.cs");
        var editor = ReadSource("Components", "Pages", "Lib", "Tabs", "Dialog", "Dialog_ItemEditor.razor");

        Assert.Contains("<Tab_ItemLibrary", source, StringComparison.Ordinal);
        Assert.Contains("Config.ApiCatalogItems", grid, StringComparison.Ordinal);
        Assert.Contains("/status", grid, StringComparison.Ordinal);
        Assert.Contains("VppItemCreateRequest", editor, StringComparison.Ordinal);
        Assert.Contains("VppItemUpdateRequest", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowHardDelete", source, StringComparison.Ordinal);
        Assert.Contains("DeleteFromApiAsync", grid, StringComparison.Ordinal);
        Assert.Contains("PermanentDeleteWarning", grid, StringComparison.Ordinal);
    }

    private static string ReadSource(params string[] relativeSegments)
    {
        var root = FindRepositoryRoot();
        var segments = new[] { root, "src", "Frontend", "Blazor" }
            .Concat(relativeSegments)
            .ToArray();
        return File.ReadAllText(Path.Combine(segments));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
