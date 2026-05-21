using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class PriceListService : IPriceListService
    {
        private const string DefaultConflictMessage = "Another price list is already default.";

        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUserNameResolver _userNameResolver;

        public PriceListService(
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IUserNameResolver userNameResolver)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _userNameResolver = userNameResolver;
        }

        public async Task<List<L07_PriceListResDTO>> ListAsync()
        {
            var result = await PriceListDtoQuery()
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.PriceListName)
                .ThenBy(x => x.PriceListCode)
                .ToListAsync();

            return await _userNameResolver.WithUserNamesAsync(result, _scopedUow.VPPContext);
        }

        public async Task<L07_PriceListResDTO?> GetByIdAsync(Guid id)
        {
            var result = await PriceListDtoQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (result == null)
            {
                return null;
            }

            await _userNameResolver.IncludeUserInfoAsync(result, _scopedUow.VPPContext);
            return result;
        }

        public async Task<L07_PriceListResDTO> CreateAsync(L07_PriceListCreateReqDTO req, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(userId, now);
                }

                var entity = new L07_PriceList
                {
                    Id = Guid.NewGuid(),
                    PriceListCode = req.Code,
                    PriceListName = req.Name,
                    Description = req.Description,
                    IsDefault = req.IsDefault,
                    CreateUserId = userId,
                    CreateDate = now,
                    UpdateUserId = userId,
                    UpdateDate = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<L07_PriceList>().Add(entity);
                await _scopedUow.CommitAsync();

                return (await GetByIdAsync(entity.Id))!;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(DefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<L07_PriceListResDTO> UpdateAsync(L07_PriceListUpdateReqDTO req, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(userId, now);
                }

                entity.PriceListCode = req.Code;
                entity.PriceListName = req.Name;
                entity.Description = req.Description;
                entity.IsDefault = req.IsDefault;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();

                return (await GetByIdAsync(entity.Id))!;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(DefaultConflictMessage, ex);
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
                var entity = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                if (entity.IsDefault)
                {
                    throw new BusinessException("Cannot delete the default price list.");
                }

                var itemCount = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                    .CountAsync(x => x.L07_PriceListId == id && !x.IsDeleted);
                if (itemCount > 0)
                {
                    throw new BusinessException($"Price list has {itemCount} items.");
                }

                var now = _dateTimeProvider.Now;
                entity.IsDeleted = true;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();
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
                var entity = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                var now = _dateTimeProvider.Now;
                await DemoteDefaultsAsync(userId, now);
                entity.IsDefault = true;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException(DefaultConflictMessage, ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<L07_PriceListResDTO> CloneAsync(L07_PriceListCloneReqDTO req, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var source = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.SourceId && !x.IsDeleted);
                if (source == null)
                {
                    throw new BusinessException("Source price list not found.");
                }

                var now = _dateTimeProvider.Now;
                var clone = new L07_PriceList
                {
                    Id = Guid.NewGuid(),
                    PriceListCode = req.Code,
                    PriceListName = req.Name,
                    Description = req.Description,
                    IsDefault = false,
                    CreateUserId = userId,
                    CreateDate = now,
                    UpdateUserId = userId,
                    UpdateDate = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<L07_PriceList>().Add(clone);

                var sourceRows = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                    .AsNoTracking()
                    .Where(x => x.L07_PriceListId == source.Id && !x.IsDeleted)
                    .ToListAsync();

                var clonedRows = sourceRows.Select(row => new L06_VPPSupplierMapping
                {
                    Id = Guid.NewGuid(),
                    L04_VPPId = row.L04_VPPId,
                    L05_VPPSupplierId = row.L05_VPPSupplierId,
                    L07_PriceListId = clone.Id,
                    Price = row.Price,
                    IsDefault = row.IsDefault,
                    Description = row.Description,
                    CreateUserId = userId,
                    CreateDate = now,
                    UpdateUserId = userId,
                    UpdateDate = now,
                    IsDeleted = false
                }).ToList();

                _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>().AddRange(clonedRows);
                await _scopedUow.CommitAsync();

                return (await GetByIdAsync(clone.Id))!;
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private IQueryable<L07_PriceListResDTO> PriceListDtoQuery()
        {
            return _scopedUow.VPPContext.Set<L07_PriceList>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => new L07_PriceListResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreateUserId = x.CreateUserId,
                    CreateDate = x.CreateDate,
                    UpdateUserId = x.UpdateUserId,
                    UpdateDate = x.UpdateDate,
                    IsDeleted = x.IsDeleted,
                    PriceListCode = x.PriceListCode,
                    PriceListName = x.PriceListName,
                    IsDefault = x.IsDefault,
                    ItemCount = x.L06_VPPSupplierMappings!.Count(m => !m.IsDeleted)
                });
        }

        private async Task DemoteDefaultsAsync(int userId, DateTime now)
        {
            var rows = await _scopedUow.VPPContext.Set<L07_PriceList>()
                .Where(x => x.IsDefault && !x.IsDeleted)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.IsDefault = false;
                row.UpdateUserId = userId;
                row.UpdateDate = now;
            }
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2601 || sqlException.Number == 2627);
    }
}
