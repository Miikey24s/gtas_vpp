using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public interface IVppCatalogService
{
    Task<VppItemResDTO?> GetItemAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<VppItemResDTO> Items, int TotalCount)> QueryItemsAsync(
        Guid? categoryId,
        string? search,
        string? filter,
        int skip,
        int top,
        string? orderby,
        string? distinct,
        string? distinctFilter,
        bool showDeleted,
        CancellationToken cancellationToken = default);

    Task<VppItemResDTO> CreateItemAsync(
        VppItemCreateRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<VppItemResDTO> UpdateItemAsync(
        VppItemUpdateRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<VppItemResDTO> SetItemStatusAsync(
        Guid id,
        VppItemStatusRequest request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<LibraryHardDeleteResult> HardDeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
