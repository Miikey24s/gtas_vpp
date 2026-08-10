using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gtas_vpp_be.Tests.VppItemRequestTests;

public sealed class ManagerOrderAdjustmentTests
{
    private static readonly DateTime Now = new(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Manager_can_adjust_order_within_ten_days_after_closing()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, closeAtUtc: Now.AddDays(-2));
        var service = CreateService(context);

        var result = await service.AdjustOrderAfterCloseAsync(
            seed.OrderId,
            5615,
            "77500",
            Request(seed.VppId, qty: 8, seed.RowVersion));

        Assert.Equal(2, result.RevisionNumber);
        var revisions = await context.Set<VppRequest>()
            .Include(order => order.RequestDetails)
            .Where(order => order.RequestSeriesId == seed.SeriesId)
            .OrderBy(order => order.RevisionNumber)
            .ToListAsync();
        Assert.Collection(
            revisions,
            original => Assert.False(original.IsCurrentRevision),
            current =>
            {
                Assert.True(current.IsCurrentRevision);
                Assert.Equal(8, Assert.Single(current.RequestDetails).Qty);
            });
        Assert.Equal(VppPeriodState.SubmissionClosed, (await context.Set<VppPeriod>().SingleAsync()).State);
    }

    [Fact]
    public async Task Manager_cannot_adjust_order_after_the_configured_window()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var seed = await SeedAsync(context, closeAtUtc: Now.AddDays(-11));
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.AdjustOrderAfterCloseAsync(
                seed.OrderId,
                5615,
                "77500",
                Request(seed.VppId, qty: 8, seed.RowVersion)));

        Assert.Contains("hết thời gian chỉnh đơn", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<Seed> SeedAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        DateTime closeAtUtc)
    {
        var settingsId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        context.Set<VppOrderPeriodSettingsVersion>().Add(new VppOrderPeriodSettingsVersion
        {
            Id = settingsId,
            MemberCompanyCode = "77500",
            VersionNumber = 1,
            Name = "Test",
            DefaultOpenPeriodCount = 3,
            DefaultNewPeriodOpenDay = 5,
            DefaultPeriodCloseDay = 5,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            SupplementApprovalGraceDays = 5,
            PostCloseAdjustmentDays = 10,
            EffectiveFromYear = 2026,
            EffectiveFromMonth = 9,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now
        });
        context.Set<VppPeriod>().Add(new VppPeriod
        {
            Id = periodId,
            SettingsVersionId = settingsId,
            MemberCompanyCode = "77500",
            Year = 2026,
            Month = 9,
            TimeZoneId = "Asia/Ho_Chi_Minh",
            StartAtUtc = closeAtUtc.AddMonths(-1),
            SubmissionDeadlineUtc = closeAtUtc,
            SupplementApprovalDeadlineUtc = closeAtUtc.AddDays(5),
            State = VppPeriodState.SubmissionClosed,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now,
            RowVersion = [2]
        });
        context.Set<VppRequest>().Add(new VppRequest
        {
            Id = orderId,
            PeriodId = periodId,
            RequestSeriesId = seriesId,
            RevisionNumber = 1,
            IsCurrentRevision = true,
            Year = 2026,
            Month = 9,
            VppCode = "VPP-202609-TEST",
            Status = (int)VPPStatus.Submitted,
            DepartmentCode = "IT",
            MemberCompanyCode = "77500",
            CreatedByUserId = 100,
            CreatedAtUtc = closeAtUtc.AddDays(-3),
            UpdatedByUserId = 100,
            UpdatedAtUtc = closeAtUtc.AddDays(-3),
            SubmittedDate = closeAtUtc.AddDays(-3),
            RowVersion = [1],
            RequestDetails =
            [
                new VppRequestDetail
                {
                    Id = Guid.NewGuid(),
                    RequestId = orderId,
                    VppId = vppId,
                    Qty = 2,
                    CreatedByUserId = 100,
                    CreatedAtUtc = closeAtUtc.AddDays(-3),
                    UpdatedByUserId = 100,
                    UpdatedAtUtc = closeAtUtc.AddDays(-3)
                }
            ]
        });
        await context.SaveChangesAsync();
        return new Seed(orderId, seriesId, vppId, [1]);
    }

    private static VPPRequestService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();
        return new VPPRequestService(
            unitOfWork.Object,
            new FakeDateTimeProvider(Now),
            configuration);
    }

    private static VppManagerOrderAdjustmentReqDTO Request(
        Guid vppId,
        int qty,
        byte[] rowVersion) => new()
        {
            Action = "Adjust",
            Reason = "Điều chỉnh số lượng theo nhu cầu thực tế.",
            EmployeeNote = "Đơn của bạn đã được cập nhật trước khi chốt kỳ.",
            RowVersion = rowVersion,
            IdempotencyKey = $"manager-adjust-{Guid.NewGuid():N}",
            Items = [new() { VppId = vppId, Qty = qty }]
        };

    private sealed record Seed(Guid OrderId, Guid SeriesId, Guid VppId, byte[] RowVersion);
}
