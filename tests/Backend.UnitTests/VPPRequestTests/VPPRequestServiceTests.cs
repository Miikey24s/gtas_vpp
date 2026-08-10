using System.Reflection;
using System.Runtime.ExceptionServices;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
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
        context.Set<AppUser>().Add(new AppUser
        {
            Id = 5615,
            UserName = "period-test",
            FullName = "Period Test User",
            MemberCompanyCode = 77500
        });
        await context.SaveChangesAsync();
        var service = CreateService(context, new DateTime(2026, 5, 12, 8, 0, 0));

        var result = await service.GetCurrentPeriodInfoAsync(5615);

        Assert.Equal(2026, result.CurrentPeriodYear);
        Assert.Equal(5, result.CurrentPeriodMonth);
        Assert.Equal(new DateTime(2026, 6, 5), result.DeadlineDate);
        Assert.False(result.IsDeadlinePassed);
    }

    [Fact]
    public async Task GetMyOrderHistorySummary_UsesCurrentRevisionsAndBuildsTwelveMonthSeries()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 7, 23, 9, 0, 0);
        var regular = CreateHistoryHeader(5615, 2026, 6, false, true, "June order", now.AddMonths(-1));
        var additional = CreateHistoryHeader(5615, 2026, 7, true, true, "July supplement", now);
        var superseded = CreateHistoryHeader(5615, 2026, 7, false, false, "Old revision", now.AddDays(-2));
        var anotherUser = CreateHistoryHeader(9001, 2026, 7, false, true, "Other user", now);
        context.Set<VppRequest>().AddRange(regular, additional, superseded, anotherUser);
        context.Set<VppRequestDetail>().AddRange(
            CreateDetailWithQuantity(regular.Id, now, 10),
            CreateDetailWithQuantity(additional.Id, now, 4),
            CreateDetailWithQuantity(superseded.Id, now, 99),
            CreateDetailWithQuantity(anotherUser.Id, now, 77));
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var result = await service.GetMyOrderHistorySummaryAsync(5615, null, null);

        Assert.Equal(2, result.PeriodCount);
        Assert.Equal(2, result.TotalOrders);
        Assert.Equal(2, result.TotalLines);
        Assert.Equal(14, result.TotalQuantity);
        Assert.Equal(202607, result.LatestPeriod);
        Assert.Equal(202606, result.AvailableFromPeriod);
        Assert.Equal(202607, result.AvailableToPeriod);
        Assert.Equal(12, result.Periods.Count);
        Assert.Equal(202508, result.Periods[0].PeriodKey);
        Assert.Equal(10, result.Periods.Single(point => point.PeriodKey == 202606).RegularQuantity);
        Assert.Equal(4, result.Periods.Single(point => point.PeriodKey == 202607).AdditionalQuantity);
        Assert.Equal(0, result.Periods.Single(point => point.PeriodKey == 202605).TotalQuantity);
    }

    [Fact]
    public async Task GetMyOrderHistoryPage_FiltersAndReturnsSummaryRowsWithoutItems()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 7, 23, 9, 0, 0);
        var matching = CreateHistoryHeader(5615, 2026, 7, false, true, "Quarterly stationery", now);
        var other = CreateHistoryHeader(5615, 2026, 6, true, true, "Supplement", now.AddMonths(-1));
        var settledPeriod = new VppPeriod
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            TimeZoneId = "Asia/Ho_Chi_Minh",
            Year = 2026,
            Month = 7,
            StartAtUtc = new DateTime(2026, 6, 5, 17, 0, 0, DateTimeKind.Utc),
            SubmissionDeadlineUtc = new DateTime(2026, 7, 5, 17, 0, 0, DateTimeKind.Utc),
            SupplementApprovalDeadlineUtc = new DateTime(2026, 7, 7, 17, 0, 0, DateTimeKind.Utc),
            State = VppPeriodState.Settled
        };
        matching.PeriodId = settledPeriod.Id;
        context.Set<VppRequest>().AddRange(matching, other);
        context.Set<VppPeriod>().Add(settledPeriod);
        context.Set<VppRequestDetail>().AddRange(
            CreateDetailWithQuantity(matching.Id, now, 12),
            CreateDetailWithQuantity(other.Id, now, 8));
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var (data, totalCount) = await service.GetMyOrderHistoryPageAsync(
            5615, 202601, 202612, 202607, "stationery", null, false, 0, 6);

        var order = Assert.Single(data);
        Assert.Equal(1, totalCount);
        Assert.Equal(matching.Id, order.Id);
        Assert.Equal(1, order.TotalLines);
        Assert.Equal(12, order.TotalQty);
        Assert.Equal("Settled", order.PeriodState);
        Assert.Empty(order.Items);
    }

    [Fact]
    public async Task GetDepartmentOrderHistory_UsesDepartmentAndCompanyScopeForSummaryAndPage()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 7, 23, 9, 0, 0);
        var matching = CreateHistoryHeader(5615, 2026, 7, false, true, "Department stationery", now);
        var otherDepartment = CreateHistoryHeader(9001, 2026, 7, false, true, "HR stationery", now);
        otherDepartment.DepartmentCode = "HR";
        var otherCompany = CreateHistoryHeader(9002, 2026, 7, true, true, "Other company", now);
        otherCompany.MemberCompanyCode = "88000";
        context.Set<VppRequest>().AddRange(matching, otherDepartment, otherCompany);
        context.Set<VppRequestDetail>().AddRange(
            CreateDetailWithQuantity(matching.Id, now, 12),
            CreateDetailWithQuantity(otherDepartment.Id, now, 30),
            CreateDetailWithQuantity(otherCompany.Id, now, 50));
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var summary = await service.GetDepartmentOrderHistorySummaryAsync("IT", "77500", null, null);
        var (data, totalCount) = await service.GetDepartmentOrderHistoryPageAsync(
            "IT", "77500", null, null, 202607, "stationery", null, false, 0, 6);

        Assert.Equal(1, summary.TotalOrders);
        Assert.Equal(12, summary.TotalQuantity);
        Assert.Equal(202607, summary.AvailableFromPeriod);
        Assert.Equal(202607, summary.AvailableToPeriod);
        var order = Assert.Single(data);
        Assert.Equal(1, totalCount);
        Assert.Equal(matching.Id, order.Id);
        Assert.Equal("IT", order.DepartmentCode);
        Assert.Equal(12, order.TotalQty);
    }

    private static VPPRequestService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            unitOfWork.Object,
            dateTimeProvider,
            config);
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

    private static VppRequest CreateHistoryHeader(
        int userId,
        int year,
        int month,
        bool isAdditional,
        bool isCurrentRevision,
        string description,
        DateTime submittedAt)
        => new()
        {
            Id = Guid.NewGuid(),
            VppCode = $"VPP-{year:D4}{month:D2}-{Guid.NewGuid():N}",
            Year = year,
            Month = month,
            RequestSeriesId = Guid.NewGuid(),
            RevisionNumber = isCurrentRevision ? 2 : 1,
            IsCurrentRevision = isCurrentRevision,
            Status = (int)VPPStatus.Submitted,
            IsAdditionalOrder = isAdditional,
            Description = description,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            SubmittedDate = submittedAt,
            CreatedByUserId = userId,
            CreatedAtUtc = submittedAt,
            UpdatedByUserId = userId,
            UpdatedAtUtc = submittedAt
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

    private static VppRequestDetail CreateDetailWithQuantity(Guid headerId, DateTime now, int quantity)
    {
        var detail = CreateDetail(headerId, now);
        detail.Qty = quantity;
        return detail;
    }

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

    private static string InvokeGenerateVppCode(VPPRequestService service, int year, int month, int userId)
        => (string)typeof(VPPRequestService)
            .GetMethod("GenerateVppCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month, userId })!;
}
