using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Dynamic.Core;

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

        public async Task<List<L07_PriceListResDTO>> ListAsync(bool showDeleted = false)
        {
            var result = await ApplyDefaultOrder(PriceListDtoQuery(showDeleted))
                .ToListAsync();

            return await _userNameResolver.WithUserNamesAsync(result, _scopedUow.VPPContext);
        }

        public async Task<(List<L07_PriceListResDTO> Data, int TotalCount)> QueryAsync(
            bool showDeleted = false,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null)
        {
            IQueryable<L07_PriceListResDTO> query = PriceListDtoQuery(showDeleted);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(L07_PriceListResDTO).GetProperty(distinct);
                if (propertyInfo != null)
                {
                    var distinctValues = await query
                        .Select(distinct)
                        .Distinct()
                        .ToDynamicListAsync();

                    var filteredValues = distinctValues
                        .Where(val => val != null)
                        .Where(val => string.IsNullOrWhiteSpace(distinctFilter)
                            || (Convert.ToString(val, CultureInfo.CurrentCulture)?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                        .ToList();

                    IEnumerable<object> pagedValues = filteredValues.Cast<object>();
                    if (skip.HasValue && skip.Value > 0)
                    {
                        pagedValues = pagedValues.Skip(skip.Value);
                    }

                    if (top.HasValue && top.Value > 0)
                    {
                        pagedValues = pagedValues.Take(top.Value);
                    }

                    var distinctRows = pagedValues.Select(val =>
                    {
                        var dto = new L07_PriceListResDTO();
                        propertyInfo.SetValue(dto, val);
                        return dto;
                    }).ToList();

                    return (distinctRows, filteredValues.Count);
                }
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                query = query.OrderBy(orderby);
            }
            else
            {
                query = ApplyDefaultOrder(query);
            }

            if (skip.HasValue && skip.Value > 0)
            {
                query = query.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                query = query.Take(top.Value);
            }

            var result = await query
                .ToListAsync();

            return (await _userNameResolver.WithUserNamesAsync(result, _scopedUow.VPPContext), totalCount);
        }

        public async Task<L07_PriceListResDTO?> GetByIdAsync(Guid id)
        {
            var result = await PriceListDtoQuery(true).FirstOrDefaultAsync(x => x.Id == id);
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

        public async Task<L07_PriceListResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                if (isDeleted && entity.IsDefault)
                {
                    throw new BusinessException("Cannot delete the default price list.");
                }

                if (isDeleted)
                {
                    var itemCount = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                        .CountAsync(x => x.L07_PriceListId == id && !x.IsDeleted);
                    if (itemCount > 0)
                    {
                        throw new BusinessException($"Price list has {itemCount} items.");
                    }
                }

                var now = _dateTimeProvider.Now;
                entity.IsDeleted = isDeleted;
                entity.UpdateUserId = userId;
                entity.UpdateDate = now;

                await _scopedUow.CommitAsync();
                return (await GetByIdAsync(id))!;
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public Task DeleteAsync(Guid id, int userId)
            => SetDeletedAsync(id, true, userId);

        public async Task HardDeleteAsync(Guid id)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                _scopedUow.VPPContext.Set<L07_PriceList>().Remove(entity);
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

        private IQueryable<L07_PriceListResDTO> PriceListDtoQuery(bool showDeleted = false)
        {
            var query = _scopedUow.VPPContext.Set<L07_PriceList>()
                .AsNoTracking();

            if (!showDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return query.Select(x => new L07_PriceListResDTO
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
                    ItemCount = x.L06_VPPSupplierMappings!.Count(m => showDeleted || !m.IsDeleted)
                });
        }

        private static IOrderedQueryable<L07_PriceListResDTO> ApplyDefaultOrder(IQueryable<L07_PriceListResDTO> query)
        {
            return query
                .OrderBy(x => x.IsDeleted)
                .ThenByDescending(x => x.IsDefault)
                .ThenBy(x => x.PriceListName)
                .ThenBy(x => x.PriceListCode);
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
