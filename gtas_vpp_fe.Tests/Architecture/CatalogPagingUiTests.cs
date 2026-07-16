using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class CatalogPagingUiTests
{
    [Fact]
    public void OrderWizard_UsesServerPagedProductCatalog()
    {
        var source = ReadSource("Components", "Pages", "VPPRequest", "OrderCreateStep2.razor");

        Assert.Contains("LoadData=\"@LoadProductsAsync\"", source, StringComparison.Ordinal);
        Assert.Contains("GetFromApiWithTotalCountAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("products/lookup", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LibraryItemGrid_UsesTypedCatalogEndpointWithoutHardDelete()
    {
        var source = ReadSource("Components", "Pages", "Lib", "Component_Library.razor");

        Assert.Contains("DataEndpoint=\"@Config.ApiCatalogItems\"", source, StringComparison.Ordinal);
        Assert.Contains("AllowHardDelete=\"false\"", source, StringComparison.Ordinal);
        Assert.Contains("SetStatus=@ApiSetStatusAsync", source, StringComparison.Ordinal);
    }

    private static string ReadSource(params string[] relativeSegments)
    {
        var root = FindRepositoryRoot();
        var segments = new[] { root, "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe" }
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
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
