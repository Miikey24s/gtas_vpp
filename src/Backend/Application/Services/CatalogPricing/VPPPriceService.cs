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
    public class VPPPriceService : IVPPPriceService
    {
        private const string OneDefaultConflictMessage = "Only one default supplier per VPP is allowed.";

        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;

        public VPPPriceService(
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<List<SupplierProductMappingResDTO>> ListByVPPAsync(Guid vppId, Guid? priceListId = null)
        {
            var effectivePriceListId = priceListId ?? await GetDefaultPriceListIdAsync();
            if (!effectivePriceListId.HasValue)
            {
                return new List<SupplierProductMappingResDTO>();
            }

            return await PriceDtoQuery()
                .Where(x => x.VppItemId == vppId && x.PriceListId == effectivePriceListId.Value)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.SupplierName)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<List<SupplierProductMappingResDTO>> ListBySupplierAsync(Guid supplierId, Guid? priceListId = null, bool showDeleted = false)
        {
            var effectivePriceListId = priceListId ?? await GetDefaultPriceListIdAsync();
            if (!effectivePriceListId.HasValue)
            {
                return new List<SupplierProductMappingResDTO>();
            }

            return await PriceDtoQuery(showDeleted)
                .Where(x => x.SupplierId == supplierId && x.PriceListId == effectivePriceListId.Value)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.VppItemName)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<(List<VppItemPriceResDTO> Data, int TotalCount)> QueryItemPricesAsync(
            Guid supplierId,
            Guid? priceListId = null,
            bool showDeleted = false,
            string? search = null,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null)
        {
            var effectivePriceListId = priceListId ?? await GetDefaultPriceListIdAsync();
            if (!effectivePriceListId.HasValue)
            {
                return (new List<VppItemPriceResDTO>(), 0);
            }

            var vppQuery = _scopedUow.VPPContext.Set<VppItem>()
                .AsNoTracking();
            if (!showDeleted)
            {
                vppQuery = vppQuery.Where(x => !x.IsDeleted);
            }

            var mappingQuery = _scopedUow.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking()
                .Where(x => x.SupplierId == supplierId
                            && x.PriceListId == effectivePriceListId.Value
                            && (showDeleted || !x.IsDeleted));
            IQueryable<VppItemPriceResDTO> query =
                from vpp in vppQuery
                from mapping in mappingQuery.Where(x => x.VppItemId == vpp.Id).DefaultIfEmpty()
                select new VppItemPriceResDTO
                {
                    VppId = vpp.Id,
                    VppCode = vpp.VppCode,
                    VppName = vpp.VppName,
                    CategoryName = vpp.VppCategory == null
                        ? null
                        : vpp.VppCategory.VppCategoryName,
                    UomName = vpp.Uom == null
                        ? null
                        : vpp.Uom.Value,
                    PriceMappingId = mapping == null ? null : mapping.Id,
                    Price = mapping == null ? null : mapping.Price,
                    NetPrice = mapping == null ? null : (mapping.NetPrice == 0m && mapping.Price != 0m ? mapping.Price : mapping.NetPrice),
                    VatRate = mapping == null ? 0m : mapping.VatRate,
                    MinimumOrderQuantity = mapping == null ? 0m : mapping.MinimumOrderQuantity,
                    LeadTimeDays = mapping == null ? 0 : mapping.LeadTimeDays,
                    IsDefault = mapping != null && mapping.IsDefault,
                    IsDeleted = mapping != null && mapping.IsDeleted,
                    Description = mapping == null ? null : mapping.Description,
                    UpdatedAtUtc = mapping == null ? null : mapping.UpdatedAtUtc
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchText = search.Trim();
                query = query.Where(x =>
                    (x.VppCode != null && x.VppCode.Contains(searchText))
                    || (x.VppName != null && x.VppName.Contains(searchText))
                    || (x.CategoryName != null && x.CategoryName.Contains(searchText))
                    || (x.UomName != null && x.UomName.Contains(searchText)));
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(VppItemPriceResDTO).GetProperty(distinct);
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

                    IEnumerable<object> pageValues = filteredValues.Cast<object>();
                    if (skip.HasValue && skip.Value > 0)
                    {
                        pageValues = pageValues.Skip(skip.Value);
                    }

                    if (top.HasValue && top.Value > 0)
                    {
                        pageValues = pageValues.Take(top.Value);
                    }

                    var distinctRows = pageValues.Select(val =>
                    {
                        var dto = new VppItemPriceResDTO();
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
                query = query.OrderBy(x => x.VppCode).ThenBy(x => x.VppName);
            }

            if (skip.HasValue && skip.Value > 0)
            {
                query = query.Skip(skip.Value);
            }

            if (top.HasValue && top.Value > 0)
            {
                query = query.Take(top.Value);
            }

            return (await query.ToListAsync(), totalCount);
        }

        public async Task<SupplierProductMappingResDTO> CreateAsync(SupplierProductPriceCreateReqDTO req, int userId)
        {
            var netPrice = req.NetPrice ?? req.Price;
            ValidatePriceTerms(netPrice, req.VatRate, req.MinimumOrderQuantity, req.LeadTimeDays);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var priceBook = await ValidateReferencesAsync(req.VppItemId, req.SupplierId, req.PriceListId);
                EnsureEditable(priceBook);

                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(req.PriceListId, req.VppItemId, userId, now);
                }

                var entity = new SupplierProductMapping
                {
                    Id = Guid.NewGuid(),
                    VppItemId = req.VppItemId,
                    SupplierId = req.SupplierId,
                    PriceListId = req.PriceListId,
                    Price = netPrice,
                    NetPrice = netPrice,
                    VatRate = req.VatRate,
                    MinimumOrderQuantity = req.MinimumOrderQuantity,
                    LeadTimeDays = req.LeadTimeDays,
                    IsDefault = req.IsDefault,
                    Description = req.Description,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<SupplierProductMapping>().Add(entity);
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

        public async Task<SupplierProductMappingResDTO> UpdateAsync(SupplierProductPriceUpdateReqDTO req, int userId)
        {
            var netPrice = req.NetPrice ?? req.Price;
            ValidatePriceTerms(netPrice, req.VatRate, req.MinimumOrderQuantity, req.LeadTimeDays);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                var priceBook = await ValidateReferencesAsync(req.VppItemId, req.SupplierId, req.PriceListId);
                EnsureEditable(priceBook);

                var now = _dateTimeProvider.Now;
                if (req.IsDefault)
                {
                    await DemoteDefaultsAsync(req.PriceListId, req.VppItemId, userId, now);
                }

                entity.VppItemId = req.VppItemId;
                entity.SupplierId = req.SupplierId;
                entity.PriceListId = req.PriceListId;
                entity.Price = netPrice;
                entity.NetPrice = netPrice;
                entity.VatRate = req.VatRate;
                entity.MinimumOrderQuantity = req.MinimumOrderQuantity;
                entity.LeadTimeDays = req.LeadTimeDays;
                entity.IsDefault = req.IsDefault;
                entity.Description = req.Description;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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
                var entity = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                await EnsureEditableAsync(entity.PriceListId);

                var now = _dateTimeProvider.Now;
                entity.IsDeleted = true;
                entity.IsDefault = false;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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

        public async Task<SupplierProductMappingResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                await EnsureEditableAsync(entity.PriceListId);

                var now = _dateTimeProvider.Now;
                entity.IsDeleted = isDeleted;
                if (isDeleted)
                {
                    entity.IsDefault = false;
                }
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

                await _scopedUow.CommitAsync();
                return await GetRequiredDtoAsync(entity.Id, includeDeleted: true);
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
                var entity = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price mapping not found.");
                }

                await EnsureEditableAsync(entity.PriceListId);

                var now = _dateTimeProvider.Now;
                await DemoteDefaultsAsync(entity.PriceListId, entity.VppItemId, userId, now);

                entity.IsDefault = true;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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

        private IQueryable<SupplierProductMappingResDTO> PriceDtoQuery(bool showDeleted = false)
        {
            var query = _scopedUow.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking();
            if (!showDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }
            return query
                .Select(x => new SupplierProductMappingResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedByUserId = x.UpdatedByUserId,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    Price = x.Price,
                    NetPrice = x.NetPrice == 0m && x.Price != 0m ? x.Price : x.NetPrice,
                    VatRate = x.VatRate,
                    MinimumOrderQuantity = x.MinimumOrderQuantity,
                    LeadTimeDays = x.LeadTimeDays,
                    RowVersion = x.RowVersion,
                    IsDefault = x.IsDefault,
                    VppItemId = x.VppItemId,
                    VppItemName = x.VppItem == null
                        ? null
                        : x.VppItem.VppName,
                    SupplierId = x.SupplierId,
                    SupplierName = x.Supplier == null
                        ? null
                        : x.Supplier.SupplierName,
                    PriceListId = x.PriceListId,
                    PriceListName = x.PriceList == null
                        ? null
                        : x.PriceList.PriceListName
                });
        }

        private async Task<SupplierProductMappingResDTO> GetRequiredDtoAsync(
            Guid id,
            bool includeDeleted = false)
        {
            return await PriceDtoQuery(includeDeleted).FirstAsync(x => x.Id == id);
        }

        private async Task<PriceList> ValidateReferencesAsync(Guid vppId, Guid supplierId, Guid priceListId)
        {
            var vppExists = await _scopedUow.VPPContext.Set<VppItem>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == vppId && !x.IsDeleted);
            if (!vppExists)
            {
                throw new BusinessException("VPP product does not exist or has been deleted.");
            }

            var supplierExists = await _scopedUow.VPPContext.Set<Supplier>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == supplierId && !x.IsDeleted);
            if (!supplierExists)
            {
                throw new BusinessException("VPP supplier does not exist or has been deleted.");
            }

            var priceList = await _scopedUow.VPPContext.Set<PriceList>()
                .FirstOrDefaultAsync(x => x.Id == priceListId && !x.IsDeleted);
            if (priceList == null)
            {
                throw new BusinessException("Price list does not exist or has been deleted.");
            }

            if (priceList.SupplierId.HasValue && priceList.SupplierId.Value != supplierId)
            {
                throw new BusinessException("Price book belongs to a different supplier.");
            }

            return priceList;
        }

        private async Task DemoteDefaultsAsync(Guid priceListId, Guid vppId, int userId, DateTime now)
        {
            var existingRows = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                .Where(x => x.PriceListId == priceListId && x.VppItemId == vppId && !x.IsDeleted)
                .ToListAsync();

            foreach (var row in existingRows)
            {
                row.IsDefault = false;
                row.UpdatedByUserId = userId;
                row.UpdatedAtUtc = now;
            }
        }

        private static void ValidatePriceTerms(decimal price, decimal vatRate, decimal moq, int leadTimeDays)
        {
            if (price < 0)
            {
                throw new BusinessException("Price must be greater than or equal to zero.");
            }
            if (vatRate < 0 || vatRate > 100)
            {
                throw new BusinessException("VAT rate must be between zero and 100.");
            }
            if (moq < 0)
            {
                throw new BusinessException("Minimum order quantity must be greater than or equal to zero.");
            }
            if (leadTimeDays < 0)
            {
                throw new BusinessException("Lead time must be greater than or equal to zero.");
            }
        }

        private static void EnsureEditable(PriceList priceBook)
        {
            if (priceBook.Status == PriceListStatus.Expired)
            {
                throw new BusinessException("This price list is no longer active. Restore it before changing prices.");
            }
        }

        private async Task EnsureEditableAsync(Guid priceListId)
        {
            var priceBook = await _scopedUow.VPPContext.Set<PriceList>()
                .FirstOrDefaultAsync(x => x.Id == priceListId && !x.IsDeleted);
            if (priceBook == null)
            {
                throw new BusinessException("Price list does not exist or has been deleted.");
            }

            EnsureEditable(priceBook);
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2601 || sqlException.Number == 2627);

        private async Task<Guid?> GetDefaultPriceListIdAsync()
        {
            return await _scopedUow.VPPContext.Set<PriceList>()
                .AsNoTracking()
                .Where(x => x.IsDefault && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync();
        }
    }
}
