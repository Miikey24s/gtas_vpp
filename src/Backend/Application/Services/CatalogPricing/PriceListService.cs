using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.Constants;
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

        public async Task<List<PriceListResDTO>> ListAsync(bool showDeleted = false)
        {
            var result = await ApplyDefaultOrder(PriceListDtoQuery(showDeleted))
                .ToListAsync();

            return await _userNameResolver.WithUserNamesAsync(result, _scopedUow.VPPContext);
        }

        public async Task<(List<PriceListResDTO> Data, int TotalCount)> QueryAsync(
            bool showDeleted = false,
            string? search = null,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null)
        {
            IQueryable<PriceListResDTO> query = PriceListDtoQuery(showDeleted);

            var normalizedSearch = search?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(x =>
                    (x.PriceListCode != null && x.PriceListCode.Contains(normalizedSearch))
                    || (x.PriceListName != null && x.PriceListName.Contains(normalizedSearch))
                    || (x.SupplierName != null && x.SupplierName.Contains(normalizedSearch))
                    || (x.ContractCode != null && x.ContractCode.Contains(normalizedSearch)));
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                var propertyInfo = typeof(PriceListResDTO).GetProperty(distinct);
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
                        var dto = new PriceListResDTO();
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

        public async Task<PriceListResDTO?> GetByIdAsync(Guid id)
        {
            var result = await PriceListDtoQuery(true).FirstOrDefaultAsync(x => x.Id == id);
            if (result == null)
            {
                return null;
            }

            await _userNameResolver.IncludeUserInfoAsync(result, _scopedUow.VPPContext);
            return result;
        }

        public async Task<PriceListResDTO> CreateAsync(PriceListCreateReqDTO req, int userId)
        {
            ValidatePriceBook(req.SupplierId, req.Version, null, null, req.CurrencyCode);
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

                var entity = new PriceList
                {
                    Id = Guid.NewGuid(),
                    PriceListCode = req.Code,
                    PriceListName = req.Name,
                    Description = req.Description,
                    IsDefault = req.IsDefault,
                    SupplierId = req.SupplierId,
                    Version = req.Version,
                    EffectiveFromUtc = nowUtc,
                    EffectiveToUtc = null,
                    Status = PriceListStatus.Published,
                    CurrencyCode = NormalizeCurrency(req.CurrencyCode),
                    VatPolicy = NormalizeVatPolicy(req.VatPolicy),
                    ContractCode = NormalizeOptional(req.ContractCode),
                    DiscountRate = req.DiscountRate,
                    RebateAmount = req.RebateAmount,
                    FeeAmount = req.FeeAmount,
                    ShippingAmount = req.ShippingAmount,
                    PublishedAtUtc = now,
                    PublishedByUserId = userId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<PriceList>().Add(entity);
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

        public async Task<PriceListResDTO> UpdateAsync(PriceListUpdateReqDTO req, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                var effectiveFromUtc = req.EffectiveFromUtc ?? entity.EffectiveFromUtc;
                ValidatePriceBook(req.SupplierId, req.Version, effectiveFromUtc, null, req.CurrencyCode);
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
                entity.EffectiveToUtc = null;
                if (entity.Status != PriceListStatus.Published)
                {
                    entity.Status = PriceListStatus.Published;
                    entity.PublishedAtUtc = now;
                    entity.PublishedByUserId = userId;
                    entity.ExpiredAtUtc = null;
                    entity.ExpiredByUserId = null;
                }
                entity.CurrencyCode = NormalizeCurrency(req.CurrencyCode);
                entity.VatPolicy = NormalizeVatPolicy(req.VatPolicy);
                entity.ContractCode = NormalizeOptional(req.ContractCode);
                entity.DiscountRate = req.DiscountRate;
                entity.RebateAmount = req.RebateAmount;
                entity.FeeAmount = req.FeeAmount;
                entity.ShippingAmount = req.ShippingAmount;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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

        public async Task<PriceListResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var entity = await _scopedUow.VPPContext.Set<PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                if (isDeleted && entity.IsDefault)
                {
                    throw new BusinessException("Cannot delete the default price list.");
                }

                var now = _dateTimeProvider.Now;
                if (!isDeleted && entity.Status != PriceListStatus.Published)
                {
                    entity.Status = PriceListStatus.Published;
                    entity.EffectiveToUtc = null;
                    entity.PublishedAtUtc = now;
                    entity.PublishedByUserId = userId;
                    entity.ExpiredAtUtc = null;
                    entity.ExpiredByUserId = null;
                }

                entity.IsDeleted = isDeleted;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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
                var entity = await _scopedUow.VPPContext.Set<PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                if (!entity.IsDeleted)
                {
                    throw new BusinessException("The price list must be deactivated before permanent deletion.");
                }

                if (entity.IsDefault)
                {
                    throw new BusinessException("The default price list cannot be hard-deleted.");
                }

                var referenceCount = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .CountAsync(x => x.PriceListId == id)
                    + await _scopedUow.VPPContext.Set<gtas_vpp_be.Model.VPP.VppRequest>()
                        .CountAsync(x => x.SettledByPriceListId == id)
                    + await _scopedUow.VPPContext.Set<gtas_vpp_be.Model.VPP.Settlement>()
                        .CountAsync(x => x.PriceListId == id)
                    + await _scopedUow.VPPContext.Set<gtas_vpp_be.Model.VPP.SettlementItem>()
                        .CountAsync(x => x.PriceListId == id);
                if (referenceCount > 0)
                {
                    throw new BusinessException($"Price list is still referenced by {referenceCount} records.");
                }

                _scopedUow.VPPContext.Set<PriceList>().Remove(entity);
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
                var entity = await _scopedUow.VPPContext.Set<PriceList>()
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                if (entity == null)
                {
                    throw new BusinessException("Price list not found.");
                }

                var now = _dateTimeProvider.Now;
                await DemoteDefaultsAsync(userId, now);
                entity.IsDefault = true;
                entity.UpdatedByUserId = userId;
                entity.UpdatedAtUtc = now;

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

        public async Task<PriceListResDTO> CloneAsync(PriceListCloneReqDTO req, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var source = await _scopedUow.VPPContext.Set<PriceList>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == req.SourceId && !x.IsDeleted);
                if (source == null)
                {
                    throw new BusinessException("Source price list not found.");
                }

                var now = _dateTimeProvider.Now;
                var nowUtc = PeriodCalculator.NormalizeNowUtc(now);
                var clone = new PriceList
                {
                    Id = Guid.NewGuid(),
                    PriceListCode = req.Code,
                    PriceListName = req.Name,
                    Description = req.Description,
                    IsDefault = false,
                    SupplierId = source.SupplierId,
                    // Bản sao là một bảng giá độc lập; Version chỉ còn là field tương thích nội bộ.
                    Version = 1,
                    EffectiveFromUtc = nowUtc,
                    EffectiveToUtc = null,
                    Status = PriceListStatus.Published,
                    CurrencyCode = source.CurrencyCode,
                    VatPolicy = source.VatPolicy,
                    ContractCode = source.ContractCode,
                    DiscountRate = source.DiscountRate,
                    RebateAmount = source.RebateAmount,
                    FeeAmount = source.FeeAmount,
                    ShippingAmount = source.ShippingAmount,
                    PublishedAtUtc = now,
                    PublishedByUserId = userId,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = now,
                    IsDeleted = false
                };

                _scopedUow.VPPContext.Set<PriceList>().Add(clone);

                var sourceRows = await _scopedUow.VPPContext.Set<SupplierProductMapping>()
                    .AsNoTracking()
                    .Where(x => x.PriceListId == source.Id && !x.IsDeleted)
                    .ToListAsync();

                var clonedRows = sourceRows.Select(row => new SupplierProductMapping
                {
                    Id = Guid.NewGuid(),
                    VppItemId = row.VppItemId,
                    SupplierId = row.SupplierId,
                    PriceListId = clone.Id,
                    Price = row.Price,
                    NetPrice = row.NetPrice == 0m && row.Price != 0m ? row.Price : row.NetPrice,
                    VatRate = row.VatRate,
                    MinimumOrderQuantity = row.MinimumOrderQuantity,
                    LeadTimeDays = row.LeadTimeDays,
                    IsDefault = row.IsDefault,
                    Description = row.Description,
                    CreatedByUserId = userId,
                    CreatedAtUtc = now,
                    UpdatedByUserId = userId,
                    UpdatedAtUtc = now,
                    IsDeleted = false
                }).ToList();

                _scopedUow.VPPContext.Set<SupplierProductMapping>().AddRange(clonedRows);
                await _scopedUow.CommitAsync();

                return (await GetByIdAsync(clone.Id))!;
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        private IQueryable<PriceListResDTO> PriceListDtoQuery(bool showDeleted = false)
        {
            var query = _scopedUow.VPPContext.Set<PriceList>()
                .AsNoTracking();

            if (!showDeleted)
            {
                query = query.Where(x => !x.IsDeleted);
            }

            return query.Select(x => new PriceListResDTO
            {
                Id = x.Id,
                Description = x.Description,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedByUserId = x.UpdatedByUserId,
                UpdatedAtUtc = x.UpdatedAtUtc,
                IsDeleted = x.IsDeleted,
                PriceListCode = x.PriceListCode,
                PriceListName = x.PriceListName,
                IsDefault = x.IsDefault,
                SupplierId = x.SupplierId,
                SupplierName = x.Supplier == null ? null : x.Supplier.SupplierName,
                Version = x.Version,
                EffectiveFromUtc = x.EffectiveFromUtc,
                EffectiveToUtc = x.EffectiveToUtc,
                Status = x.Status == PriceListStatus.Draft
                        ? "Draft"
                        : x.Status == PriceListStatus.Published ? "Published" : "Expired",
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
                ItemCount = x.SupplierProductMappings!.Count(m => showDeleted || !m.IsDeleted),
                DataSource = x.ImportBatches!
                    .Where(batch => !batch.IsDeleted
                        && batch.Status == PriceListImportBatchStatus.Completed)
                    .OrderByDescending(batch => batch.CompletedAtUtc)
                    .Select(batch => batch.FileFormat)
                    .FirstOrDefault()
                    ?? (x.PriceListCode == VppPricingDefaults.DefaultPriceListCode
                        ? PriceListDataSources.Default
                        : PriceListDataSources.Manual)
            });
        }

        private static IOrderedQueryable<PriceListResDTO> ApplyDefaultOrder(IQueryable<PriceListResDTO> query)
        {
            return query
                .OrderBy(x => x.IsDeleted)
                .ThenByDescending(x => x.IsDefault)
                .ThenBy(x => x.PriceListName)
                .ThenBy(x => x.PriceListCode);
        }

        private async Task DemoteDefaultsAsync(int userId, DateTime now)
        {
            var rows = await _scopedUow.VPPContext.Set<PriceList>()
                .Where(x => x.IsDefault && !x.IsDeleted)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.IsDefault = false;
                row.UpdatedByUserId = userId;
                row.UpdatedAtUtc = now;
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

            var exists = await _scopedUow.VPPContext.Set<Supplier>()
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
            if (!supplierId.HasValue || supplierId.Value == Guid.Empty)
            {
                throw new BusinessException("A supplier is required for a price list.");
            }

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
