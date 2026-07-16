using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Domain;
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
            ValidatePriceBook(req.SupplierId, req.Version, req.EffectiveFromUtc, req.EffectiveToUtc, req.CurrencyCode);
            PriceBookWorkflowService.ValidateCommercialTerms(
                req.DiscountRate, req.RebateAmount, req.FeeAmount, req.ShippingAmount);
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                await ValidateSupplierAsync(req.SupplierId);
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
                    SupplierId = req.SupplierId,
                    Version = req.Version,
                    EffectiveFromUtc = req.EffectiveFromUtc.HasValue ? NormalizeUtc(req.EffectiveFromUtc.Value) : nowUtc,
                    EffectiveToUtc = req.EffectiveToUtc.HasValue ? NormalizeUtc(req.EffectiveToUtc.Value) : null,
                    Status = L07_PriceListStatus.Draft,
                    CurrencyCode = NormalizeCurrency(req.CurrencyCode),
                    VatPolicy = NormalizeVatPolicy(req.VatPolicy),
                    ContractCode = NormalizeOptional(req.ContractCode),
                    DiscountRate = req.DiscountRate,
                    RebateAmount = req.RebateAmount,
                    FeeAmount = req.FeeAmount,
                    ShippingAmount = req.ShippingAmount,
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

                if (entity.Status == L07_PriceListStatus.Published)
                {
                    throw new BusinessException("Published price books are immutable; create a new version instead.");
                }

                var effectiveFromUtc = req.EffectiveFromUtc ?? entity.EffectiveFromUtc;
                ValidatePriceBook(req.SupplierId, req.Version, effectiveFromUtc, req.EffectiveToUtc, req.CurrencyCode);
                PriceBookWorkflowService.ValidateCommercialTerms(
                    req.DiscountRate, req.RebateAmount, req.FeeAmount, req.ShippingAmount);
                await ValidateSupplierAsync(req.SupplierId);

                if (req.RowVersion is { Length: > 0 })
                {
                    if (entity.RowVersion is not { Length: > 0 } || !entity.RowVersion.SequenceEqual(req.RowVersion))
                    {
                        throw new ConflictException("The price book changed. Reload before retrying.");
                    }
                    _scopedUow.VPPContext.Entry(entity).Property(x => x.RowVersion).OriginalValue = req.RowVersion;
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
                entity.SupplierId = req.SupplierId;
                entity.Version = req.Version;
                entity.EffectiveFromUtc = NormalizeUtc(effectiveFromUtc);
                entity.EffectiveToUtc = req.EffectiveToUtc.HasValue ? NormalizeUtc(req.EffectiveToUtc.Value) : null;
                entity.CurrencyCode = NormalizeCurrency(req.CurrencyCode);
                entity.VatPolicy = NormalizeVatPolicy(req.VatPolicy);
                entity.ContractCode = NormalizeOptional(req.ContractCode);
                entity.DiscountRate = req.DiscountRate;
                entity.RebateAmount = req.RebateAmount;
                entity.FeeAmount = req.FeeAmount;
                entity.ShippingAmount = req.ShippingAmount;
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

                if (isDeleted && entity.Status == L07_PriceListStatus.Published)
                {
                    throw new BusinessException("Published price books cannot be deleted; expire them through the versioned workflow.");
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

                if (entity.Status == L07_PriceListStatus.Published)
                {
                    throw new BusinessException("Published price books cannot be hard-deleted.");
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
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                var clone = new L07_PriceList
                {
                    Id = Guid.NewGuid(),
                    PriceListCode = req.Code,
                    PriceListName = req.Name,
                    Description = req.Description,
                    IsDefault = false,
                    SupplierId = source.SupplierId,
                    Version = source.Version + 1,
                    EffectiveFromUtc = nowUtc,
                    EffectiveToUtc = null,
                    Status = L07_PriceListStatus.Draft,
                    CurrencyCode = source.CurrencyCode,
                    VatPolicy = source.VatPolicy,
                    ContractCode = source.ContractCode,
                    DiscountRate = source.DiscountRate,
                    RebateAmount = source.RebateAmount,
                    FeeAmount = source.FeeAmount,
                    ShippingAmount = source.ShippingAmount,
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
                    NetPrice = row.NetPrice == 0m && row.Price != 0m ? row.Price : row.NetPrice,
                    VatRate = row.VatRate,
                    MinimumOrderQuantity = row.MinimumOrderQuantity,
                    LeadTimeDays = row.LeadTimeDays,
                    SupplierSku = row.SupplierSku,
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
                    SupplierId = x.SupplierId,
                    SupplierName = x.Supplier == null ? null : x.Supplier.SupplierName,
                    Version = x.Version,
                    EffectiveFromUtc = x.EffectiveFromUtc,
                    EffectiveToUtc = x.EffectiveToUtc,
                    Status = x.Status == L07_PriceListStatus.Draft
                        ? "Draft"
                        : x.Status == L07_PriceListStatus.Published ? "Published" : "Expired",
                    CurrencyCode = x.CurrencyCode,
                    VatPolicy = x.VatPolicy,
                    ContractCode = x.ContractCode,
                    LegacyBackfillStatus = x.LegacyBackfillStatus,
                    DiscountRate = x.DiscountRate,
                    RebateAmount = x.RebateAmount,
                    FeeAmount = x.FeeAmount,
                    ShippingAmount = x.ShippingAmount,
                    PublishedAtUtc = x.PublishedAtUtc,
                    PublishedByUserId = x.PublishedByUserId,
                    ExpiredAtUtc = x.ExpiredAtUtc,
                    ExpiredByUserId = x.ExpiredByUserId,
                    StatusReason = x.StatusReason,
                    RowVersion = x.RowVersion,
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

        private async Task ValidateSupplierAsync(Guid? supplierId)
        {
            if (!supplierId.HasValue)
            {
                return;
            }

            var exists = await _scopedUow.VPPContext.Set<L05_VPPSupplier>()
                .AsNoTracking()
                .AnyAsync(x => x.Id == supplierId.Value && !x.IsDeleted);
            if (!exists)
            {
                throw new BusinessException("Supplier does not exist or has been deleted.");
            }
        }

        private static void ValidatePriceBook(
            Guid? supplierId,
            int version,
            DateTime? effectiveFromUtc,
            DateTime? effectiveToUtc,
            string currencyCode)
        {
            if (version <= 0)
            {
                throw new BusinessException("Price book version must be greater than zero.");
            }

            if (effectiveFromUtc.HasValue && effectiveToUtc.HasValue
                && NormalizeUtc(effectiveToUtc.Value) <= NormalizeUtc(effectiveFromUtc.Value))
            {
                throw new BusinessException("EffectiveToUtc must be later than EffectiveFromUtc.");
            }

            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
            {
                throw new BusinessException("CurrencyCode must contain exactly three letters.");
            }

            _ = supplierId;
        }

        private static string NormalizeCurrency(string value)
            => value.Trim().ToUpperInvariant();

        private static string NormalizeVatPolicy(string? value)
            => string.IsNullOrWhiteSpace(value) ? "item-rate" : value.Trim().ToLowerInvariant();

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static DateTime NormalizeUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
