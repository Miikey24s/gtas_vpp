using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class AdminFilterArchitectureTests
{
    [Theory]
    [InlineData("Tab_CategoryLibrary.razor", "LibraryAllStatuses")]
    [InlineData("Tab_SupplierLibrary.razor", "LibraryAllStatuses")]
    [InlineData("Tab_DepartmentLibrary.razor", "AllParentDepartments", "LibraryAllStatuses")]
    [InlineData("Tab_ItemLibrary.razor", "AllCategories", "AllUnits", "AllSuppliers", "LibraryAllStatuses")]
    [InlineData("Tab_PriceListLibrary.razor", "AllSuppliers", "LibraryAllStatuses")]
    [InlineData("Tab_PriceLibrary.razor", "AllCategories", "AllUnits")]
    public void AdminCollectionFilters_FollowTheirVisibleColumnOrder(
        string fileName,
        params string[] orderedFilterKeys)
    {
        var source = ReadFrontendSource($"Components/Pages/Lib/Tabs/{fileName}");
        var toolbar = ReadFirstToolbar(source);

        Assert.Equal(orderedFilterKeys.Length, Regex.Matches(toolbar, "<VppFilterSelect\\b").Count);
        Assert.True(toolbar.IndexOf("<VppFilterSearch", StringComparison.Ordinal) >= 0);

        var previousIndex = toolbar.IndexOf("<VppFilterSearch", StringComparison.Ordinal);
        foreach (var key in orderedFilterKeys)
        {
            var currentIndex = toolbar.IndexOf($"Loc[\"{key}\"]", StringComparison.Ordinal);
            Assert.True(currentIndex > previousIndex, $"{key} is out of order in {fileName}.");
            previousIndex = currentIndex;
        }

        Assert.True(toolbar.IndexOf("<VppClearFiltersButton", StringComparison.Ordinal) > previousIndex);
        Assert.True(toolbar.IndexOf("<VppColumnPicker", StringComparison.Ordinal)
            > toolbar.IndexOf("<VppClearFiltersButton", StringComparison.Ordinal));
    }

    private static string ReadFirstToolbar(string source)
    {
        var start = source.IndexOf("<Toolbar>", StringComparison.Ordinal);
        var end = source.IndexOf("</Toolbar>", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return source[start..(end + "</Toolbar>".Length)];
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/Frontend/Blazor"));
        return File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
