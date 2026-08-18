using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Library;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class GridColumnMetadataRegistryTests
{
    [Fact]
    public void L01Metadata_PreservesLegacyConfigurationAndOrder()
    {
        Assert.Equal(
            new[]
            {
                Define(nameof(LookupCategoryResDTO.Code), "Code", "150px", order: 1),
                Define(nameof(LookupCategoryResDTO.Name), "Name", "250px", order: 2),
                Define(nameof(LookupCategoryResDTO.ModuleName), "Module", "150px", order: 3),
                Define(nameof(LookupCategoryResDTO.CreatedByUserName), ignore: true, order: 4),
                Define(nameof(LookupCategoryResDTO.UpdatedByUserName), ignore: true, order: 5),
                Define(nameof(LookupCategoryResDTO.LookupValues), ignore: true, order: 6)
            },
            GridColumnMetadataRegistry.GetAll(typeof(LookupCategoryResDTO)).ToArray());
    }

    [Fact]
    public void L02Metadata_PreservesLegacyConfigurationAndOrder()
    {
        Assert.Equal(
            new[]
            {
                Define(nameof(LookupValueResDTO.Category), ignore: true, order: 1),
                Define(nameof(LookupValueResDTO.LookupCategoryId), ignore: true, order: 2),
                Define(nameof(LookupValueResDTO.Code), "Code", "150px", order: 3),
                Define(nameof(LookupValueResDTO.Value), "Value", "200px", order: 4),
                Define(nameof(LookupValueResDTO.Sort), "Sort", "80px", order: 5),
                Define(nameof(LookupValueResDTO.VppItemsByUom), ignore: true, order: 6),
                Define(nameof(LookupValueResDTO.CreatedByUserName), ignore: true, order: 7),
                Define(nameof(LookupValueResDTO.UpdatedByUserName), ignore: true, order: 8)
            },
            GridColumnMetadataRegistry.GetAll(typeof(LookupValueResDTO)).ToArray());
    }

    [Fact]
    public void VppItemMetadata_PreservesConfigurationAndOrder()
    {
        Assert.Equal(
            new[]
            {
                Define(nameof(VppItemResDTO.UomId), "Unit", order: 1, isDropdownList: true),
                Define(nameof(VppItemResDTO.Uom), ignore: true, order: 2),
                Define(nameof(VppItemResDTO.VppCategoryId), "Category", order: 3, isDropdownList: true),
                Define(nameof(VppItemResDTO.MaxQuantityPerOrder), "MaxQuantityPerOrder", "140px", order: 4),
                Define(nameof(VppItemResDTO.DefaultSupplierName), "Supplier", "180px", order: 5, isReadOnly: true),
                Define(nameof(VppItemResDTO.DefaultPrice), "Price", "140px", order: 6, isReadOnly: true),
                Define(nameof(VppItemResDTO.DefaultVatRate), ignore: true, order: 7),
                Define(nameof(VppItemResDTO.VppCategory), ignore: true, order: 8),
                Define(nameof(VppItemResDTO.SupplierProductMappings), ignore: true, order: 9)
            },
            GridColumnMetadataRegistry.GetAll(typeof(VppItemResDTO)).ToArray());
    }

    [Fact]
    public void Registry_ContainsExactlyTheTwentyTwoMigratedDefinitions()
    {
        var rowTypes = new[]
        {
            typeof(LookupCategoryResDTO),
            typeof(LookupValueResDTO),
            typeof(VppItemResDTO)
        };

        var definitions = rowTypes
            .SelectMany(type => GridColumnMetadataRegistry.GetAll(type))
            .ToArray();

        Assert.Equal(23, definitions.Length);
        Assert.All(
            rowTypes,
            rowType => Assert.All(
                GridColumnMetadataRegistry.GetAll(rowType),
                definition => Assert.NotNull(rowType.GetProperty(definition.PropertyName))));
        Assert.Null(GridColumnMetadataRegistry.Get(typeof(VppItemResDTO), nameof(VppItemResDTO.VppCode)));
    }

    private static GridColumnMetadata Define(
        string propertyName,
        string? displayName = null,
        string? width = null,
        bool ignore = false,
        int order = int.MaxValue,
        bool isDropdownList = false,
        bool isReadOnly = false) =>
        new(propertyName, displayName, width, ignore, order, isDropdownList, isReadOnly);
}
