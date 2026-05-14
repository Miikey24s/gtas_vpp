using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_shared.DTOs.Res.VPP;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Service.Services
{
    public interface IVPPRequestService
    {
        Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses);
        Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersSummaryAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses);
        Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetMyOrdersSummaryPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top);
        Task<VPP01_RequestHeaderResDTO?> GetOrderByIdAsync(Guid id);
        Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode);
        Task<VPP01_RequestHeaderResDTO> UpdateOrderAsync(VPP01_UpdateReqDTO req);
        Task CancelOrderAsync(Guid id, int userId);
        Task<VPP01_RequestHeaderResDTO?> GetPreviousOrderItemsAsync(int userId);
        Task<VPP_PeriodInfoResDTO> GetCurrentPeriodInfoAsync(int userId);
        Task<List<VPP01_RequestHeaderResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode);
        Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetAllOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top);
        Task<List<VPP01_RequestHeaderResDTO>> GetDepartmentOrdersAsync(int? year, int? month, int? status, string? departmentCode);
        Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetDepartmentOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top);
        Task<List<VPP01_RequestHeaderResDTO>> GetPendingAdditionalOrdersAsync();
        Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetPendingAdditionalOrdersPagedAsync(int? skip, int? top);
        Task ApproveAdditionalOrderAsync(Guid id, int adminId);
        Task RejectAdditionalOrderAsync(Guid id, int adminId, string? reason);
    }
    
    public class VPPRequestService : BaseServices, IVPPRequestService
    {
        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly PeriodCalculator _periodCalculator;
        private readonly int _deadlineDay;

        public VPPRequestService(
            IUnitOfWorkFactory uowFactory,
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork scopedUow,
            IDateTimeProvider dateTimeProvider,
            IConfiguration config,
            IEnvironmentResolver environmentResolver,
            IUserNameResolver userNameResolver,
            ILogger<BaseServices> baseLogger,
            IOptions<JiraSettings> jiraSettings,
            PeriodCalculator? periodCalculator = null)
            : base(uowFactory, httpContextAccessor, environmentResolver, userNameResolver, baseLogger, jiraSettings)
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _deadlineDay = config.GetValue<int>("VPPDeadlineDay", 5);
            _periodCalculator = periodCalculator ?? new PeriodCalculator(_deadlineDay);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersSummaryAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
        }

        public async Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetMyOrdersSummaryPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top)
        {
            return await GetFilteredOrdersPagedAsync(userId, years, months, statuses, skip, top);
        }

        private async Task<List<VPP01_RequestHeaderResDTO>> GetFilteredOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            var yearFilter = years?.Distinct().ToArray();
            var monthFilter = months?.Distinct().ToArray();
            var statusFilter = statuses?.Distinct().ToArray();

            var query = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => x.CreateUserId == userId && !x.IsDeleted);

            if (yearFilter is { Length: > 0 }) query = query.Where(x => yearFilter.Contains(x.Y));
            if (monthFilter is { Length: > 0 }) query = query.Where(x => monthFilter.Contains(x.M));
            if (statusFilter is { Length: > 0 }) query = query.Where(x => statusFilter.Contains(x.Status));

            var result = await query
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ThenByDescending(x => x.SubmittedDate ?? x.UpdateDate)
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        private async Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetFilteredOrdersPagedAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses, int? skip, int? top)
        {
            var yearFilter = years?.Distinct().ToArray();
            var monthFilter = months?.Distinct().ToArray();
            var statusFilter = statuses?.Distinct().ToArray();

            var query = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => x.CreateUserId == userId && !x.IsDeleted);

            if (yearFilter is { Length: > 0 }) query = query.Where(x => yearFilter.Contains(x.Y));
            if (monthFilter is { Length: > 0 }) query = query.Where(x => monthFilter.Contains(x.M));
            if (statusFilter is { Length: > 0 }) query = query.Where(x => statusFilter.Contains(x.Status));

            // Aggregate stats in a single DB query
            var stats = await query.Select(x => new
            {
                Lines = x.VPP02_RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.VPP02_RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var totalCount = stats?.TotalCount ?? 0;
            var totalLines = stats?.TotalLines ?? 0;
            var totalQty = stats?.TotalQty ?? 0;

            var orderedQuery = query
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ThenByDescending(x => x.SubmittedDate ?? x.UpdateDate);

            IQueryable<VPP01_RequestHeader> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, totalCount, totalLines, totalQty);
        }

        public async Task<VPP01_RequestHeaderResDTO?> GetOrderByIdAsync(Guid id)
        {
            var data = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (data is not null)
            {
                await ApplyRequesterNamesAsync(new List<VPP01_RequestHeaderResDTO> { data });
                ApplyPeriodFlags(new[] { data });
            }

            return data;
        }

        public async Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode)
        {
            ValidateItems(req.Items);

            // F-09: Reject obviously invalid Y/M from client (e.g. Y=2099) — caller cannot
            // pick an arbitrary period; BE owns the truth via PeriodCalculator.
            ValidateRequestedPeriod(req);

            if (req.IsAdditionalOrder)
            {
                // Additional orders: only for the previous (just closed) period
                var prev = _periodCalculator.Previous(_dateTimeProvider.Now);
                if (req.Y != prev.Year || req.M != prev.Month)
                    throw new BusinessException(
                        $"Additional orders can only be created for period {prev.Year:D4}-{prev.Month:D2}.");

                // Max 3 additional orders total per user per period
                var additionalCount = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .AsNoTracking()
                    .CountAsync(x => x.CreateUserId == createUserId
                                  && x.Y == req.Y && x.M == req.M
                                  && x.IsAdditionalOrder && !x.IsDeleted);
                if (additionalCount >= 3)
                    throw new BusinessException("Maximum 3 additional orders per period.");

                // Cannot create if there's a pending additional order
                var hasPending = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .AsNoTracking()
                    .AnyAsync(x => x.CreateUserId == createUserId
                                && x.Y == req.Y && x.M == req.M
                                && x.IsAdditionalOrder && !x.IsDeleted
                                && x.Status == (int)VPPStatus.Pending);
                if (hasPending)
                    throw new BusinessException("Cannot create additional order while another is pending approval.");
            }
            else
            {
                // Regular orders: must target the current open period
                var current = _periodCalculator.Current(_dateTimeProvider.Now);
                if (req.Y != current.Year || req.M != current.Month)
                    throw new BusinessException(
                        $"Regular orders can only be created for the current period {current.Year:D4}-{current.Month:D2}.");

                // Defence in depth — should be impossible if Y/M validation above passed.
                if (IsDeadlinePassed(req.Y, req.M))
                    throw new BusinessException("Cannot create order for a period that has passed the deadline.");

                // Only 1 regular order per user per period
                var hasExisting = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .AsNoTracking()
                    .AnyAsync(x => x.CreateUserId == createUserId
                                && x.Y == req.Y && x.M == req.M
                                && !x.IsAdditionalOrder && !x.IsDeleted);
                if (hasExisting)
                    throw new InvalidOperationException("You already have an order for this period.");
            }

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var now = _dateTimeProvider.Now;
                var header = new VPP01_RequestHeader
                {
                    Id = Guid.NewGuid(),
                    Y = req.Y,
                    M = req.M,
                    VPPCode = GenerateVPPCode(req.Y, req.M, createUserId),
                    Status = req.IsAdditionalOrder ? (int)VPPStatus.Pending : (int)VPPStatus.Submitted,
                    Description = req.Description,
                    IsAdditionalOrder = req.IsAdditionalOrder,
                    DepartmentCode = departmentCode,
                    MemberCompanyCode = memberCompanyCode,
                    CreateUserId = createUserId,
                    CreateDate = now,
                    UpdateUserId = createUserId,
                    UpdateDate = now,
                    SubmittedDate = now
                };

                await ValidateActiveProductsAsync(req.Items);
                header.VPP02_RequestDetails = await BuildRequestDetailsAsync(req.Items, createUserId, header.Id, now);

                _scopedUow.VPPContext.Set<VPP01_RequestHeader>().Add(header);
                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "CREATE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails))
                });
                await _scopedUow.CommitAsync();

                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await _scopedUow.RollbackAsync();
                throw new ConflictException("Bạn đã có đơn cho kỳ này.", ex);
            }
            catch
            {
                await _scopedUow.RollbackAsync();
                throw;
            }
        }
        
        public async Task<VPP01_RequestHeaderResDTO> UpdateOrderAsync(VPP01_UpdateReqDTO req)
        {
            ValidateItems(req.Items);

            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != req.UpdateUserId) throw new UnauthorizedAccessException("Cannot update another user's order.");
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Y, header.M)) 
                    throw new InvalidOperationException("Deadline has passed.");

                var now = _dateTimeProvider.Now;

                header.Status = TransitionStatus(header.Status, OrderAction.Update, "Cannot edit order in this status.");
                header.Description = req.Description;
                header.UpdateUserId = req.UpdateUserId;
                header.UpdateDate = now;
                header.SubmittedDate ??= now;

                await _scopedUow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && !x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateUserId, req.UpdateUserId)
                        .SetProperty(x => x.UpdateDate, now));

                await ValidateActiveProductsAsync(req.Items);
                var newDetails = await BuildRequestDetailsAsync(req.Items, req.UpdateUserId, header.Id, now);

                _scopedUow.VPPContext.Set<VPP02_RequestDetail>().AddRange(newDetails);
                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "UPDATE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, newDetails))
                });

                await _scopedUow.CommitAsync();
                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task CancelOrderAsync(Guid id, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot cancel another user's order.");

                // Regular orders: check period still open
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Y, header.M))
                    throw new InvalidOperationException("Cannot cancel order for a closed period.");

                var now = _dateTimeProvider.Now;

                header.IsDeleted = true;
                header.Status = TransitionStatus(header.Status, OrderAction.Cancel, "Only Submitted or Pending orders can be cancelled.");
                header.UpdateUserId = userId;
                header.UpdateDate = now;

                await _scopedUow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && !x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateUserId, userId)
                        .SetProperty(x => x.UpdateDate, now));

                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "CANCEL",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()))
                });

                await _scopedUow.CommitAsync();
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task<VPP01_RequestHeaderResDTO?> GetPreviousOrderItemsAsync(int userId)
        {
            var (_, _, prevYear, prevMonth) = GetCurrentAndPreviousPeriod();

            var prevHeader = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Include(x => x.VPP02_RequestDetails.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(x => x.CreateUserId == userId && x.Y == prevYear && x.M == prevMonth && !x.IsDeleted && !x.IsAdditionalOrder);

            if (prevHeader == null) return null;

            // Filter out items whose product has been soft-deleted
            var activeVppIds = await _scopedUow.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync();

            var result = prevHeader.Adapt<VPP01_RequestHeaderResDTO>();
            result.Items = result.Items.Where(i => activeVppIds.Contains(i.VPPId)).ToList();
            ApplyPeriodFlags(new[] { result });
            return result;
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode)
        {
            var result = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode))
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetAllOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top)
        {
            var query = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode));

            var stats = await query.Select(x => new
            {
                Lines = x.VPP02_RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.VPP02_RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var totalCount = stats?.TotalCount ?? 0;
            var totalLines = stats?.TotalLines ?? 0;
            var totalQty = stats?.TotalQty ?? 0;

            var orderedQuery = query
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ThenByDescending(x => x.UpdateDate);

            IQueryable<VPP01_RequestHeader> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, totalCount, totalLines, totalQty);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetDepartmentOrdersAsync(int? year, int? month, int? status, string? departmentCode)
        {
            var result = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode))
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetDepartmentOrdersPagedAsync(int? year, int? month, int? status, string? departmentCode, int? skip, int? top)
        {
            var query = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode));

            // Aggregate stats in a single DB query
            var stats = await query.Select(x => new
            {
                Lines = x.VPP02_RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.VPP02_RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var totalCount = stats?.TotalCount ?? 0;
            var totalLines = stats?.TotalLines ?? 0;
            var totalQty = stats?.TotalQty ?? 0;

            var orderedQuery = query
                .OrderByDescending(x => x.Y)
                .ThenByDescending(x => x.M)
                .ThenByDescending(x => x.UpdateDate);

            IQueryable<VPP01_RequestHeader> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, totalCount, totalLines, totalQty);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetPendingAdditionalOrdersAsync()
        {
            var result = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsAdditionalOrder && x.Status == (int)VPPStatus.Pending)
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return result;
        }

        public async Task<(List<VPP01_RequestHeaderResDTO> Data, int TotalCount, int TotalLines, int TotalQty)> GetPendingAdditionalOrdersPagedAsync(int? skip, int? top)
        {
            var query = _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsAdditionalOrder && x.Status == (int)VPPStatus.Pending);

            var stats = await query.Select(x => new
            {
                Lines = x.VPP02_RequestDetails.Count(d => !d.IsDeleted),
                Qty = x.VPP02_RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0
            }).GroupBy(x => 1).Select(g => new
            {
                TotalCount = g.Count(),
                TotalLines = g.Sum(x => x.Lines),
                TotalQty = g.Sum(x => x.Qty)
            }).FirstOrDefaultAsync();

            var orderedQuery = query.OrderByDescending(x => x.UpdateDate);

            IQueryable<VPP01_RequestHeader> pagedQuery = orderedQuery;
            if (skip.HasValue && skip.Value > 0) pagedQuery = pagedQuery.Skip(skip.Value);
            if (top.HasValue && top.Value > 0) pagedQuery = pagedQuery.Take(top.Value);

            var result = await pagedQuery
                .ProjectToType<VPP01_RequestHeaderResDTO>()
                .AsSplitQuery()
                .ToListAsync();

            await ApplyRequesterNamesAsync(result);
            ApplyPeriodFlags(result);
            return (result, stats?.TotalCount ?? 0, stats?.TotalLines ?? 0, stats?.TotalQty ?? 0);
        }

        public async Task ApproveAdditionalOrderAsync(Guid id, int adminId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsAdditionalOrder);

                if (header == null) throw new KeyNotFoundException("Order not found or not an additional order.");

                var now = _dateTimeProvider.Now;
                header.Status = TransitionStatus(header.Status, OrderAction.Approve, "Order must be in Pending status.");
                header.ApprovedById = adminId;
                header.ApprovedAt = now;
                header.RejectReason = null;
                header.UpdateUserId = adminId;
                header.UpdateDate = now;

                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "APPROVE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()))
                });

                await _scopedUow.CommitAsync();
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task RejectAdditionalOrderAsync(Guid id, int adminId, string? reason)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsAdditionalOrder);

                if (header == null) throw new KeyNotFoundException("Order not found or not an additional order.");

                var now = _dateTimeProvider.Now;
                header.Status = TransitionStatus(header.Status, OrderAction.Reject, "Order must be in Pending status.");
                header.RejectedById = adminId;
                header.RejectedAt = now;
                header.RejectReason = reason;
                header.UpdateUserId = adminId;
                header.UpdateDate = now;

                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "REJECT",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()))
                });

                await _scopedUow.CommitAsync();
            }
            catch
            {
                _scopedUow.Rollback();
                throw;
            }
        }

        public async Task<VPP_PeriodInfoResDTO> GetCurrentPeriodInfoAsync(int userId)
        {
            var (curYear, curMonth, prevYear, prevMonth) = GetCurrentAndPreviousPeriod();

            var hasCurrentOrder = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .AnyAsync(x => x.CreateUserId == userId && x.Y == curYear && x.M == curMonth && !x.IsAdditionalOrder && !x.IsDeleted);

            var additionalCount = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .CountAsync(x => x.CreateUserId == userId && x.Y == prevYear && x.M == prevMonth && x.IsAdditionalOrder && !x.IsDeleted);

            var hasPendingAdditional = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .AnyAsync(x => x.CreateUserId == userId && x.Y == prevYear && x.M == prevMonth && x.IsAdditionalOrder && !x.IsDeleted && x.Status == (int)VPPStatus.Pending);

            var hasPreviousOrder = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                .AsNoTracking()
                .AnyAsync(x => x.CreateUserId == userId && x.Y == prevYear && x.M == prevMonth && !x.IsAdditionalOrder && !x.IsDeleted);

            var deadlinePassed = IsDeadlinePassed(curYear, curMonth);

            return new VPP_PeriodInfoResDTO
            {
                CurrentPeriodYear = curYear,
                CurrentPeriodMonth = curMonth,
                PreviousPeriodYear = prevYear,
                PreviousPeriodMonth = prevMonth,
                DeadlineDate = _periodCalculator.DeadlineFor(new Period(curYear, curMonth)),
                IsDeadlinePassed = deadlinePassed,
                HasCurrentPeriodOrder = hasCurrentOrder,
                AdditionalOrderCount = additionalCount,
                MaxAdditionalOrders = 3,
                HasPendingAdditional = hasPendingAdditional,
                CanCreateOrder = !deadlinePassed && !hasCurrentOrder,
                CanCreateAdditional = additionalCount < 3 && !hasPendingAdditional,
                HasPreviousOrder = hasPreviousOrder,
                CanCopyPrevious = !deadlinePassed && !hasCurrentOrder && hasPreviousOrder
            };
        }

        // ─── Private helpers ───────────────────────────────────────────────

        private bool IsDeadlinePassed(int year, int month)
            => _periodCalculator.IsDeadlinePassed(_dateTimeProvider.Now, new Period(year, month));

        private static int TransitionStatus(int currentStatus, OrderAction action, string invalidMessage)
        {
            try
            {
                return OrderStateMachine.Transition(currentStatus, action);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(invalidMessage, ex);
            }
        }

        private string GenerateVPPCode(int year, int month, int userId)
            => $"VPP-{year:D4}{month:D2}-{Guid.NewGuid():N}".Substring(0, 24);

        private (int curYear, int curMonth, int prevYear, int prevMonth) GetCurrentAndPreviousPeriod()
        {
            var now = _dateTimeProvider.Now;
            var current = _periodCalculator.Current(now);
            var previous = _periodCalculator.Previous(now);
            return (current.Year, current.Month, previous.Year, previous.Month);
        }

        /// <summary>
        /// Reject obviously invalid Y/M from the client (F-09). Order Y/M must fall
        /// inside [current period - 12 months, current period + 1 month] — anything
        /// outside is a malformed request.
        /// </summary>
        private void ValidateRequestedPeriod(VPP01_CreateReqDTO req)
        {
            if (req.Y < 1900 || req.Y > 9999)
                throw new BusinessException($"Invalid year {req.Y}.");
            if (req.M < 1 || req.M > 12)
                throw new BusinessException($"Invalid month {req.M}.");

            var requested = new Period(req.Y, req.M);
            var current = _periodCalculator.Current(_dateTimeProvider.Now);
            var monthsDiff = ((requested.Year - current.Year) * 12) + (requested.Month - current.Month);
            if (monthsDiff < -12 || monthsDiff > 1)
            {
                throw new BusinessException(
                    $"Period {requested} is too far from the current period {current}.");
            }
        }

        /// <summary>
        /// Validates that all requested product IDs are active (not soft-deleted)
        /// and their categories are also active. Prevents orders with deleted products.
        /// </summary>
        private async Task ValidateActiveProductsAsync(IEnumerable<VPP02_ItemReqDTO> items)
        {
            var requestedIds = items.Select(x => x.VPPId).Distinct().ToArray();
            if (requestedIds.Length == 0) return;

            var activeProducts = await _scopedUow.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => requestedIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.VPPName,
                    CategoryDeleted = x.VPPCategory != null && x.VPPCategory.IsDeleted
                })
                .ToListAsync();

            // Check for missing/deleted products
            var activeIds = activeProducts.Select(x => x.Id).ToHashSet();
            var missingIds = requestedIds.Where(id => !activeIds.Contains(id)).ToArray();
            if (missingIds.Length > 0)
                throw new BusinessException(
                    $"The following products have been disabled and cannot be added to the order. Please remove them and try again.");

            // Check for products whose category has been deleted
            var categoryDeletedItems = activeProducts.Where(x => x.CategoryDeleted).ToArray();
            if (categoryDeletedItems.Length > 0)
            {
                var names = string.Join(", ", categoryDeletedItems.Select(x => x.VPPName));
                throw new BusinessException(
                    $"The category for the following products has been disabled: {names}. Please remove them and try again.");
            }
        }

        private async Task<List<VPP02_RequestDetail>> BuildRequestDetailsAsync(IEnumerable<VPP02_ItemReqDTO> items, int userId, Guid headerId, DateTime now)
        {
            var requestedItems = items.ToList();
            var currentPrices = await GetCurrentSinglePricesAsync(requestedItems.Select(x => x.VPPId));

            return requestedItems.Select(item => new VPP02_RequestDetail
            {
                Id = Guid.NewGuid(),
                VPPId = item.VPPId,
                Qty = item.Qty,
                CurrentSinglePrice = currentPrices.TryGetValue(item.VPPId, out var price) ? price : 0,
                Description = item.Description,
                VPP01_RequestHeaderId = headerId,
                CreateUserId = userId,
                CreateDate = now,
                UpdateUserId = userId,
                UpdateDate = now
            }).ToList();
        }

        private async Task<Dictionary<Guid, long>> GetCurrentSinglePricesAsync(IEnumerable<Guid> vppIds)
        {
            var distinctVppIds = vppIds.Distinct().ToArray();
            if (distinctVppIds.Length == 0)
            {
                return new Dictionary<Guid, long>();
            }

            var priceRows = await _scopedUow.VPPContext.Set<L06_VPPSupplierMapping>()
                .AsNoTracking()
                .Where(x => distinctVppIds.Contains(x.L04_VPPId) && !x.IsDeleted)
                .Select(x => new { VPPId = x.L04_VPPId, x.Price })
                .ToListAsync();

            return priceRows
                .GroupBy(x => x.VPPId)
                .ToDictionary(g => g.Key, g => (long)g.Select(x => x.Price).FirstOrDefault());
        }

        /// <summary>
        /// P1: Materialize period-derived flags on response DTOs so FE never recomputes
        /// them with its own clock (was F-02 / F-33 root cause). Pure in-memory pass.
        /// </summary>
        private void ApplyPeriodFlags(IEnumerable<VPP01_RequestHeaderResDTO> orders)
        {
            var now = _dateTimeProvider.Now;
            foreach (var order in orders)
            {
                var deadlinePassed = _periodCalculator.IsDeadlinePassed(
                    now, new Period(order.Y, order.M));
                order.IsDeadlinePassed = deadlinePassed;
                order.CanEdit = order.IsAdditionalOrder
                    ? order.Status == (int)VPPStatus.Pending
                    : (order.Status == (int)VPPStatus.Submitted && !deadlinePassed);
                order.CanCancel = order.CanEdit;
            }
        }

        private async Task ApplyRequesterNamesAsync(List<VPP01_RequestHeaderResDTO> orders)
        {
            var userIds = orders
                .Select(x => x.CreateUserId)
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (userIds.Length == 0)
            {
                return;
            }

            var users = await _scopedUow.VPPContext.Set<v_Users>()
                .AsNoTracking()
                .Where(x => userIds.Contains(x.UserID))
                .Select(x => new { x.UserID, x.FullName })
                .ToDictionaryAsync(x => x.UserID, x => x.FullName);

            foreach (var order in orders)
            {
                order.RequesterName = users.TryGetValue(order.CreateUserId, out var fullName)
                    ? fullName
                    : null;
            }
        }

        private static object BuildLogPayload(VPP01_RequestHeader header, IEnumerable<VPP02_RequestDetail> details)
            => new
            {
                header.Id,
                header.VPPCode,
                header.Y,
                header.M,
                header.Status,
                header.Description,
                header.DepartmentCode,
                header.MemberCompanyCode,
                header.CreateUserId,
                header.CreateDate,
                header.UpdateUserId,
                header.UpdateDate,
                header.SubmittedDate,
                Detail = details.Select(x => new
                {
                    x.Id,
                    x.VPPId,
                    x.Qty,
                    x.CurrentSinglePrice,
                    x.Description,
                    x.CreateUserId,
                    x.CreateDate,
                    x.UpdateUserId,
                    x.UpdateDate,
                    x.IsDeleted,
                    x.VPP01_RequestHeaderId
                }).ToList()
            };

        private static bool IsUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqlException sqlException
               && (sqlException.Number == 2601 || sqlException.Number == 2627);

        private static void ValidateItems(List<VPP02_ItemReqDTO>? items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Order must contain at least one item.");
            if (items.Any(i => i.Qty <= 0))
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            if (items.GroupBy(i => i.VPPId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate product in order items is not allowed.");
        }
    }
}
