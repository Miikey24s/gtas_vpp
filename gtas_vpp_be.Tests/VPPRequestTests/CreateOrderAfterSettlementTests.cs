using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.VPPRequestTests;

public class CreateOrderAfterSettlementTests
{
    [Fact]
    public async Task CreateOrder_AfterSettlement_BlocksCreate()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 4, 1, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        AddSettledHeader(context, 2026, 3, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateOrderAsync(CreateOrderRequest(2026, 3, isAdditionalOrder: false, vppId), 5615, "IT", "77500"));

        Assert.Contains("pricing/settlement", ex.Message);
    }

    [Fact]
    public async Task CreateAdditional_AfterSettlement_BlocksCreate()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 4, 10, 9, 0, 0);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        AddSettledHeader(context, 2026, 4, now);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateOrderAsync(CreateOrderRequest(2026, 4, isAdditionalOrder: true, vppId), 5615, "IT", "77500"));

        Assert.Contains("pricing/settlement", ex.Message);
    }

    private static VPPRequestService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory.Object,
            ServiceTestHelpers.CreateHttpContextAccessor(),
            unitOfWork.Object,
            new FakeDateTimeProvider(now),
            config,
                ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static VPP01_CreateReqDTO CreateOrderRequest(int year, int month, bool isAdditionalOrder, Guid vppId)
        => new()
        {
            Y = year,
            M = month,
            Description = "Test order",
            IsAdditionalOrder = isAdditionalOrder,
            SupplementReason = isAdditionalOrder ? "Needed for a new employee" : null,
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = vppId, Qty = 1, Description = "Item" }
            }
        };

    private static void AddSettledHeader(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        int y,
        int m,
        DateTime now)
    {
        context.Set<VPP01_RequestHeader>().Add(new VPP01_RequestHeader
        {
            Id = Guid.NewGuid(),
            Y = y,
            M = m,
            VPPCode = $"VPP-{Guid.NewGuid():N}"[..24],
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreateUserId = 9999,
            CreateDate = now.AddDays(-1),
            UpdateUserId = 9999,
            UpdateDate = now.AddDays(-1),
            SubmittedDate = now.AddDays(-1),
            SettledAt = now.AddMinutes(-30),
            SettledByUserId = 5615
        });
    }
}
