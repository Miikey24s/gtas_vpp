using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

public sealed class OrderQuantityLimitServiceTests
{
    [Fact]
    public async Task ValidateAsync_AllowsLimitAndRejectsQuantityAboveLimit()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var itemId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, itemId);
        var item = await context.VppItems.SingleAsync(x => x.Id == itemId);
        item.VppName = "Giấy A4";
        item.MaxQuantityPerOrder = 500;
        await context.SaveChangesAsync();

        var service = new OrderQuantityLimitService(
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object);

        await service.ValidateAsync(
        [
            new VppRequestDetailItemReqDTO { VppId = itemId, Qty = 500 }
        ]);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.ValidateAsync(
        [
            new VppRequestDetailItemReqDTO { VppId = itemId, Qty = 501 }
        ]));

        Assert.Contains("Giấy A4", exception.Message, StringComparison.Ordinal);
        Assert.Contains("tối đa 500", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DomainAndWireLimits_StayAligned()
    {
        Assert.Equal(gtas_vpp_shared.Constants.VppOrderQuantityLimits.Minimum, VppItemQuantityLimits.Minimum);
        Assert.Equal(gtas_vpp_shared.Constants.VppOrderQuantityLimits.Default, VppItemQuantityLimits.Default);
        Assert.Equal(gtas_vpp_shared.Constants.VppOrderQuantityLimits.Maximum, VppItemQuantityLimits.Maximum);
    }
}
