using System.Reflection;
using System.Runtime.ExceptionServices;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

public class VPPRequestServiceTests
{
    [Fact]
    public void ValidateItems_NullItems_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(null));

        Assert.Contains("at least one item", exception.Message);
    }

    [Fact]
    public void ValidateItems_EmptyItems_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(new List<VppRequestDetailItemReqDTO>()));

        Assert.Contains("at least one item", exception.Message);
    }

    [Fact]
    public void ValidateItems_NonPositiveQuantity_ThrowsInvalidOperationException()
    {
        var items = new List<VppRequestDetailItemReqDTO>
        {
            new() { VppId = Guid.NewGuid(), Qty = 0 }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(items));

        Assert.Contains("greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateItems_DuplicateVppId_ThrowsInvalidOperationException()
    {
        var vppId = Guid.NewGuid();
        var items = new List<VppRequestDetailItemReqDTO>
        {
            new() { VppId = vppId, Qty = 1 },
            new() { VppId = vppId, Qty = 2 }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(items));

        Assert.Contains("Duplicate product", exception.Message);
    }

    [Fact]
    public void IsDeadlinePassed_DateBeforeDeadline_ReturnsFalse()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 4, 23, 59, 59));

        var result = InvokeIsDeadlinePassed(service, 2026, 4);

        Assert.False(result);
    }

    [Fact]
    public void IsDeadlinePassed_DateAfterDeadline_ReturnsTrue()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 6));

        var result = InvokeIsDeadlinePassed(service, 2026, 3);

        Assert.True(result);
    }

    // NOTE: Obsolete test removed — VppCode format đã đổi sang
    // "VPP-{Year:D4}{Month:D2}-{Guid:N}" (24 chars, no userId leak) trong P0.3.
    // Coverage chuyển sang VppCodeGeneratorTests.cs (3 test: format/uniqueness/no-userId).

    [Fact]
    public async Task CreateOrderAsync_RegularOrder_CreatesSubmittedHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var request = CreateOrderRequest(year: 2026, month: 3, isAdditionalOrder: false, vppId: vppId);

        var result = await service.CreateOrderAsync(request, 5615, "IT", "77500");

        Assert.Equal((int)VPPStatus.Submitted, result.Status);
        var header = Assert.Single(context.Set<VppRequest>());
        Assert.Equal((int)VPPStatus.Submitted, header.Status);
        Assert.False(header.IsAdditionalOrder);
    }

    [Fact]
    public async Task CreateOrderAsync_AdditionalOrder_CreatesPendingHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 10, 9, 7, 8));
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var regular = await service.CreateOrderAsync(
            CreateOrderRequest(year: 2026, month: 4, isAdditionalOrder: false, vppId: vppId),
            5615,
            "IT",
            "77500");
        var request = CreateOrderRequest(year: 2026, month: 4, isAdditionalOrder: true, vppId: vppId);
        request.BaseRequestId = regular.Id;

        var result = await service.CreateOrderAsync(request, 5615, "IT", "77500");

        Assert.Equal((int)VPPStatus.Pending, result.Status);
        var header = Assert.Single(context.Set<VppRequest>().Where(x => x.IsAdditionalOrder));
        Assert.Equal((int)VPPStatus.Pending, header.Status);
        Assert.True(header.IsAdditionalOrder);
        Assert.Equal(regular.Id, header.BaseRequestId);
        Assert.Equal("Needed for a new employee", header.SupplementReason);
        Assert.Equal(1, header.SupplementAttemptNumber);
        var createLog = Assert.Single(context.Set<RequestLog>().Where(x =>
            x.RequestId == header.Id && x.Action == "CREATE"));
        Assert.Equal("Needed for a new employee", createLog.Reason);
    }

    [Fact]
    public async Task CreateOrder_SnapshotsPriceFromL06_Test()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 4, 1, 9, 7, 8);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (vppId, 125000));

        var service = CreateService(context, now);
        var request = CreateOrderRequest(year: 2026, month: 3, isAdditionalOrder: false, vppId: vppId);

        await service.CreateOrderAsync(request, 5615, "IT", "77500");

        var detail = Assert.Single(context.Set<VppRequestDetail>());
        Assert.Equal(125000L, detail.CurrentSinglePrice);
    }

    [Fact]
    public async Task Approve_SetsApprovedByAndAt_Test()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 2, 10, 30, 0);
        var header = CreatePendingAdditionalHeader(now);
        header.RowVersion = new byte[] { 1, 2, 3 };
        context.Set<VppRequest>().Add(header);
        context.Set<VppRequestDetail>().Add(CreateDetail(header.Id, now));
        await context.SaveChangesAsync();

        var service = CreateService(context, now);

        await service.ApproveAdditionalOrderAsync(
            header.Id, 9001, header.RowVersion, "approve-test", "IT", true, "77500");

        var saved = Assert.Single(context.Set<VppRequest>());
        Assert.Equal((int)VPPStatus.Approved, saved.Status);
        Assert.Equal(9001, saved.ApprovedById);
        Assert.Equal(now, saved.ApprovedAt);
        Assert.Null(saved.RejectReason);
    }

    [Fact]
    public async Task Reject_SetsRejectedAuditFields_AndWritesReasonIntoTypedLog()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 2, 10, 30, 0);
        var header = CreatePendingAdditionalHeader(now);
        header.RowVersion = new byte[] { 4, 5, 6 };
        context.Set<VppRequest>().Add(header);
        context.Set<VppRequestDetail>().Add(CreateDetail(header.Id, now));
        await context.SaveChangesAsync();

        var service = CreateService(context, now);

        await service.RejectAdditionalOrderAsync(
            header.Id, 9002, "Budget exceeded", header.RowVersion,
            "reject-test", "IT", true, "77500");

        var saved = Assert.Single(context.Set<VppRequest>());
        Assert.Equal((int)VPPStatus.Rejected, saved.Status);
        Assert.Equal(9002, saved.RejectedById);
        Assert.Equal(now, saved.RejectedAt);
        Assert.Equal("Budget exceeded", saved.RejectReason);

        var log = Assert.Single(context.Set<RequestLog>());
        Assert.Equal("REJECT", log.LogTitle);
        Assert.Equal("REJECT", log.Action);
        Assert.Equal(9002, log.ActorUserId);
        Assert.Equal("Budget exceeded", log.Reason);
        Assert.Contains("\"RejectReason\":\"Budget exceeded\"", log.LogJS);
    }

    [Fact]
    public async Task GetCurrentPeriodInfoAsync_CurrentPeriodUsesNextMonthDeadline()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 5, 12, 8, 0, 0));

        var result = await service.GetCurrentPeriodInfoAsync(5615);

        Assert.Equal(2026, result.CurrentPeriodYear);
        Assert.Equal(5, result.CurrentPeriodMonth);
        Assert.Equal(new DateTime(2026, 6, 5), result.DeadlineDate);
        Assert.False(result.IsDeadlinePassed);
    }

    private static VPPRequestService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory.Object,
            httpContextAccessor,
            unitOfWork.Object,
            dateTimeProvider,
            config,
                ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static VppRequestCreateReqDTO CreateOrderRequest(int year, int month, bool isAdditionalOrder, Guid? vppId = null)
        => new()
        {
            Year = year,
            Month = month,
            Description = "Test order",
            IsAdditionalOrder = isAdditionalOrder,
            SupplementReason = isAdditionalOrder ? "Needed for a new employee" : null,
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId ?? Guid.NewGuid(), Qty = 3, Description = "Item 1" }
            }
        };

    private static VppRequest CreatePendingAdditionalHeader(DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Month = 4,
            VppCode = "VPP-202604-TEST-000001",
            Status = (int)VPPStatus.Pending,
            IsAdditionalOrder = true,
            RequestSeriesId = Guid.NewGuid(),
            RevisionNumber = 1,
            IsCurrentRevision = true,
            BaseRequestSeriesId = Guid.NewGuid(),
            SupplementSequence = 1,
            SupplementAttemptNumber = 1,
            SupplementReason = "Needed for a new employee",
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreatedByUserId = 5615,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now.AddDays(-1),
            SubmittedDate = now.AddDays(-1)
        };

    private static VppRequestDetail CreateDetail(Guid headerId, DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            RequestId = headerId,
            VppId = Guid.NewGuid(),
            Qty = 1,
            CurrentSinglePrice = 1000,
            Description = "Seed detail",
            CreatedByUserId = 5615,
            CreatedAtUtc = now.AddDays(-1),
            UpdatedByUserId = 5615,
            UpdatedAtUtc = now.AddDays(-1)
        };

    private static void InvokeValidateItems(List<VppRequestDetailItemReqDTO>? items)
    {
        try
        {
            typeof(VPPRequestService)
                .GetMethod("ValidateItems", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object?[] { items });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
    }

    private static bool InvokeIsDeadlinePassed(VPPRequestService service, int year, int month)
        => (bool)typeof(VPPRequestService)
            .GetMethod("IsDeadlinePassed", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month })!;

    private static string InvokeGenerateVppCode(VPPRequestService service, int year, int month, int userId)
        => (string)typeof(VPPRequestService)
            .GetMethod("GenerateVppCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month, userId })!;
}
