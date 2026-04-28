using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res;
using gtas_vpp_shared.DTOs.Res.VPP;
using Mapster;
using Microsoft.AspNetCore.Http;
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
        Task<VPP01_RequestHeaderResDTO?> GetOrderByIdAsync(Guid id);
        Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode);
        Task<VPP01_RequestHeaderResDTO> UpdateOrderAsync(VPP01_UpdateReqDTO req);
        Task CancelOrderAsync(Guid id, int userId);
        Task UndoCancelAsync(Guid id, int userId);
        Task<VPP01_RequestHeaderResDTO?> GetPreviousOrderItemsAsync(int userId);
        Task<VPP_PeriodInfoResDTO> GetCurrentPeriodInfoAsync(int userId);
        Task<List<VPP01_RequestHeaderResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode);
        Task<List<VPP01_RequestHeaderResDTO>> GetDepartmentOrdersAsync(int? year, int? month, int? status, string? departmentCode);
        Task<List<VPP01_RequestHeaderResDTO>> GetPendingAdditionalOrdersAsync();
        Task ApproveAdditionalOrderAsync(Guid id, int adminId);
        Task RejectAdditionalOrderAsync(Guid id, int adminId, string? reason);
    }
    
    public class VPPRequestService : BaseServices, IVPPRequestService
    {
        private readonly IUnitOfWork _scopedUow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly int _deadlineDay;

        public VPPRequestService(IUnitOfWorkFactory uowFactory, IHttpContextAccessor httpContextAccessor, IUnitOfWork scopedUow, IDateTimeProvider dateTimeProvider, IConfiguration config, IEnvironmentResolver environmentResolver, IUserNameResolver userNameResolver, ILogger<BaseServices> baseLogger, IOptions<JiraSettings> jiraSettings)
            : base(uowFactory, httpContextAccessor, environmentResolver, userNameResolver, baseLogger, jiraSettings) 
        {
            _scopedUow = scopedUow;
            _dateTimeProvider = dateTimeProvider;
            _deadlineDay = config.GetValue<int>("VPPDeadlineDay", 5);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersSummaryAsync(int userId, IEnumerable<int>? years, IEnumerable<int>? months, IEnumerable<int>? statuses)
        {
            return await GetFilteredOrdersAsync(userId, years, months, statuses);
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
            return result;
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
            }

            return data;
        }

        public async Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode)
        {
            ValidateItems(req.Items);

            if (req.IsAdditionalOrder)
            {
                // Additional orders: only for the previous (just closed) period
                var (_, _, prevYear, prevMonth) = GetCurrentAndPreviousPeriod();
                if (req.Y != prevYear || req.M != prevMonth)
                    throw new InvalidOperationException("Additional orders can only be created for the previous (just closed) period.");

                // Max 3 additional orders total per user per period
                var additionalCount = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .AsNoTracking()
                    .CountAsync(x => x.CreateUserId == createUserId
                                  && x.Y == req.Y && x.M == req.M
                                  && x.IsAdditionalOrder && !x.IsDeleted);
                if (additionalCount >= 3)
                    throw new InvalidOperationException("Maximum 3 additional orders per period.");

                // Cannot create if there's a pending additional order
                var hasPending = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .AsNoTracking()
                    .AnyAsync(x => x.CreateUserId == createUserId
                                && x.Y == req.Y && x.M == req.M
                                && x.IsAdditionalOrder && !x.IsDeleted
                                && x.Status == (int)VPPStatus.Pending);
                if (hasPending)
                    throw new InvalidOperationException("Cannot create additional order while another is pending approval.");
            }
            else
            {
                // Regular orders: check deadline
                if (IsDeadlinePassed(req.Y, req.M))
                    throw new InvalidOperationException("Cannot create order for a period that has passed the deadline.");

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

                header.VPP02_RequestDetails = req.Items.Select(i => new VPP02_RequestDetail
                {
                    Id = Guid.NewGuid(),
                    VPPId = i.VPPId,
                    Qty = i.Qty,
                    CurrentSinglePrice = 0,
                    Description = i.Description,
                    VPP01_RequestHeaderId = header.Id,
                    CreateUserId = createUserId,
                    CreateDate = now,
                    UpdateUserId = createUserId,
                    UpdateDate = now
                }).ToList();

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
            catch
            {
                _scopedUow.Rollback();
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
                if (header.Status is (int)VPPStatus.Cancelled or (int)VPPStatus.Approved or (int)VPPStatus.Rejected)
                    throw new InvalidOperationException("Cannot edit order in this status.");
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Y, header.M)) 
                    throw new InvalidOperationException("Deadline has passed.");

                var now = _dateTimeProvider.Now;

                header.Status = header.IsAdditionalOrder ? (int)VPPStatus.Pending : (int)VPPStatus.Submitted;
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

                var newDetails = req.Items.Select(i => new VPP02_RequestDetail
                {
                    Id = Guid.NewGuid(),
                    VPPId = i.VPPId,
                    Qty = i.Qty,
                    CurrentSinglePrice = 0,
                    Description = i.Description,
                    VPP01_RequestHeaderId = header.Id,
                    CreateUserId = req.UpdateUserId,
                    CreateDate = now,
                    UpdateUserId = req.UpdateUserId,
                    UpdateDate = now
                }).ToList();

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

        public async Task UndoCancelAsync(Guid id, int userId)
        {
            await _scopedUow.BeginTransactionAsync();
            try
            {
                var header = await _scopedUow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Cancelled order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot undo cancel of another user's order.");
                
                // Cannot undo if period is closed (for regular orders)
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Y, header.M))
                    throw new InvalidOperationException("Cannot undo cancel for a closed period.");

                var now = _dateTimeProvider.Now;

                // Restore to original status
                header.IsDeleted = false;
                header.Status = header.IsAdditionalOrder ? (int)VPPStatus.Pending : (int)VPPStatus.Submitted;
                header.UpdateUserId = userId;
                header.UpdateDate = now;

                await _scopedUow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, false)
                        .SetProperty(x => x.UpdateUserId, userId)
                        .SetProperty(x => x.UpdateDate, now));

                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "UNDO_CANCEL",
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
                
                // Only Submitted or Pending orders can be cancelled
                if (header.Status is not ((int)VPPStatus.Submitted or (int)VPPStatus.Pending))
                    throw new InvalidOperationException("Only Submitted or Pending orders can be cancelled.");
                
                // Regular orders: check period still open
                if (!header.IsAdditionalOrder && IsDeadlinePassed(header.Y, header.M))
                    throw new InvalidOperationException("Cannot cancel order for a closed period.");

                var now = _dateTimeProvider.Now;

                header.IsDeleted = true;
                header.Status = (int)VPPStatus.Cancelled;
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
            return result;
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
            return result;
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
            return result;
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
                if (header.Status != (int)VPPStatus.Pending) throw new InvalidOperationException("Order must be in Pending status.");

                var now = _dateTimeProvider.Now;
                header.Status = (int)VPPStatus.Approved;
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
                if (header.Status != (int)VPPStatus.Pending) throw new InvalidOperationException("Order must be in Pending status.");

                var now = _dateTimeProvider.Now;
                header.Status = (int)VPPStatus.Rejected;
                header.UpdateUserId = adminId;
                header.UpdateDate = now;

                _scopedUow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "REJECT",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(new { Reason = reason, Payload = BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()) })
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
                DeadlineDate = new DateTime(curYear, curMonth, _deadlineDay),
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
            => _dateTimeProvider.Now >= new DateTime(year, month, _deadlineDay);

        private string GenerateVPPCode(int year, int month, int userId)
            => $"VPP-{year}{month:D2}-{userId}-{_dateTimeProvider.Now:mmss}";

        private (int curYear, int curMonth, int prevYear, int prevMonth) GetCurrentAndPreviousPeriod()
        {
            var now = _dateTimeProvider.Now;
            var currentMonth = new DateTime(now.Year, now.Month, 1);
            var currentPeriod = now.Day >= _deadlineDay ? currentMonth.AddMonths(1) : currentMonth;
            var previousPeriod = currentPeriod.AddMonths(-1);
            return (currentPeriod.Year, currentPeriod.Month, previousPeriod.Year, previousPeriod.Month);
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
