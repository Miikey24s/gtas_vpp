using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Service.Services;

public interface IOrderQuantityLimitService
{
    Task ValidateAsync(
        IEnumerable<VppRequestDetailItemReqDTO> items,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Kiểm tra mặt hàng còn hiệu lực và số lượng không vượt giới hạn của từng đơn.
/// Rule này dùng chung cho cả đơn thường và đơn bổ sung.
/// </summary>
public sealed class OrderQuantityLimitService : IOrderQuantityLimitService
{
    private readonly IUnitOfWork _unitOfWork;

    public OrderQuantityLimitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ValidateAsync(
        IEnumerable<VppRequestDetailItemReqDTO> items,
        CancellationToken cancellationToken = default)
    {
        var requestedItems = items.ToList();
        var requestedIds = requestedItems.Select(x => x.VppId).Distinct().ToArray();
        if (requestedIds.Length == 0)
        {
            return;
        }

        var catalogItems = await _unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .Where(x => requestedIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.VppName,
                x.MaxQuantityPerOrder,
                CategoryDeleted = x.VppCategory != null && x.VppCategory.IsDeleted
            })
            .ToListAsync(cancellationToken);

        var catalogById = catalogItems.ToDictionary(x => x.Id);
        var missingIds = requestedIds.Where(id => !catalogById.ContainsKey(id)).ToArray();
        if (missingIds.Length > 0)
        {
            throw new BusinessException(
                "Một số mặt hàng đã ngừng hoạt động. Vui lòng bỏ các mặt hàng này khỏi đơn và thử lại.");
        }

        var categoryDeletedItems = catalogItems.Where(x => x.CategoryDeleted).ToArray();
        if (categoryDeletedItems.Length > 0)
        {
            var names = string.Join(", ", categoryDeletedItems.Select(x => x.VppName));
            throw new BusinessException(
                $"Danh mục của các mặt hàng sau đã ngừng hoạt động: {names}. Vui lòng bỏ khỏi đơn và thử lại.");
        }

        foreach (var requestedItem in requestedItems)
        {
            var catalogItem = catalogById[requestedItem.VppId];
            if (requestedItem.Qty <= catalogItem.MaxQuantityPerOrder)
            {
                continue;
            }

            throw new BusinessException(
                $"{catalogItem.VppName}: đã nhập {requestedItem.Qty}, tối đa {catalogItem.MaxQuantityPerOrder} trong mỗi đơn.");
        }
    }
}
