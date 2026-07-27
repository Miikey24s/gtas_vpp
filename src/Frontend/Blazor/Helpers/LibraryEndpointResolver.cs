using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Ánh xạ response contract danh mục dùng chung sang API endpoint canonical.
/// Không bao giờ suy tên endpoint từ prefix tên class DTO.
/// </summary>
public static class LibraryEndpointResolver
{
    public static string Resolve<TResponse>() => Resolve(typeof(TResponse));

    public static string Resolve(Type responseType)
    {
        ArgumentNullException.ThrowIfNull(responseType);

        if (responseType == typeof(LookupCategoryResDTO)) return Config.LibraryApi.LookupCategories;
        if (responseType == typeof(LookupValueResDTO)) return Config.LibraryApi.LookupValues;
        if (responseType == typeof(VppCategoryResDTO)) return Config.LibraryApi.VppCategories;
        if (responseType == typeof(VppItemResDTO)) return Config.ApiCatalogItems;
        if (responseType == typeof(SupplierResDTO)) return Config.LibraryApi.Suppliers;
        if (responseType == typeof(SupplierProductMappingResDTO)) return Config.LibraryApi.SupplierProductMappings;
        if (responseType == typeof(DepartmentResDTO)) return Config.LibraryApi.Departments;
        if (responseType == typeof(PriceListResDTO)) return Config.LibraryApi.PriceList;

        throw new InvalidOperationException(
            $"No canonical library endpoint is registered for response type '{responseType.Name}'.");
    }
}
