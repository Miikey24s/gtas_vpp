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
            [typeof(LookupCategoryResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.Code), "Code", "150px", order: 1),
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.Name), "Name", "250px", order: 2),
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.ModuleName), "Module", "150px", order: 3),
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.CreatedByUserName), ignore: true, order: 4),
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.UpdatedByUserName), ignore: true, order: 5),
                    Define<LookupCategoryResDTO>(nameof(LookupCategoryResDTO.LookupValues), ignore: true, order: 6)
                }),
            [typeof(LookupValueResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.Category), ignore: true, order: 1),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.LookupCategoryId), ignore: true, order: 2),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.Code), "Code", "150px", order: 3),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.Value), "Value", "200px", order: 4),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.Sort), "Sort", "80px", order: 5),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.VppItemsByUom), ignore: true, order: 6),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.CreatedByUserName), ignore: true, order: 7),
                    Define<LookupValueResDTO>(nameof(LookupValueResDTO.UpdatedByUserName), ignore: true, order: 8)
                }),
            [typeof(VppItemResDTO)] = Array.AsReadOnly(
                new GridColumnMetadata[]
                {
                    Define<VppItemResDTO>(nameof(VppItemResDTO.UomId), "Unit", order: 1, isDropdownList: true),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.Uom), ignore: true, order: 2),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.VppCategoryId), "Category", order: 3, isDropdownList: true),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.DefaultSupplierName), "Supplier", "180px", order: 4, isReadOnly: true),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.DefaultPrice), "Price", "140px", order: 5, isReadOnly: true),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.DefaultVatRate), ignore: true, order: 6),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.VppCategory), ignore: true, order: 7),
                    Define<VppItemResDTO>(nameof(VppItemResDTO.SupplierProductMappings), ignore: true, order: 8)
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
