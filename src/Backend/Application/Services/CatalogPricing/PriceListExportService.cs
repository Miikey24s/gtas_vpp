using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public sealed class PriceListExportService(IUnitOfWork unitOfWork) : IPriceListExportService
{
    public async Task<PriceListExportResult> ExportExcelAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default)
    {
        var priceList = await unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(item => item.Id == priceListId)
            .Select(item => new
            {
                item.PriceListCode,
                item.PriceListName
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessException("Không tìm thấy bảng giá.");

        var mappings = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.PriceListId == priceListId
                && !mapping.IsDeleted)
            .ToListAsync(cancellationToken);
        if (mappings.GroupBy(mapping => mapping.VppItemId).Any(group => group.Count() > 1))
        {
            throw new BusinessException("Bảng giá có mặt hàng bị lặp. Vui lòng xử lý dữ liệu trước khi xuất file.");
        }

        var mappingByItem = mappings.ToDictionary(mapping => mapping.VppItemId);
        var units = await unitOfWork.VPPContext.Set<LookupValue>()
            .AsNoTracking()
            .Where(unit => !unit.IsDeleted)
            .ToDictionaryAsync(unit => unit.Id, unit => unit.Value, cancellationToken);
        var catalog = await unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.VppCode)
            .ThenBy(item => item.VppName)
            .Select(item => new
            {
                item.Id,
                item.VppCode,
                item.VppName,
                item.UomId
            })
            .ToListAsync(cancellationToken);
        var rows = catalog.Select(item =>
        {
            mappingByItem.TryGetValue(item.Id, out var mapping);
            return new PriceListWorkbookRow(
                item.VppCode,
                item.VppName,
                units.GetValueOrDefault(item.UomId),
                mapping?.Price,
                mapping?.VatRate,
                mapping?.Description);
        }).ToArray();

        return new PriceListExportResult(
            PriceListWorkbookBuilder.Build(rows),
            ExportFileContract.PriceList(priceList.PriceListCode, priceList.PriceListName, "xlsx"),
            ExportFileContract.ExcelContentType);
    }
}
