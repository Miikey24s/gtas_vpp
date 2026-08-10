using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PeriodSettlementTests;

public sealed class PostSettlementOrderCorrectionServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 20, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Adjust_RequiresAnotherManager_AndCreatesRequestAndSettlementRevisions()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);

        var pending = await service.CreateAsync("77500", 5615, new PostSettlementOrderCorrectionCreateReqDTO
        {
            RequestId = seed.RequestId,
            Action = "Adjust",
            Reason = "Điều chỉnh số lượng theo biên bản đối soát",
            EmployeeNote = "Số lượng đã được quản lý kỳ điều chỉnh sau khi chốt.",
            RequestRowVersion = seed.RequestRowVersion,
            Items = [new PostSettlementOrderCorrectionItemReqDTO { VppId = seed.VppId, Qty = 3 }]
        });
        var tracked = await context.Set<PostSettlementOrderCorrection>().SingleAsync();
        tracked.RowVersion = [9, 8, 7];

        await Assert.ThrowsAsync<ConflictException>(() => service.ConfirmAsync(
            pending.Id, 5615, new PostSettlementOrderCorrectionDecisionReqDTO { RowVersion = tracked.RowVersion }));
        var confirmed = await service.ConfirmAsync(
            pending.Id, 5616, new PostSettlementOrderCorrectionDecisionReqDTO { RowVersion = tracked.RowVersion });

        Assert.Equal("Confirmed", confirmed.Status);
        Assert.NotNull(confirmed.ResultRequestId);
        Assert.NotNull(confirmed.ResultSettlementId);
        var requests = await context.Set<VppRequest>().OrderBy(x => x.RevisionNumber).ToListAsync();
        Assert.Equal(2, requests.Count);
        Assert.False(requests[0].IsCurrentRevision);
        Assert.True(requests[1].IsCurrentRevision);
        Assert.Equal(3, Assert.Single(requests[1].RequestDetails).Qty);
        var settlements = await context.Set<Settlement>().OrderBy(x => x.RevisionNumber).ToListAsync();
        Assert.Equal(2, settlements.Count);
        Assert.False(settlements[0].IsCurrentRevision);
        Assert.True(settlements[1].IsCurrentRevision);
        Assert.Equal(3m, Assert.Single(settlements[1].Items).Quantity);
        Assert.Equal(settlements[1].GrandTotal, settlements[1].Allocations.Sum(x => x.GrossAmount));
    }

    [Fact]
    public async Task Cancel_CreatesCancelledRequestAndZeroDemandSettlementRevision()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var pending = await service.CreateAsync("77500", 5615, new PostSettlementOrderCorrectionCreateReqDTO
        {
            RequestId = seed.RequestId,
            Action = "Cancel",
            Reason = "Hủy theo biên bản xác nhận của đơn vị",
            EmployeeNote = "Đơn đã được hủy sau chốt và có lưu lịch sử.",
            RequestRowVersion = seed.RequestRowVersion
        });
        var tracked = await context.Set<PostSettlementOrderCorrection>().SingleAsync();
        tracked.RowVersion = [1, 2, 3];

        await service.ConfirmAsync(
            pending.Id, 5616, new PostSettlementOrderCorrectionDecisionReqDTO { RowVersion = tracked.RowVersion });

        var currentRequest = await context.Set<VppRequest>().SingleAsync(x => x.IsCurrentRevision);
        Assert.Equal((int)VPPStatus.Cancelled, currentRequest.Status);
        var currentSettlement = await context.Set<Settlement>()
            .Include(x => x.Items)
            .Include(x => x.Allocations)
            .SingleAsync(x => x.IsCurrentRevision);
        Assert.Empty(currentSettlement.Items);
        Assert.Empty(currentSettlement.Allocations);
        Assert.Equal(0m, currentSettlement.GrandTotal);
    }

    [Fact]
    public async Task Pending_correction_preserves_current_revisions_and_blocks_duplicate_request()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var request = new PostSettlementOrderCorrectionCreateReqDTO
        {
            RequestId = seed.RequestId,
            Action = "Adjust",
            Reason = "Điều chỉnh theo biên bản đối soát đã ký",
            EmployeeNote = "Yêu cầu đang chờ quản lý khác xác nhận.",
            RequestRowVersion = seed.RequestRowVersion,
            Items = [new PostSettlementOrderCorrectionItemReqDTO { VppId = seed.VppId, Qty = 3 }]
        };

        var pending = await service.CreateAsync("77500", 5615, request);

        Assert.Equal("Pending", pending.Status);
        Assert.Single(await context.Set<VppRequest>().Where(x => x.IsCurrentRevision).ToListAsync());
        Assert.Single(await context.Set<Settlement>().Where(x => x.IsCurrentRevision).ToListAsync());
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync("77500", 5616, request));
    }

    [Fact]
    public async Task Reject_requires_another_manager_and_reason_without_creating_revisions()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var pending = await service.CreateAsync("77500", 5615, new PostSettlementOrderCorrectionCreateReqDTO
        {
            RequestId = seed.RequestId,
            Action = "Cancel",
            Reason = "Hủy theo biên bản xác nhận của đơn vị",
            EmployeeNote = "Yêu cầu hủy đang chờ quản lý khác xác nhận.",
            RequestRowVersion = seed.RequestRowVersion
        });
        var tracked = await context.Set<PostSettlementOrderCorrection>().SingleAsync();
        tracked.RowVersion = [7, 7, 7];

        await Assert.ThrowsAsync<ConflictException>(() => service.RejectAsync(
            pending.Id,
            5615,
            new PostSettlementOrderCorrectionDecisionReqDTO
            {
                Reason = "Không đồng ý yêu cầu hủy đơn này.",
                RowVersion = tracked.RowVersion
            }));
        await Assert.ThrowsAsync<BusinessException>(() => service.RejectAsync(
            pending.Id,
            5616,
            new PostSettlementOrderCorrectionDecisionReqDTO
            {
                Reason = "Sai",
                RowVersion = tracked.RowVersion
            }));

        var rejected = await service.RejectAsync(
            pending.Id,
            5616,
            new PostSettlementOrderCorrectionDecisionReqDTO
            {
                Reason = "Biên bản chưa đủ căn cứ để hủy đơn.",
                RowVersion = tracked.RowVersion
            });

        Assert.Equal("Rejected", rejected.Status);
        Assert.Single(await context.Set<VppRequest>().ToListAsync());
        Assert.Single(await context.Set<Settlement>().ToListAsync());
    }

    [Fact]
    public async Task Confirm_rejects_stale_request_revision_before_mutating_settlement()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context);
        var service = CreateService(context);
        var pending = await service.CreateAsync("77500", 5615, new PostSettlementOrderCorrectionCreateReqDTO
        {
            RequestId = seed.RequestId,
            Action = "Adjust",
            Reason = "Điều chỉnh theo biên bản đối soát đã ký",
            EmployeeNote = "Số lượng đề nghị điều chỉnh sau khi chốt.",
            RequestRowVersion = seed.RequestRowVersion,
            Items = [new PostSettlementOrderCorrectionItemReqDTO { VppId = seed.VppId, Qty = 3 }]
        });
        var correction = await context.Set<PostSettlementOrderCorrection>().SingleAsync();
        correction.RowVersion = [6, 6, 6];
        var currentRequest = await context.Set<VppRequest>().SingleAsync();
        currentRequest.IsCurrentRevision = false;
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => service.ConfirmAsync(
            pending.Id,
            5616,
            new PostSettlementOrderCorrectionDecisionReqDTO { RowVersion = correction.RowVersion }));

        Assert.Single(await context.Set<VppRequest>().ToListAsync());
        Assert.Single(await context.Set<Settlement>().ToListAsync());
        Assert.True((await context.Set<Settlement>().SingleAsync()).IsCurrentRevision);
    }

    private static PostSettlementOrderCorrectionService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
        => new(ServiceTestHelpers.CreateUnitOfWorkMock(context).Object, new FakeDateTimeProvider(Now));

    private static async Task<SeedResult> SeedAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var periodId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();
        var settlementItemId = Guid.NewGuid();
        var vppId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 5, 6 };
        context.Set<VppPeriod>().Add(new VppPeriod
        {
            Id = periodId,
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 8,
            StartAtUtc = Now.AddMonths(-1),
            SubmissionDeadlineUtc = Now.AddDays(-10),
            SupplementApprovalDeadlineUtc = Now.AddDays(-8),
            State = VppPeriodState.Settled,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        var request = new VppRequest
        {
            Id = requestId,
            PeriodId = periodId,
            RequestSeriesId = Guid.NewGuid(),
            RevisionNumber = 1,
            IsCurrentRevision = true,
            VppCode = "VPP-202608-TEST",
            Year = 2026,
            Month = 8,
            Status = (int)VPPStatus.Submitted,
            MemberCompanyCode = "77500",
            DepartmentCode = "IT",
            SettledAt = Now.AddDays(-1),
            SettledByUserId = 5000,
            SettledByPriceListId = priceListId,
            RowVersion = rowVersion,
            CreatedByUserId = 100,
            CreatedAtUtc = Now.AddMonths(-1),
            UpdatedByUserId = 100,
            UpdatedAtUtc = Now.AddDays(-1),
            RequestDetails = [new VppRequestDetail
            {
                Id = Guid.NewGuid(),
                RequestId = requestId,
                VppId = vppId,
                Qty = 2,
                CreatedByUserId = 100,
                CreatedAtUtc = Now.AddMonths(-1),
                UpdatedByUserId = 100,
                UpdatedAtUtc = Now.AddDays(-1)
            }]
        };
        context.Set<VppRequest>().Add(request);
        context.Set<Settlement>().Add(new Settlement
        {
            Id = settlementId,
            PeriodId = periodId,
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 8,
            RevisionNumber = 1,
            IsCurrentRevision = true,
            PrimarySupplierId = Guid.NewGuid(),
            PrimarySupplierName = "Supplier",
            PriceListId = priceListId,
            PriceListName = "Price book",
            PriceListVersion = 1,
            PriceAsOfUtc = Now.AddDays(-2),
            CalculationVersion = PriceCalculationEngine.CurrentVersion,
            InputHash = new string('A', 64),
            IdempotencyKey = string.Concat("correction", "-", new string('K', 14)),
            CommandPayloadHash = new string('B', 64),
            CurrencyCode = "VND",
            Subtotal = 200,
            VatAmount = 20,
            GrandTotal = 220,
            ConfirmedAtUtc = Now.AddDays(-1),
            ConfirmedByUserId = 5000,
            CreatedByUserId = 5000,
            CreatedAtUtc = Now.AddDays(-1),
            UpdatedByUserId = 5000,
            UpdatedAtUtc = Now.AddDays(-1),
            Items = [new SettlementItem
            {
                Id = settlementItemId,
                SettlementId = settlementId,
                VppId = vppId,
                VppCode = "PEN-01",
                VppName = "Bút",
                UomId = Guid.NewGuid(),
                UomCode = "EA",
                UomName = "Cái",
                SupplierId = Guid.NewGuid(),
                PriceListId = priceListId,
                PriceBookItemId = Guid.NewGuid(),
                Quantity = 2,
                NetUnitPrice = 100,
                VatRate = 10,
                NetAmount = 200,
                VatAmount = 20,
                GrossAmount = 220,
                CreatedByUserId = 5000,
                CreatedAtUtc = Now.AddDays(-1),
                UpdatedByUserId = 5000,
                UpdatedAtUtc = Now.AddDays(-1)
            }]
        });
        await context.SaveChangesAsync();
        request.RowVersion = rowVersion;
        return new SeedResult(requestId, vppId, rowVersion);
    }

    private sealed record SeedResult(Guid RequestId, Guid VppId, byte[] RequestRowVersion);
}
