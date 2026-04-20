using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Service.Services
{
    public interface IVPPRequestService
    {
        Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersAsync(int userId, int? year, int? month, int? status);
        Task<VPP01_RequestHeaderResDTO?> GetOrderByIdAsync(Guid id);
        Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode);
        Task<VPP01_RequestHeaderResDTO> UpdateOrderAsync(VPP01_UpdateReqDTO req);
        Task SubmitOrderAsync(Guid id, int userId);
        Task CancelOrderAsync(Guid id, int userId);
        Task DeleteDraftAsync(Guid id, int userId);
        Task UndoDeleteAsync(Guid id, int userId);
        Task<VPP01_RequestHeaderResDTO> CopyPreviousMonthAsync(int userId, int year, int month, string departmentCode, string memberCompanyCode);
        Task<List<VPP01_RequestHeaderResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode);
    }
    public class VPPRequestService : BaseServices, IVPPRequestService
    {
        public VPPRequestService(IUnitOfWorkFactory uow, IHttpContextAccessor httpContextAccessor)
            : base(uow, httpContextAccessor) { }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetMyOrdersAsync(int userId, int? year, int? month, int? status)
        {
            var data = await ReadAsync<VPP01_RequestHeader>(
                nameof(ContextType.VPPContext),
                true,
                x => x.CreateUserId == userId
                     && !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status),
                q => q.Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.UOM)
                       .Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.VPPCategory));

            return (data ?? new()).Select(MapToResDTO).ToList();
        }

        public async Task<VPP01_RequestHeaderResDTO?> GetOrderByIdAsync(Guid id)
        {
            var data = await ReadAsync<VPP01_RequestHeader>(
                nameof(ContextType.VPPContext),
                true,
                x => x.Id == id && !x.IsDeleted,
                q => q.Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.UOM)
                       .Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.VPPCategory));

            var entity = data?.FirstOrDefault();
            return entity == null ? null : MapToResDTO(entity);
        }

        public async Task<VPP01_RequestHeaderResDTO> CreateOrderAsync(VPP01_CreateReqDTO req, int createUserId, string departmentCode, string memberCompanyCode)
        {
            ValidateItems(req.Items);
            if (IsDeadlinePassed(req.Y, req.M))
            {
                throw new InvalidOperationException("Cannot create order for a period that has passed the deadline.");
            }

            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                var header = new VPP01_RequestHeader
                {
                    Id = Guid.NewGuid(),
                    Y = req.Y,
                    M = req.M,
                    VPPCode = GenerateVPPCode(req.Y, req.M, createUserId),
                    Status = (int)VPPStatus.Submitted,
                    Description = req.Description,
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

                uow.VPPContext.Set<VPP01_RequestHeader>().Add(header);
                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "CREATE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails))
                });
                await uow.CommitAsync();

                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task<VPP01_RequestHeaderResDTO> UpdateOrderAsync(VPP01_UpdateReqDTO req)
        {
            ValidateItems(req.Items);

            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = await uow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != req.UpdateUserId) throw new UnauthorizedAccessException("Cannot update another user's order.");
                if (header.Status is (int)VPPStatus.Cancelled or (int)VPPStatus.Closed)
                    throw new InvalidOperationException("Only Submitted orders can be edited.");
                if (IsDeadlinePassed(header.Y, header.M)) throw new InvalidOperationException("Deadline has passed.");

                var now = DateTime.Now;

                header.Status = (int)VPPStatus.Submitted;
                header.Description = req.Description;
                header.UpdateUserId = req.UpdateUserId;
                header.UpdateDate = now;
                header.SubmittedDate ??= now;

                await uow.VPPContext.Set<VPP02_RequestDetail>()
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

                uow.VPPContext.Set<VPP02_RequestDetail>().AddRange(newDetails);
                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "UPDATE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, newDetails))
                });

                await uow.CommitAsync();
                return (await GetOrderByIdAsync(header.Id))!;
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task SubmitOrderAsync(Guid id, int userId)
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = await uow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot submit another user's order.");
                if (header.Status == (int)VPPStatus.Submitted) throw new InvalidOperationException("Order has already been submitted.");
                if (IsDeadlinePassed(header.Y, header.M))
                    throw new InvalidOperationException("Deadline has passed.");

                header.Status = (int)VPPStatus.Submitted;
                header.SubmittedDate = DateTime.Now;
                header.UpdateUserId = userId;
                header.UpdateDate = DateTime.Now;

                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "SUBMIT",
                    LogDate = DateTime.Now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()))
                });

                await uow.CommitAsync();
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task UndoDeleteAsync(Guid id, int userId)
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = await uow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Deleted order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot undo delete of another user's order.");

                var now = DateTime.Now;

                header.IsDeleted = false;
                header.UpdateUserId = userId;
                header.UpdateDate = now;

                await uow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, false)
                        .SetProperty(x => x.UpdateUserId, userId)
                        .SetProperty(x => x.UpdateDate, now));

                var restoredDetails = (header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>())
                    .Select(x => new VPP02_RequestDetail
                    {
                        Id = x.Id,
                        VPPId = x.VPPId,
                        Qty = x.Qty,
                        CurrentSinglePrice = x.CurrentSinglePrice,
                        Description = x.Description,
                        VPP01_RequestHeaderId = x.VPP01_RequestHeaderId,
                        CreateUserId = x.CreateUserId,
                        CreateDate = x.CreateDate,
                        UpdateUserId = userId,
                        UpdateDate = now,
                        IsDeleted = false
                    })
                    .ToList();

                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "UNDO_DELETE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, restoredDetails))
                });

                await uow.CommitAsync();
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task CancelOrderAsync(Guid id, int userId)
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = await uow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot cancel another user's order.");
                if (header.Status == (int)VPPStatus.Closed)
                    throw new InvalidOperationException("Closed orders cannot be cancelled.");

                header.Status = (int)VPPStatus.Cancelled;
                header.UpdateUserId = userId;
                header.UpdateDate = DateTime.Now;

                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "CANCEL",
                    LogDate = DateTime.Now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>()))
                });

                await uow.CommitAsync();
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task DeleteDraftAsync(Guid id, int userId)
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = await uow.VPPContext.Set<VPP01_RequestHeader>()
                    .Include(x => x.VPP02_RequestDetails)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.Status is not ((int)VPPStatus.Submitted))
                    throw new InvalidOperationException("Only Submitted orders can be deleted.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot delete another user's order.");

                var now = DateTime.Now;

                header.IsDeleted = true;
                header.UpdateUserId = userId;
                header.UpdateDate = now;

                await uow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && !x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateUserId, userId)
                        .SetProperty(x => x.UpdateDate, now));

                var deletedDetails = (header.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>())
                    .Select(x => new VPP02_RequestDetail
                    {
                        Id = x.Id,
                        VPPId = x.VPPId,
                        Qty = x.Qty,
                        CurrentSinglePrice = x.CurrentSinglePrice,
                        Description = x.Description,
                        VPP01_RequestHeaderId = x.VPP01_RequestHeaderId,
                        CreateUserId = x.CreateUserId,
                        CreateDate = x.CreateDate,
                        UpdateUserId = userId,
                        UpdateDate = now,
                        IsDeleted = true
                    })
                    .ToList();

                uow.VPPContext.Set<VPP03_Log>().Add(new VPP03_Log
                {
                    Id = Guid.NewGuid(),
                    VPP01_RequestHeaderId = header.Id,
                    LogTitle = "DELETE",
                    LogDate = now,
                    LogJS = JsonSerializer.Serialize(BuildLogPayload(header, deletedDetails))
                });

                await uow.CommitAsync();
            }
            catch
            {
                uow.Rollback();
                throw;
            }
        }

        public async Task<VPP01_RequestHeaderResDTO> CopyPreviousMonthAsync(int userId, int year, int month, string departmentCode, string memberCompanyCode)
        {
            // Find previous month
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            var prev = await ReadAsync<VPP01_RequestHeader>(
                nameof(ContextType.VPPContext),
                true,
                x => x.CreateUserId == userId && x.Y == prevYear && x.M == prevMonth && !x.IsDeleted,
                q => q.Include(x => x.VPP02_RequestDetails));

            var prevHeader = prev?.FirstOrDefault();
            if (prevHeader == null) throw new KeyNotFoundException("No order found for previous month.");

            var req = new VPP01_CreateReqDTO
            {
                Y = year,
                M = month,
                Description = prevHeader.Description,
                Items = prevHeader.VPP02_RequestDetails.Select(d => new VPP02_ItemReqDTO
                {
                    VPPId = d.VPPId,
                    Qty = d.Qty,
                    Description = d.Description
                }).ToList()
            };

            return await CreateOrderAsync(req, userId, departmentCode, memberCompanyCode);
        }

        public async Task<List<VPP01_RequestHeaderResDTO>> GetAllOrdersAsync(int? year, int? month, int? status, string? departmentCode)
        {
            var data = await ReadAsync<VPP01_RequestHeader>(
                nameof(ContextType.VPPContext),
                true,
                x => !x.IsDeleted
                     && (year == null || x.Y == year)
                     && (month == null || x.M == month)
                     && (status == null || x.Status == status)
                     && (departmentCode == null || x.DepartmentCode == departmentCode),
                q => q.Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.UOM)
                       .Include(x => x.VPP02_RequestDetails)
                       .ThenInclude(d => d.VPP)
                       .ThenInclude(v => v.VPPCategory));

            return (data ?? new()).Select(MapToResDTO).ToList();
        }

        // ─── Private helpers ───────────────────────────────────────────────

        private static bool IsDeadlinePassed(int year, int month)
            => DateTime.Now > new DateTime(year, month, 5);

        private static string GenerateVPPCode(int year, int month, int userId)
            => $"VPP-{year}{month:D2}-{userId}-{DateTime.Now:mmss}";

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
            {
                throw new InvalidOperationException("Order must contain at least one item.");
            }

            if (items.Any(i => i.Qty <= 0))
            {
                throw new InvalidOperationException("Item quantity must be greater than zero.");
            }

            if (items.GroupBy(i => i.VPPId).Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("Duplicate product in order items is not allowed.");
            }
        }

        private static VPP01_RequestHeaderResDTO MapToResDTO(VPP01_RequestHeader h) => new()
        {
            Id = h.Id,
            Description = h.Description,
            CreateUserId = h.CreateUserId,
            CreateDate = h.CreateDate,
            UpdateUserId = h.UpdateUserId,
            UpdateDate = h.UpdateDate,
            IsDeleted = h.IsDeleted,
            VPPCode = h.VPPCode,
            Y = h.Y,
            M = h.M,
            Status = h.Status,
            DepartmentCode = h.DepartmentCode,
            MemberCompanyCode = h.MemberCompanyCode,
            SubmittedDate = h.SubmittedDate,
            Items = (h.VPP02_RequestDetails ?? new List<VPP02_RequestDetail>())
            .Where(d => !d.IsDeleted)
            .Select(d => new VPP02_RequestDetailResDTO
            {
                Id = d.Id,
                Description = d.Description,
                CreateUserId = d.CreateUserId,
                CreateDate = d.CreateDate,
                UpdateUserId = d.UpdateUserId,
                UpdateDate = d.UpdateDate,
                IsDeleted = d.IsDeleted,
                VPPId = d.VPPId,
                VPPCode = d.VPP?.VPPCode,
                VPPName = d.VPP?.VPPName,
                UOMCode = d.VPP?.UOM?.ClassDetailCode,
                UOMName = d.VPP?.UOM?.ClassDetailValue,
                CategoryName = d.VPP?.VPPCategory?.VPPCategoryName,
                Qty = d.Qty,
                CurrentSinglePrice = d.CurrentSinglePrice
            }).ToList()
        };
    }
}
