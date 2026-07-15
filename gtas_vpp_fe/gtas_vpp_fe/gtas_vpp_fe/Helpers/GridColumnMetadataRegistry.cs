using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_fe.Helpers;

public sealed record GridColumnMetadata(
    string PropertyName,
    string? DisplayName,
    string? Width,
    bool Ignore,
    int Order,
    bool IsDropdownList,
    bool IsReadOnly);

public static class GridColumnMetadataRegistry
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<GridColumnMetadata>> Definitions =
        new Dictionary<Type, IReadOnlyList<GridColumnMetadata>>
        {
            [typeof(L01_ClassResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.ClassCode), "Class Code", "150px", order: 1),
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.ClassName), "Class Name", "250px", order: 2),
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.ClassModul), "Class Module", "150px", order: 3),
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.CreateUserName), ignore: true, order: 4),
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.UpdateUserName), ignore: true, order: 5),
                    Define<L01_ClassResDTO>(nameof(L01_ClassResDTO.L02_ClassDetails), ignore: true, order: 6)
                }),
            [typeof(L02_ClassDetailResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.Class), ignore: true, order: 1),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ClassId), ignore: true, order: 2),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ClassDetailCode), "Code", "150px", order: 3),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ClassDetailValue), "Value", "200px", order: 4),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ExtraField1), "Extra 1", "120px", order: 5),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ExtraField2), "Extra 2", "120px", order: 6),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.ExtraField3), "Extra 3", "120px", order: 7),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.Sort), "Sort", "80px", order: 8),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.VPPs_UOM), ignore: true, order: 9),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.CreateUserName), ignore: true, order: 10),
                    Define<L02_ClassDetailResDTO>(nameof(L02_ClassDetailResDTO.UpdateUserName), ignore: true, order: 11)
                }),
            [typeof(L04_VPPResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.UOMId), "UOM", order: 1, isDropdownList: true),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.UOM), ignore: true, order: 2),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.VPPCategoryId), "VPP Category", order: 3, isDropdownList: true),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.DefaultSupplierName), "Supplier", "180px", order: 4, isReadOnly: true),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.DefaultPrice), "Price", "140px", order: 5, isReadOnly: true),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.DefaultVatRate), ignore: true, order: 6),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.VPPCategory), ignore: true, order: 7),
                    Define<L04_VPPResDTO>(nameof(L04_VPPResDTO.L06_VPPSupplierMappings), ignore: true, order: 8)
                })
        };

    public static GridColumnMetadata? Get(Type rowType, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(rowType);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return Definitions.TryGetValue(rowType, out var definitions)
            ? definitions.FirstOrDefault(definition =>
                definition.PropertyName.Equals(propertyName, StringComparison.Ordinal))
            : null;
    }

    public static IReadOnlyList<GridColumnMetadata> GetAll(Type rowType)
    {
        ArgumentNullException.ThrowIfNull(rowType);

        return Definitions.TryGetValue(rowType, out var definitions)
            ? definitions
            : Array.Empty<GridColumnMetadata>();
    }

    private static GridColumnMetadata Define<T>(
        string propertyName,
        string? displayName = null,
        string? width = null,
        bool ignore = false,
        int order = int.MaxValue,
        bool isDropdownList = false,
        bool isReadOnly = false)
    {
        if (typeof(T).GetProperty(propertyName) is null)
        {
            throw new InvalidOperationException(
                $"Grid metadata references missing property {typeof(T).FullName}.{propertyName}.");
        }

        return new GridColumnMetadata(
            propertyName,
            displayName,
            width,
            ignore,
            order,
            isDropdownList,
            isReadOnly);
    }
}
