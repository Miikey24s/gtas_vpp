using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services
{
    public interface IVPPPriceService
    {
        Task<List<SupplierProductMappingResDTO>> ListByVPPAsync(Guid vppId, Guid? priceListId = null);
        Task<List<SupplierProductMappingResDTO>> ListBySupplierAsync(Guid supplierId, Guid? priceListId = null, bool showDeleted = false);
        Task<(List<VppItemPriceResDTO> Data, int TotalCount)> QueryItemPricesAsync(
            Guid supplierId,
            Guid? priceListId = null,
            bool showDeleted = false,
            string? search = null,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null);
        Task<SupplierProductMappingResDTO> CreateAsync(SupplierProductPriceCreateReqDTO req, int userId);
        Task<SupplierProductMappingResDTO> UpdateAsync(SupplierProductPriceUpdateReqDTO req, int userId);
        Task<SupplierProductMappingResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId);
        Task DeleteAsync(Guid id, int userId);
        Task SetDefaultAsync(Guid id, int userId);
    }
}
