using System.Reflection;
using System.Runtime.ExceptionServices;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests.VPPRequestTests;

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
        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(new List<VPP02_ItemReqDTO>()));

        Assert.Contains("at least one item", exception.Message);
    }

    [Fact]
    public void ValidateItems_NonPositiveQuantity_ThrowsInvalidOperationException()
    {
        var items = new List<VPP02_ItemReqDTO>
        {
            new() { VPPId = Guid.NewGuid(), Qty = 0 }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => InvokeValidateItems(items));

        Assert.Contains("greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateItems_DuplicateVPPId_ThrowsInvalidOperationException()
    {
        var vppId = Guid.NewGuid();
        var items = new List<VPP02_ItemReqDTO>
        {
            new() { VPPId = vppId, Qty = 1 },
            new() { VPPId = vppId, Qty = 2 }
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

        var result = InvokeIsDeadlinePassed(service, 2026, 4);

        Assert.True(result);
    }

    // NOTE: Obsolete test removed — VPPCode format đã đổi sang
    // "VPP-{Y:D4}{M:D2}-{Guid:N}" (24 chars, no userId leak) trong P0.3.
    // Coverage chuyển sang VPPCodeGeneratorTests.cs (3 test: format/uniqueness/no-userId).

    [Fact]
    public async Task CreateOrderAsync_RegularOrder_CreatesSubmittedHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));
        var request = CreateOrderRequest(isAdditionalOrder: false);

        var result = await service.CreateOrderAsync(request, 5615, "IT", "77500");

        Assert.Equal((int)VPPStatus.Submitted, result.Status);
        var header = Assert.Single(context.Set<VPP01_RequestHeader>());
        Assert.Equal((int)VPPStatus.Submitted, header.Status);
        Assert.False(header.IsAdditionalOrder);
    }

    [Fact]
    public async Task CreateOrderAsync_AdditionalOrder_CreatesPendingHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 10, 9, 7, 8));
        var request = CreateOrderRequest(isAdditionalOrder: true);

        var result = await service.CreateOrderAsync(request, 5615, "IT", "77500");

        Assert.Equal((int)VPPStatus.Pending, result.Status);
        var header = Assert.Single(context.Set<VPP01_RequestHeader>());
        Assert.Equal((int)VPPStatus.Pending, header.Status);
        Assert.True(header.IsAdditionalOrder);
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
            new EnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static VPP01_CreateReqDTO CreateOrderRequest(bool isAdditionalOrder)
        => new()
        {
            Y = 2026,
            M = 4,
            Description = "Test order",
            IsAdditionalOrder = isAdditionalOrder,
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = Guid.NewGuid(), Qty = 3, Description = "Item 1" }
            }
        };

    private static void InvokeValidateItems(List<VPP02_ItemReqDTO>? items)
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

    private static string InvokeGenerateVPPCode(VPPRequestService service, int year, int month, int userId)
        => (string)typeof(VPPRequestService)
            .GetMethod("GenerateVPPCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month, userId })!;
}
