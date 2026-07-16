using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public interface IVppCatalogService
{
    Task<L04_VPPResDTO?> GetItemAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<L04_VPPResDTO> Items, int TotalCount)> QueryItemsAsync(
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

    Task<L04_VPPResDTO> CreateItemAsync(
        L04_VppCreateReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<L04_VPPResDTO> UpdateItemAsync(
        L04_VppUpdateReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<L04_VPPResDTO> SetItemStatusAsync(
        Guid id,
        L04_VppStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);
}
