using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

// Gom nhu cầu kỳ (D7): endpoint chỉ đọc phải nhìn đúng tập "đơn hợp lệ hiện hành"
// mà bước chốt kỳ dùng — đơn thường Submitted/Approved, đơn bổ sung chỉ Approved.
public sealed class VPPRequestPeriodDemandTests
{
    private const int RequesterId = 5615;
    private const int PeerRequesterId = 7777;
    private const string Company = "77500";
    private static readonly DateTime OpenPeriodNow = new(2026, 4, 10, 9, 0, 0);

    [Fact]
    public async Task PeriodDemand_AggregatesByItem_AcrossCurrentValidOrdersOnly()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await SeedUomAsync(context);
        await SeedUserAsync(context, RequesterId, "Nguyễn Văn A");
        await SeedUserAsync(context, PeerRequesterId, "Trần Thị B");
        var service = CreateService(context);

        var itRegular = await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Description = "IT regular",
            Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 3, Description = "Hồ sơ dự án" }]
        }, RequesterId, "IT", Company);

        await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Description = "HR regular",
            Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 5 }]
        }, PeerRequesterId, "HR", Company);

        // Đơn bổ sung Pending KHÔNG được tính vào nhu cầu (chỉ Approved mới hợp lệ).
        await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            IsAdditionalOrder = true,
            BaseRequestId = itRegular.Id,
            SupplementReason = "Pending supplement must not count",
            Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 100 }]
        }, RequesterId, "IT", Company);

        var demand = await service.GetPeriodDemandAsync(2026, 4, Company);

        Assert.Equal(2026, demand.Year);
        Assert.Equal(4, demand.Month);
        Assert.Equal(2, demand.TotalOrders);
        Assert.Equal(8, demand.TotalQty);

        var item = Assert.Single(demand.Items);
        Assert.Equal(vppId, item.VppId);
        Assert.Equal("Test VPP", item.VppName);
        Assert.Equal("Test Category", item.CategoryName);
        Assert.Equal(8, item.TotalQty);
        Assert.Equal(0, item.UnitPrice);

        // RequesterName resolve qua view v_Users — provider InMemory không seed được view
        // không khóa nên chỉ kiểm UserId; tên hiển thị được phủ ở integration/E2E.
        Assert.Equal(2, item.Breakdown.Count);
        Assert.Equal(5, item.Breakdown[0].Qty);
        Assert.Equal(PeerRequesterId, item.Breakdown[0].UserId);
        Assert.Equal(3, item.Breakdown[1].Qty);
        Assert.Equal(RequesterId, item.Breakdown[1].UserId);
        Assert.Equal("Hồ sơ dự án", item.Breakdown[1].Note);
    }

    [Fact]
    public async Task PeriodDemand_ScopesByCompany_AndReturnsEmptyForBlankPeriod()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await SeedUomAsync(context);
        await SeedUserAsync(context, RequesterId, "Nguyễn Văn A");
        var service = CreateService(context);

        await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 2 }]
        }, RequesterId, "IT", Company);

        var otherCompany = await service.GetPeriodDemandAsync(2026, 4, "99999");
        Assert.Equal(0, otherCompany.TotalOrders);
        Assert.Empty(otherCompany.Items);

        var emptyPeriod = await service.GetPeriodDemandAsync(2026, 5, Company);
        Assert.Equal(0, emptyPeriod.TotalOrders);
        Assert.Equal(0, emptyPeriod.TotalQty);
        Assert.Empty(emptyPeriod.Items);
    }

    // ServiceTestHelpers.SeedActiveVPPAsync gán UomId cố định nhưng không seed LookupValue;
    // projection của GetPeriodDemandAsync join qua VppItem.Uom nên phải có principal thật.
    private static readonly Guid FakeUomId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static async Task SeedUomAsync(VPPContext context)
    {
        context.Set<gtas_vpp_be.Model.Library.LookupValue>().Add(new gtas_vpp_be.Model.Library.LookupValue
        {
            Id = FakeUomId,
            Code = "UOM",
            Value = "Cái",
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc),
            UpdatedByUserId = 1,
            UpdatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc),
            IsDeleted = false
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(VPPContext context, int userId, string fullName)
    {
        context.Set<AppUser>().Add(new AppUser
        {
            Id = userId,
            UserName = $"user{userId}",
            NormalizedUserName = $"USER{userId}",
            FullName = fullName,
            MemberCompanyCode = 77500,
            CreatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
    }

    private static VPPRequestService CreateService(VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5",
                ["VPP:MaxApprovedSupplements"] = "3",
                ["VPP:MaxSupplementAttempts"] = "6"
            })
            .Build();
        return new VPPRequestService(
            factory.Object,
            ServiceTestHelpers.CreateHttpContextAccessor(),
            unitOfWork.Object,
            new FakeDateTimeProvider(OpenPeriodNow),
            configuration,
            ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<gtas_vpp_be.Service.Services.BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }
}
