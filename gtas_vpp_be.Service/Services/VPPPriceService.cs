using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class VPPPriceService : IVPPPriceService
    {
        private const string OneDefaultConflictMessage = "Only one default supplier per VPP is allowed.";

        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;

        public VPPPriceService(IUnitOfWork scopedUow, IDateTimeProvider dateTimeProvider)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<List<L06_VPPSupplierMappingResDTO>> ListByVPPAsync(Guid vppId, Guid? priceListId = null)
        {
            var effectivePriceListId = priceListId ?? await GetDefaultPriceListIdAsync();
            if (!effectivePriceListId.HasValue)
            {
                return new List<L06_VPPSupplierMappingResDTO>();
            }

            return await PriceDtoQuery()
                .Where(x => x.L04_VPPId == vppId && x.L07_PriceListId == effectivePriceListId.Value)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.L05_SupplierName)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<List<L06_VPPSupplierMappingResDTO>> ListBySupplierAsync(Guid supplierId, Guid? priceListId = null, bool showDeleted = false)
        {
            var effectivePriceListId = priceListId ?? await GetDefaultPriceListIdAsync();
            if (!effectivePriceListId.HasValue)
            {
                return new List<L06_VPPSupplierMappingResDTO>();
            }

            return await PriceDtoQuery(showDeleted)
                .Where(x => x.L05_VPPSupplierId == supplierId && x.L07_PriceListId == effectivePriceListId.Value)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.L04_VPPName)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<L06_VPPSupplierMappingResDTO> CreateAsync(L06_PriceCreateReqDTO req, int userId)
        {
            ValidatePrice(req.Price);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                await ValidateReferencesAsync(req.L04_VPPId, req.L05_VPPSupplierId, req.L07_PriceListId);

                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(req.L07_PriceListId, req.L04_VPPId, userId, now);
                }

                var entity = new L06_VPPSupplierMapping
                {
                    Id = Guid.NewGuid(),
                    L04_VPPId = req.L04_VPPId,
                    L05_VPPSupplierId = req.L05_VPPSupplierId,
                    L07_PriceListId = req.L07_PriceListId,
                    Price = req.Price,
                    IsDefault = req.IsDefault,
                    Description = req.Description,
                    CreateUserId = userId,
                    CreateDate = now,
                    UpdateUserId = userId,
                    UpdateDate = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>().Add(entity);
                await _scopedUow.CommitAsync();

                return await GetRequiredDtoAsync(entity.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(OneDefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<L06_VPPSupplierMappingResDTO> UpdateAsync(L06_PriceUpdateReqDTO req, int userId)
        {
            ValidatePrice(req.Price);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                await ValidateReferencesAsync(req.L04_VPPId, req.L05_VPPSupplierId, req.L07_PriceListId);

                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(req.L07_PriceListId, req.L04_VPPId, userId, now);
                }

                entity.L04_VPPId = req.L04_VPPId;
                entity.L05_VPPSupplierId = req.L05_VPPSupplierId;
                entity.L07_PriceListId = req.L07_PriceListId;
                entity.Price = req.Price;
                entity.IsDefault = req.IsDefault;
                entity.Description = req.Description;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();

                return await GetRequiredDtoAsync(entity.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(OneDefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                var now = _dateTimeProvider.Now;
                entity.IsDeleted = true;
                entity.IsDefault = false;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(OneDefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task SetDefaultAsync(Guid id, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                var now = _dateTimeProvider.Now;
                await DemoteDefaultsAsync(entity.L07_PriceListId, entity.L04_VPPId, userId, now);

                entity.IsDefault = true;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(OneDefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private IQueryable<L06_VPPSupplierMappingResDTO> PriceDtoQuery(bool showDeleted = false)
        {
            var query = _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                .AsNoTracking();
            if (!showDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }
            return query
                .Select(x => new L06_VPPSupplierMappingResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreateUserId = x.CreateUserId,
                    CreateDate = x.CreateDate,
                    UpdateUserId = x.UpdateUserId,
                    UpdateDate = x.UpdateDate,
                    IsDeleted = x.IsDeleted,
                    Price = x.Price,
                    IsDefault = x.IsDefault,
                    L04_VPPId = x.L04_VPPId,
                    L04_VPPName = x.L04_VPP != null ? x.L04_VPP.VPPName : null,
                    L05_VPPSupplierId = x.L05_VPPSupplierId,
                    L05_SupplierName = x.L05_VPPSupplier != null ? x.L05_VPPSupplier.SupplierName : null,
                    L07_PriceListId = x.L07_PriceListId,
                    L07_PriceListName = x.L07_PriceList != null ? x.L07_PriceList.PriceListName : null
                });
        }

        private async Task<L06_VPPSupplierMappingResDTO> GetRequiredDtoAsync(Guid id)
        {
            return await PriceDtoQuery().FirstAsync(x => x.Id == id);
        }

        private async Task ValidateReferencesAsync(Guid vppId, Guid supplierId, Guid priceListId)
        {
            var vppExists = await _scopedUow.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == vppId && !x.IsDeleted);
            if (!vppExists)
            {
                throw new BusinessException("VPP product does not exist or has been deleted.");
            }

            var supplierExists = await _scopedUow.VPPContext.Set<L05_VPPSupplier>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == supplierId && !x.IsDeleted);
            if (!supplierExists)
            {
                throw new BusinessException("VPP supplier does not exist or has been deleted.");
            }

            var priceListExists = await _scopedUow.VPPContext.Set<L07_PriceList>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == priceListId && !x.IsDeleted);
            if (!priceListExists)
            {
                throw new BusinessException("Price list does not exist or has been deleted.");
            }
        }

        private async Task DemoteDefaultsAsync(Guid priceListId, Guid vppId, int userId, DateTime now)
        {
            var existingRows = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                .Where(x => x.L07_PriceListId == priceListId && x.L04_VPPId == vppId && !x.IsDeleted)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.IsDefault = false;
                row.UpdateUserId = userId;
                row.UpdateDate = now;
            }
        }

        private static void ValidatePrice(decimal price)
        {
            if (price < 0)
            {
                throw new BusinessException("Price must be greater than or equal to zero.");
            }
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2601 || sqlException.Number == 2627);

        private async Task<Guid?> GetDefaultPriceListIdAsync()
        {
            return await _scopedUow.VPPContext.Set<L07_PriceList>()
                .AsNoTracking()
                .Where(x => x.IsDefault && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();
        }
    }
}
