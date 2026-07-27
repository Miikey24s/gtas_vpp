using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class LibraryEndpointResolverTests
{
    [Fact]
    public void Resolve_ReturnsCanonicalEndpointsForEveryLibraryContract()
    {
        Assert.Equal(Config.LibraryApi.LookupCategories, LibraryEndpointResolver.Resolve<LookupCategoryResDTO>());
        Assert.Equal(Config.LibraryApi.LookupValues, LibraryEndpointResolver.Resolve<LookupValueResDTO>());
        Assert.Equal(Config.LibraryApi.VppCategories, LibraryEndpointResolver.Resolve<VppCategoryResDTO>());
        Assert.Equal(Config.ApiCatalogItems, LibraryEndpointResolver.Resolve<VppItemResDTO>());
        Assert.Equal(Config.LibraryApi.Suppliers, LibraryEndpointResolver.Resolve<SupplierResDTO>());
        Assert.Equal(Config.LibraryApi.SupplierProductMappings, LibraryEndpointResolver.Resolve<SupplierProductMappingResDTO>());
        Assert.Equal(Config.LibraryApi.Departments, LibraryEndpointResolver.Resolve<DepartmentResDTO>());
        Assert.Equal(Config.LibraryApi.PriceList, LibraryEndpointResolver.Resolve<PriceListResDTO>());
    }

    [Fact]
    public void Resolve_RejectsUnregisteredContractsInsteadOfGuessingAPath()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => LibraryEndpointResolver.Resolve<UnregisteredResponse>());

        Assert.Contains(nameof(UnregisteredResponse), exception.Message, StringComparison.Ordinal);
    }

    private sealed class UnregisteredResponse;
}
