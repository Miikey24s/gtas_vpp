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

        var rows = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.PriceListId == priceListId
                && !mapping.IsDeleted
                && mapping.VppItem != null
                && !mapping.VppItem.IsDeleted)
            .OrderBy(mapping => mapping.VppItem!.VppCode)
            .ThenBy(mapping => mapping.VppItem!.VppName)
            .Select(mapping => new PriceListWorkbookRow(
                mapping.VppItem!.VppCode,
                mapping.SupplierSku,
                mapping.VppItem.VppName,
                mapping.Price,
                mapping.VatRate,
                mapping.MinimumOrderQuantity,
                mapping.LeadTimeDays,
                mapping.IsDefault,
                mapping.Description))
            .ToListAsync(cancellationToken);

        return new PriceListExportResult(
            PriceListWorkbookBuilder.Build(rows),
            ExportFileContract.PriceList(priceList.PriceListCode, priceList.PriceListName, "xlsx"),
            ExportFileContract.ExcelContentType);
    }
}
