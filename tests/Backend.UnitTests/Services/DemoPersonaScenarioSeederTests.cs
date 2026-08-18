using gtas_vpp_be.Model;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.Services;

public sealed class DemoPersonaScenarioSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesRichIdempotentHistoryForThreeCanonicalPersonas()
    {
        await using var context = CreateContext();
        SeedAccountsAndCatalog(context);
        await context.SaveChangesAsync();
        var options = new DemoPersonaSeedOptions(
            NowUtc: new DateTime(2026, 8, 5, 8, 0, 0, DateTimeKind.Utc),
            AutoResolveUniquePersonas: true);

        await DemoPersonaScenarioSeeder.SeedAsync(context, options);

        var initialRequestIds = await context.Requests
            .OrderBy(request => request.Id)
            .Select(request => request.Id)
            .ToArrayAsync();
        Assert.Equal(24, initialRequestIds.Length);
        Assert.Equal(5, await context.Periods.CountAsync());
        Assert.Equal(33, await context.RequestLogs.CountAsync());
        Assert.DoesNotContain(
            await context.Requests.Select(request => request.Status).ToArrayAsync(),
            status => status == (int)VPPStatus.Pending);

        var periods = await context.Periods.ToDictionaryAsync(period => period.Id);
        Assert.All(periods.Values, period =>
        {
            Assert.Equal(5, (period.SupplementApprovalDeadlineUtc - period.SubmissionDeadlineUtc).TotalDays);
            Assert.Equal(10, (period.PostCloseAdjustmentDeadlineUtc!.Value - period.SubmissionDeadlineUtc).TotalDays);
        });

        var supplements = await context.Requests
            .Where(request => request.IsAdditionalOrder)
            .ToListAsync();
        Assert.All(supplements, request =>
        {
            var period = periods[request.PeriodId!.Value];
            Assert.True(request.SubmittedDate > period.SubmissionDeadlineUtc);
            Assert.True(request.SubmittedDate <= period.SupplementApprovalDeadlineUtc);
            var resolvedAt = request.ApprovedAt ?? request.RejectedAt ?? request.CancelledAt;
            Assert.NotNull(resolvedAt);
            Assert.True(resolvedAt <= period.SupplementApprovalDeadlineUtc);
        });

        foreach (var userId in new[] { 101, 102, 103 })
        {
            var requests = await context.Requests
                .Where(request => request.CreatedByUserId == userId)
                .ToListAsync();
            Assert.Equal(8, requests.Count);
            Assert.Equal(5, requests.Count(request => !request.IsAdditionalOrder));
            Assert.Equal(3, requests.Count(request => request.IsAdditionalOrder));
            Assert.Contains(requests, request => request.Status == (int)VPPStatus.Approved);
            Assert.Contains(requests, request => request.Status == (int)VPPStatus.Rejected);
            Assert.Contains(requests, request => request.Status == (int)VPPStatus.Cancelled);
        }

        await DemoPersonaScenarioSeeder.SeedAsync(context, options);

        Assert.Equal(
            initialRequestIds,
            await context.Requests
                .OrderBy(request => request.Id)
                .Select(request => request.Id)
                .ToArrayAsync());
        Assert.Equal(33, await context.RequestLogs.CountAsync());
    }

    private static VPPMigrationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VPPMigrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new VPPMigrationDbContext(options);
    }

    private static void SeedAccountsAndCatalog(VPPMigrationDbContext context)
    {
        var department = new Department
        {
            Id = Guid.Parse("70000000-0000-0000-0000-000000000001"),
            Code = "DEMO-IT",
            Name = "Phòng Công nghệ thông tin"
        };
        context.Departments.Add(department);

        AddAccount(context, 101, "demo_employee", "Nhân viên Demo", CanonicalRbac.Employee.GroupId, department.Id);
        AddAccount(context, 102, "demo_manager", "Quản lý Demo", CanonicalRbac.Manager.GroupId, department.Id);
        AddAccount(context, 103, "demo_dev", "Quản trị hệ thống Demo", CanonicalRbac.Dev.GroupId, department.Id);

        for (var index = 1; index <= 6; index++)
        {
            var itemId = Guid.Parse($"71000000-0000-0000-0000-{index:X12}");
            context.VppItems.Add(new VppItem
            {
                Id = itemId,
                VppCode = $"DEMO-{index:D3}",
                VppName = $"Mặt hàng demo {index}",
                UomId = Guid.NewGuid(),
                VppCategoryId = Guid.NewGuid()
            });
            context.SupplierProductMappings.Add(new SupplierProductMapping
            {
                Id = Guid.Parse($"72000000-0000-0000-0000-{index:X12}"),
                VppItemId = itemId,
                SupplierId = Guid.NewGuid(),
                PriceListId = Guid.NewGuid(),
                Price = 10_000 + index * 1_000,
                NetPrice = 10_000 + index * 1_000,
                IsDefault = true
            });
        }
    }

    private static void AddAccount(
        VPPMigrationDbContext context,
        int userId,
        string username,
        string fullName,
        Guid groupId,
        Guid departmentId)
    {
        context.Users.Add(new AppUser
        {
            Id = userId,
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            FullName = fullName,
            MemberCompanyCode = CanonicalRbac.DefaultMemberCompanyCode,
            AccountStatus = AppAccountStatus.Active
        });
        context.UserGroupMemberships.Add(new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = userId,
            PermissionGroupId = groupId,
            DepartmentId = departmentId
        });
    }
}
