using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
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

            using var uow = _unitOfWork.Create(GetEnvironment());
            await uow.BeginTransactionAsync();
            try
            {
                var header = new VPP01_RequestHeader
                {
                    Id = Guid.NewGuid(),
                    Y = req.Y,
                    M = req.M,
                    VPPCode = GenerateVPPCode(req.Y, req.M, createUserId),
                    Status = NormalizeEditableStatus(req.Status, (int)VPPStatus.Submitted),
                    Description = req.Description,
                    DepartmentCode = departmentCode,
                    MemberCompanyCode = memberCompanyCode,
                    CreateUserId = createUserId,
                    CreateDate = DateTime.Now,
                    UpdateUserId = createUserId,
                    UpdateDate = DateTime.Now,
                    SubmittedDate = NormalizeEditableStatus(req.Status, (int)VPPStatus.Submitted) == (int)VPPStatus.Submitted ? DateTime.Now : null
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
                    CreateDate = DateTime.Now,
                    UpdateUserId = createUserId,
                    UpdateDate = DateTime.Now
                }).ToList();

                uow.VPPContext.Set<VPP01_RequestHeader>().Add(header);
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
                    .FirstOrDefaultAsync(x => x.Id == req.Id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != req.UpdateUserId) throw new UnauthorizedAccessException("Cannot update another user's order.");
                if (header.Status is (int)VPPStatus.Cancelled or (int)VPPStatus.Closed)
                    throw new InvalidOperationException("Only Draft or Submitted orders can be edited.");
                if (IsDeadlinePassed(header.Y, header.M)) throw new InvalidOperationException("Deadline has passed.");

                var now = DateTime.Now;

                header.IsDeleted = true;
                header.UpdateUserId = req.UpdateUserId;
                header.UpdateDate = now;

                await uow.VPPContext.Set<VPP02_RequestDetail>()
                    .Where(x => x.VPP01_RequestHeaderId == header.Id && !x.IsDeleted)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsDeleted, true)
                        .SetProperty(x => x.UpdateUserId, req.UpdateUserId)
                        .SetProperty(x => x.UpdateDate, now));

                var newHeaderId = Guid.NewGuid();
                var targetStatus = NormalizeEditableStatus(req.Status, header.Status);
                var newHeader = new VPP01_RequestHeader
                {
                    Id = newHeaderId,
                    Y = header.Y,
                    M = header.M,
                    VPPCode = header.VPPCode,
                    Status = targetStatus,
                    Description = req.Description,
                    DepartmentCode = header.DepartmentCode,
                    MemberCompanyCode = header.MemberCompanyCode,
                    CreateUserId = header.CreateUserId,
                    CreateDate = now,
                    UpdateUserId = req.UpdateUserId,
                    UpdateDate = now,
                    SubmittedDate = targetStatus == (int)VPPStatus.Submitted
                        ? (header.SubmittedDate ?? now)
                        : null
                };

                newHeader.VPP02_RequestDetails = req.Items.Select(i => new VPP02_RequestDetail
                {
                    Id = Guid.NewGuid(),
                    VPPId = i.VPPId,
                    Qty = i.Qty,
                    CurrentSinglePrice = 0,
                    Description = i.Description,
                    VPP01_RequestHeaderId = newHeaderId,
                    CreateUserId = req.UpdateUserId,
                    CreateDate = now,
                    UpdateUserId = req.UpdateUserId,
                    UpdateDate = now
                }).ToList();

                uow.VPPContext.Set<VPP01_RequestHeader>().Add(newHeader);

                await uow.CommitAsync();
                return (await GetOrderByIdAsync(newHeaderId))!;
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
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot submit another user's order.");
                if (header.Status != (int)VPPStatus.Draft) throw new InvalidOperationException("Only Draft orders can be submitted.");
                if (IsDeadlinePassed(header.Y, header.M))
                    throw new InvalidOperationException("Deadline has passed.");

                header.Status = (int)VPPStatus.Submitted;
                header.SubmittedDate = DateTime.Now;
                header.UpdateUserId = userId;
                header.UpdateDate = DateTime.Now;

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
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.CreateUserId != userId) throw new UnauthorizedAccessException("Cannot cancel another user's order.");
                if (header.Status == (int)VPPStatus.Closed)
                    throw new InvalidOperationException("Closed orders cannot be cancelled.");

                header.Status = (int)VPPStatus.Cancelled;
                header.UpdateUserId = userId;
                header.UpdateDate = DateTime.Now;

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
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

                if (header == null) throw new KeyNotFoundException("Order not found.");
                if (header.Status is not ((int)VPPStatus.Draft) and not ((int)VPPStatus.Submitted))
                    throw new InvalidOperationException("Only Draft or Submitted orders can be deleted.");
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

        private static int NormalizeEditableStatus(int? status, int defaultStatus)
            => status == (int)VPPStatus.Draft || status == (int)VPPStatus.Submitted
                ? status.Value
                : defaultStatus;

        private static string GenerateVPPCode(int year, int month, int userId)
            => $"VPP-{year}{month:D2}-{userId}-{DateTime.Now:mmss}";

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
