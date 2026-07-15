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
                Define(nameof(L01_ClassResDTO.ClassCode), "Class Code", "150px", order: 1),
                Define(nameof(L01_ClassResDTO.ClassName), "Class Name", "250px", order: 2),
                Define(nameof(L01_ClassResDTO.ClassModul), "Class Module", "150px", order: 3),
                Define(nameof(L01_ClassResDTO.CreateUserName), ignore: true, order: 4),
                Define(nameof(L01_ClassResDTO.UpdateUserName), ignore: true, order: 5),
                Define(nameof(L01_ClassResDTO.L02_ClassDetails), ignore: true, order: 6)
            },
            GridColumnMetadataRegistry.GetAll(typeof(L01_ClassResDTO)).ToArray());
    }

    [Fact]
    public void L02Metadata_PreservesLegacyConfigurationAndOrder()
    {
        Assert.Equal(
            new[]
            {
                Define(nameof(L02_ClassDetailResDTO.Class), ignore: true, order: 1),
                Define(nameof(L02_ClassDetailResDTO.ClassId), ignore: true, order: 2),
                Define(nameof(L02_ClassDetailResDTO.ClassDetailCode), "Code", "150px", order: 3),
                Define(nameof(L02_ClassDetailResDTO.ClassDetailValue), "Value", "200px", order: 4),
                Define(nameof(L02_ClassDetailResDTO.ExtraField1), "Extra 1", "120px", order: 5),
                Define(nameof(L02_ClassDetailResDTO.ExtraField2), "Extra 2", "120px", order: 6),
                Define(nameof(L02_ClassDetailResDTO.ExtraField3), "Extra 3", "120px", order: 7),
                Define(nameof(L02_ClassDetailResDTO.Sort), "Sort", "80px", order: 8),
                Define(nameof(L02_ClassDetailResDTO.VPPs_UOM), ignore: true, order: 9),
                Define(nameof(L02_ClassDetailResDTO.CreateUserName), ignore: true, order: 10),
                Define(nameof(L02_ClassDetailResDTO.UpdateUserName), ignore: true, order: 11)
            },
            GridColumnMetadataRegistry.GetAll(typeof(L02_ClassDetailResDTO)).ToArray());
    }

    [Fact]
    public void L04Metadata_PreservesLegacyConfigurationAndOrder()
    {
        Assert.Equal(
            new[]
            {
                Define(nameof(L04_VPPResDTO.UOMId), "UOM", order: 1, isDropdownList: true),
                Define(nameof(L04_VPPResDTO.UOM), ignore: true, order: 2),
                Define(nameof(L04_VPPResDTO.VPPCategoryId), "VPP Category", order: 3, isDropdownList: true),
                Define(nameof(L04_VPPResDTO.DefaultSupplierName), "Supplier", "180px", order: 4, isReadOnly: true),
                Define(nameof(L04_VPPResDTO.DefaultPrice), "Price", "140px", order: 5, isReadOnly: true),
                Define(nameof(L04_VPPResDTO.DefaultVatRate), ignore: true, order: 6),
                Define(nameof(L04_VPPResDTO.VPPCategory), ignore: true, order: 7),
                Define(nameof(L04_VPPResDTO.L06_VPPSupplierMappings), ignore: true, order: 8)
            },
            GridColumnMetadataRegistry.GetAll(typeof(L04_VPPResDTO)).ToArray());
    }

    [Fact]
    public void Registry_ContainsExactlyTheTwentyFiveMigratedDefinitions()
    {
        var rowTypes = new[]
        {
            typeof(L01_ClassResDTO),
            typeof(L02_ClassDetailResDTO),
            typeof(L04_VPPResDTO)
        };

        var definitions = rowTypes
            .SelectMany(type => GridColumnMetadataRegistry.GetAll(type))
            .ToArray();

        Assert.Equal(25, definitions.Length);
        Assert.All(
            rowTypes,
            rowType => Assert.All(
                GridColumnMetadataRegistry.GetAll(rowType),
                definition => Assert.NotNull(rowType.GetProperty(definition.PropertyName))));
        Assert.Null(GridColumnMetadataRegistry.Get(typeof(L04_VPPResDTO), nameof(L04_VPPResDTO.VPPCode)));
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
