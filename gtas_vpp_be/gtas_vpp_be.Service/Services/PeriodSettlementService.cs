using System.Text.Json;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.View;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class PeriodSettlementService : IPeriodSettlementService
    {
        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPriceBookWorkflowService? _priceBookWorkflowService;

        public PeriodSettlementService(IUnitOfWork scopedUow, IDateTimeProvider dateTimeProvider)
            : this(scopedUow, dateTimeProvider, null)
        {
        }

        public PeriodSettlementService(
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IPriceBookWorkflowService? priceBookWorkflowService)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _priceBookWorkflowService = priceBookWorkflowService;
        }

        public async Task<VPP_SettlementPreviewResDTO> PreviewAsync(
            VPP_SettlementPreviewReqDTO req,
            CancellationToken cancellationToken = default)
        {
            ValidatePeriod(req.Y, req.M);
            if (_priceBookWorkflowService is null)
            {
                throw new BusinessException("Settlement preview is unavailable.");
            }

            var headers = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => x.Y == req.Y && x.M == req.M && !x.IsDeleted
                         && ((x.IsAdditionalOrder
                              && x.IsCurrentRevision
                              && x.Status == (int)VPPStatus.Approved)
                             || (!x.IsAdditionalOrder
                                 && x.IsCurrentRevision
                                 && (x.Status == (int)VPPStatus.Submitted
                                     || x.Status == (int)VPPStatus.Approved))))
                .Include(x => x.VPP02_RequestDetails.Where(detail => !detail.IsDeleted))
                .ToListAsync(cancellationToken);

            var pendingAdditionalCount = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .CountAsync(x => x.Y == req.Y && x.M == req.M && !x.IsDeleted
                              && x.IsAdditionalOrder
                              && x.Status == (int)VPPStatus.Pending, cancellationToken);

            var totals = headers
                .SelectMany(x => x.VPP02_RequestDetails)
                .GroupBy(x => x.VPPId)
                .ToDictionary(group => group.Key, group => group.Sum(detail => (decimal)detail.Qty));

            var asOfUtc = req.PriceAsOfUtc.HasValue
                ? NormalizeUtc(req.PriceAsOfUtc.Value)
                : PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
            var response = new VPP_SettlementPreviewResDTO
            {
                Y = req.Y,
                M = req.M,
                PriceAsOfUtc = asOfUtc,
                RequestedItemCount = totals.Count,
                RequestedLineCount = headers.Sum(x => x.VPP02_RequestDetails.Count),
                PendingAdditionalCount = pendingAdditionalCount
            };

            foreach (var supplement in headers.Where(x => x.IsAdditionalOrder && !x.BaseRequestId.HasValue))
            {
                response.Blockers.Add($"SUPPLEMENT_WITHOUT_BASE:{supplement.Id}");
            }
            if (pendingAdditionalCount > 0)
            {
                response.Blockers.Add($"PENDING_SUPPLEMENTS:{pendingAdditionalCount}");
            }
            if (totals.Count == 0)
            {
                response.Blockers.Add("NO_SUBMITTED_ITEMS");
            }

            foreach (var exception in req.Exceptions ?? [])
            {
                var valid = exception.VppId != Guid.Empty
                             && exception.SupplierId != Guid.Empty
                             && !string.IsNullOrWhiteSpace(exception.Reason)
                             && exception.Reason.Trim().Length is >= 5 and <= 500;
                response.Exceptions.Add(new VPP_SettlementExceptionResDTO
                {
                    VppId = exception.VppId,
                    SupplierId = exception.SupplierId,
                    Reason = exception.Reason?.Trim(),
                    IsValid = valid
                });
                if (!valid)
                {
                    response.Blockers.Add($"INVALID_SUPPLIER_EXCEPTION:{exception.VppId}");
                }
            }

            if (totals.Count > 0)
            {
                var comparison = await _priceBookWorkflowService.CompareAsync(new PriceBookComparisonReqDTO
                {
                    PriceAsOfUtc = asOfUtc,
                    Items = totals.Select(x => new PriceBookComparisonItemReqDTO
                    {
                        VppId = x.Key,
                        Quantity = x.Value
                    }).ToList()
                }, cancellationToken);
                response.Quotes = comparison.Quotes;

                var eligible = response.Quotes.Where(x => x.IsEligible).AsEnumerable();
                if (req.PriceListId.HasValue)
                {
                    eligible = eligible.Where(x => x.PriceListId == req.PriceListId.Value);
                }
                if (req.PrimarySupplierId.HasValue)
                {
                    eligible = eligible.Where(x => x.SupplierId == req.PrimarySupplierId.Value);
                }

                response.PrimaryQuote = eligible.FirstOrDefault();
                if (response.PrimaryQuote is null)
                {
                    response.Blockers.Add(req.PrimarySupplierId.HasValue
                        ? "PRIMARY_SUPPLIER_NOT_COVERED"
                        : req.PriceListId.HasValue ? "PRICE_BOOK_NOT_COVERED" : "NO_COMPLETE_PRICE_COVERAGE");
                }
                else
                {
                    response.PrimarySupplierId = response.PrimaryQuote.SupplierId;
                    response.PrimaryPriceListId = response.PrimaryQuote.PriceListId;
                    response.PrimaryPriceListVersion = response.PrimaryQuote.Version;
                }
            }

            response.InputHash = ComputeInputHash(req, asOfUtc, totals);
            return response;
        }

        public async Task<VPP_PeriodSettlementResDTO> SettleAsync(VPP_SettlePeriodReqDTO req, int userId)
        {
            ValidatePeriod(req.Y, req.M);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var list = await ResolvePriceListAsync(req.PriceListId);
                if (list == null)
                {
                    throw new BusinessException("No price list available.");
                }

                var pendingCount = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .CountAsync(x => x.Y == req.Y && x.M == req.M && !x.IsDeleted
                                  && x.IsAdditionalOrder
                                  && x.Status == (int)VPPStatus.Pending);
                if (pendingCount > 0)
                {
                    throw new ConflictException(
                        $"Cannot settle: {pendingCount} additional order(s) still pending approval.");
                }

                var headers = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Where(x => x.Y == req.Y && x.M == req.M && !x.IsDeleted
                             && (x.Status == (int)VPPStatus.Submitted
                              || x.Status == (int)VPPStatus.Approved))
                    .Include(h => h.VPP02_RequestDetails.Where(d => !d.IsDeleted))
                    .ToListAsync();

                var distinctVppIds = headers
                    .SelectMany(x => x.VPP02_RequestDetails)
                    .Select(x => x.VPPId)
                    .Distinct()
                    .ToArray();

                var priceRows = distinctVppIds.Length == 0
                    ? new List<L06_VPPSupplierMapping>()
                    : await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                        .AsNoTracking()
                        .Where(x => x.L07_PriceListId == list.Id
                                 && !x.IsDeleted
                                 && distinctVppIds.Contains(x.L04_VPPId))
                        .ToListAsync();

                var missing = distinctVppIds
                    .Where(id => !priceRows.Any(row => row.L04_VPPId == id))
                    .ToList();
                if (missing.Count > 0)
                {
                    var missingNames = await _scopedUow.VPPContext.Set<L04_VPP>()
                        .Where(v => missing.Contains(v.Id))
                        .Select(v => v.VPPName)
                        .ToListAsync();

                    throw new BusinessException(
                        $"Price list '{list.PriceListName}' is missing prices for {missing.Count} item(s): "
                        + string.Join(", ", missingNames));
                }

                var priceByVppId = priceRows
                    .GroupBy(x => x.L04_VPPId)
                    .ToDictionary(
                        g => g.Key,
                        g => (long)(g.FirstOrDefault(x => x.IsDefault)?.Price ?? g.First().Price));

                var now = _dateTimeProvider.Now;
                foreach (var header in headers)
                {
                    foreach (var detail in header.VPP02_RequestDetails)
                    {
                        detail.CurrentSinglePrice = priceByVppId[detail.VPPId];
                        detail.UpdateUserId = userId;
                        detail.UpdateDate = now;
                    }

                    header.SettledAt = now;
                    header.SettledByUserId = userId;
                    header.SettledByPriceListId = list.Id;
                    header.UpdateUserId = userId;
                    header.UpdateDate = now;

                    _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                    {
                        Id = Guid.NewGuid(),
                        VPP01_RequestHeaderId = header.Id,
                        LogDate = now,
                        LogTitle = "PERIOD_SETTLED",
                        LogJS = JsonSerializer.Serialize(new
                        {
                            req.Y,
                            req.M,
                            list.Id,
                            list.PriceListName,
                            OrderId = header.Id
                        })
                    });
                }

                await _scopedUow.CommitAsync();
                return await GetStatusAsync(req.Y, req.M);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }

        public async Task<VPP_PeriodSettlementResDTO> GetStatusAsync(int y, int m)
        {
            ValidatePeriod(y, m);

            var baseQuery = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => x.Y == y && x.M == m && !x.IsDeleted);

            var pendingAdditionalCount = await baseQuery
                .CountAsync(x => x.IsAdditionalOrder && x.Status == (int)VPPStatus.Pending);

            var orderCount = await baseQuery
                .CountAsync(x => x.Status == (int)VPPStatus.Submitted
                              || x.Status == (int)VPPStatus.Approved);

            var latest = await baseQuery
                .Where(x => x.SettledAt != null)
                .OrderByDescending(x => x.SettledAt)
                .Select(x => new
                {
                    x.SettledAt,
                    x.SettledByUserId,
                    x.SettledByPriceListId
                })
                .FirstOrDefaultAsync();

            var result = new VPP_PeriodSettlementResDTO
            {
                Y = y,
                M = m,
                IsSettled = latest?.SettledAt != null,
                SettledAt = latest?.SettledAt,
                SettledByUserId = latest?.SettledByUserId,
                PriceListId = latest?.SettledByPriceListId,
                OrderCount = orderCount,
                PendingAdditionalCount = pendingAdditionalCount
            };

            await PopulateNamesAsync(new[] { result });
            return result;
        }

        public async Task<List<VPP_PeriodSettlementResDTO>> ListSettledAsync()
        {
            var settledRows = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.SettledAt != null)
                .Select(x => new
                {
                    x.Y,
                    x.M,
                    x.SettledAt,
                    x.SettledByUserId,
                    x.SettledByPriceListId
                })
                .ToListAsync();

            var result = new List<VPP_PeriodSettlementResDTO>();
            foreach (var group in settledRows.GroupBy(x => new { x.Y, x.M }))
            {
                var latest = group.OrderByDescending(x => x.SettledAt).First();
                var status = await GetStatusAsync(group.Key.Y, group.Key.M);
                status.IsSettled = true;
                status.SettledAt = latest.SettledAt;
                status.SettledByUserId = latest.SettledByUserId;
                status.PriceListId = latest.SettledByPriceListId;
                result.Add(status);
            }

            await PopulateNamesAsync(result);
            return result
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ToList();
        }

        private async Task<L07_PriceList?> ResolvePriceListAsync(Guid? priceListId)
        {
            var query = _scopedUow.VPPContext.Set<L07_PriceList>()
                .Where(x => !x.IsDeleted);

            return priceListId.HasValue
                ? await query.FirstOrDefaultAsync(x => x.Id == priceListId.Value)
                : await query.FirstOrDefaultAsync(x => x.IsDefault);
        }

        private async Task PopulateNamesAsync(IEnumerable<VPP_PeriodSettlementResDTO> rows)
        {
            var rowList = rows.ToList();
            var userIds = rowList
                .Select(x => x.SettledByUserId)
                .Where(x => x.HasValue && x.Value > 0)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();
            var listIds = rowList
                .Select(x => x.PriceListId)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToArray();

            var users = userIds.Length == 0
                ? new Dictionary<int, string?>()
                : await _scopedUow.VPPContext.Set<v_Users>()
                    .AsNoTracking()
                    .Where(x => userIds.Contains(x.UserID))
                    .Select(x => new { x.UserID, x.FullName })
                    .ToDictionaryAsync(x => x.UserID, x => x.FullName);

            var priceLists = listIds.Length == 0
                ? new Dictionary<Guid, string?>()
                : await _scopedUow.VPPContext.Set<L07_PriceList>()
                    .AsNoTracking()
                    .Where(x => listIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.PriceListName })
                    .ToDictionaryAsync(x => x.Id, x => x.PriceListName);

            foreach (var row in rowList)
            {
                if (row.SettledByUserId.HasValue && users.TryGetValue(row.SettledByUserId.Value, out var userName))
                {
                    row.SettledByUserName = userName;
                }

                if (row.PriceListId.HasValue && priceLists.TryGetValue(row.PriceListId.Value, out var priceListName))
                {
                    row.PriceListName = priceListName;
                }
            }
        }

        private static void ValidatePeriod(int y, int m)
        {
            if (y < 1900 || y > 9999)
            {
                throw new BusinessException($"Invalid year {y}.");
            }

            if (m < 1 || m > 12)
            {
                throw new BusinessException($"Invalid month {m}.");
            }
        }

        private static string ComputeInputHash(
            VPP_SettlementPreviewReqDTO req,
            DateTime asOfUtc,
            IReadOnlyDictionary<Guid, decimal> totals)
        {
            var canonical = JsonSerializer.Serialize(new
            {
                req.Y,
                req.M,
                PriceAsOfUtc = asOfUtc,
                req.PriceListId,
                req.PrimarySupplierId,
                Items = totals.OrderBy(x => x.Key).Select(x => new { VppId = x.Key, Quantity = x.Value }),
                Exceptions = (req.Exceptions ?? []).OrderBy(x => x.VppId).ThenBy(x => x.SupplierId)
                    .Select(x => new { x.VppId, x.SupplierId, Reason = x.Reason?.Trim() })
            });
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(canonical)));
        }

        private static DateTime NormalizeUtc(DateTime value)
            => value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
