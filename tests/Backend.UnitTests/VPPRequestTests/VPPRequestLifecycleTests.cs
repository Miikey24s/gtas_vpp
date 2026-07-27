using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
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

public sealed class VPPRequestLifecycleTests
{
    private const int RequesterId = 5615;
    private const int ApproverId = 9001;
    private const string Department = "IT";
    private const string Company = "77500";
    private static readonly DateTime OpenPeriodNow = new(2026, 4, 10, 9, 0, 0);

    [Fact]
    public async Task Update_CreatesImmutableRevision_AndHistoryKeepsBothVersions()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId, qty: 1);
        var rowVersion = await SetRowVersionAsync(context, original.Id);

        var update = CreateUpdate(
            original.Id, vppId, qty: 4, description: "Updated request");
        update.RowVersion = rowVersion;
        var updated = await service.UpdateOrderAsync(update);

        var revisions = await context.Set<VppRequest>()
            .Include(x => x.RequestDetails)
            .OrderBy(x => x.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, revisions.Count);
        Assert.False(revisions[0].IsCurrentRevision);
        Assert.Equal(updated.Id, revisions[0].SupersededByRequestId);
        Assert.Equal(1, Assert.Single(revisions[0].RequestDetails).Qty);
        Assert.True(revisions[1].IsCurrentRevision);
        Assert.Equal(2, revisions[1].RevisionNumber);
        Assert.Equal(original.Id, revisions[1].SupersedesRequestId);
        Assert.Equal(original.RequestSeriesId, revisions[1].RequestSeriesId);
        Assert.Equal(4, Assert.Single(revisions[1].RequestDetails).Qty);

        var history = Assert.IsType<gtas_vpp_shared.DTOs.Res.VPP.VppRequestHistoryResDTO>(
            await service.GetOrderHistoryAsync(original.Id));
        Assert.Equal(updated.Id, history.CurrentRequestId);
        Assert.Equal(new[] { 1, 2 }, history.Revisions.Select(x => x.RevisionNumber));
        Assert.False(history.Revisions[0].CanEdit);
        Assert.False(history.Revisions[0].CanCancel);
        Assert.True(history.Revisions[1].CanEdit);
        Assert.Equal(new[] { "CREATE", "UPDATE" }, history.Timeline.Select(x => x.Action));
    }

    [Fact]
    public async Task Cancel_PreservesHistory_AndCancelledCurrentRevisionCanBeReplaced()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId, qty: 2);
        var originalRowVersion = await SetRowVersionAsync(context, original.Id);

        await service.CancelOrderAsync(original.Id, RequesterId, new VppRequestCancelReqDTO
        {
            Reason = "No longer required",
            IdempotencyKey = "cancel-lifecycle-1",
            RowVersion = originalRowVersion
        });

        var cancelled = await context.Set<VppRequest>()
            .Include(x => x.RequestDetails)
            .SingleAsync(x => x.IsCurrentRevision);
        Assert.Equal((int)VPPStatus.Cancelled, cancelled.Status);
        Assert.Equal(2, cancelled.RevisionNumber);
        Assert.Equal(original.Id, cancelled.SupersedesRequestId);
        Assert.Equal(RequesterId, cancelled.CancelledById);
        Assert.Equal("No longer required", cancelled.CancelReason);
        Assert.Equal(2, Assert.Single(cancelled.RequestDetails).Qty);

        var cancelledHistory = Assert.IsType<gtas_vpp_shared.DTOs.Res.VPP.VppRequestHistoryResDTO>(
            await service.GetOrderHistoryAsync(original.Id));
        Assert.Equal(2, cancelledHistory.Revisions.Count);
        Assert.False(cancelledHistory.Revisions[0].CanEdit);
        Assert.False(cancelledHistory.Revisions[0].CanCancel);
        Assert.True(cancelledHistory.Revisions[1].CanReplace);
        Assert.Equal(new[] { "CREATE", "CANCEL" }, cancelledHistory.Timeline.Select(x => x.Action));

        var cancelledRowVersion = await SetRowVersionAsync(context, cancelled.Id);
        var replacementCommand = CreateUpdate(
            cancelled.Id, vppId, qty: 3, description: "Replacement request");
        replacementCommand.RowVersion = cancelledRowVersion;
        var replacement = await service.UpdateOrderAsync(replacementCommand);
        var finalHistory = Assert.IsType<gtas_vpp_shared.DTOs.Res.VPP.VppRequestHistoryResDTO>(
            await service.GetOrderHistoryAsync(original.Id));
        Assert.Equal(replacement.Id, finalHistory.CurrentRequestId);
        Assert.Equal(3, finalHistory.Revisions.Count);
        Assert.False(finalHistory.Revisions.Single(x => x.RevisionNumber == 2).CanReplace);
        Assert.True(finalHistory.Revisions.Single(x => x.RevisionNumber == 3).CanEdit);
        Assert.Equal(new[] { "CREATE", "CANCEL", "REPLACE" }, finalHistory.Timeline.Select(x => x.Action));
    }

    [Fact]
    public async Task CancelledRegular_KeepsPeriodSlot_AndOnlyRevisionReplacementIsAvailable()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await SeedRequesterAsync(context);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, original.Id);

        await service.CancelOrderAsync(original.Id, RequesterId, new VppRequestCancelReqDTO
        {
            Reason = "No longer required",
            RowVersion = rowVersion
        });

        var duplicate = new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Description = "Attempted second series",
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId, Qty = 1, Description = "Paper" }
            }
        };
        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateOrderAsync(duplicate, RequesterId, Department, Company));

        var periodInfo = await service.GetCurrentPeriodInfoAsync(RequesterId);
        Assert.True(periodInfo.HasCurrentPeriodOrder);
        Assert.False(periodInfo.CanCreateOrder);
        Assert.False(periodInfo.CanCreateAdditional);
        Assert.Contains("ineligible", periodInfo.CanCreateAdditionalReason);

        var history = Assert.IsType<gtas_vpp_shared.DTOs.Res.VPP.VppRequestHistoryResDTO>(
            await service.GetOrderHistoryAsync(original.Id));
        var cancelled = Assert.Single(history.Revisions, x => x.IsCurrentRevision);
        Assert.Equal((int)VPPStatus.Cancelled, cancelled.Status);
        Assert.True(cancelled.CanReplace);
        Assert.False(cancelled.CanEdit);
        Assert.False(cancelled.CanCancel);
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_ConflictsWithoutCreatingRevision()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId);
        var header = await context.Set<VppRequest>().SingleAsync(x => x.Id == original.Id);
        header.RowVersion = new byte[] { 1, 2, 3 };
        await context.SaveChangesAsync();
        var request = CreateUpdate(original.Id, vppId, qty: 2, description: "Stale update");
        request.RowVersion = new byte[] { 9, 9, 9 };

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateOrderAsync(request));

        Assert.Contains("changed while you were editing", exception.Message);
        Assert.Single(context.Set<VppRequest>());
        Assert.True(header.IsCurrentRevision);
    }

    [Fact]
    public async Task Update_IdempotencyRetryReturnsSameRevision_AndDifferentPayloadConflicts()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, original.Id);
        var command = CreateUpdate(original.Id, vppId, qty: 2, description: "Idempotent update");
        command.IdempotencyKey = "update-retry-1";
        command.RowVersion = rowVersion;

        var first = await service.UpdateOrderAsync(command);
        var replay = await service.UpdateOrderAsync(command);

        Assert.Equal(first.Id, replay.Id);
        Assert.Equal(2, await context.Set<VppRequest>().CountAsync());
        Assert.Single(context.Set<RequestLog>(), x => x.Action == "UPDATE");

        var changedPayload = CreateUpdate(
            original.Id, vppId, qty: 3, description: "Different update payload");
        changedPayload.IdempotencyKey = command.IdempotencyKey;
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.UpdateOrderAsync(changedPayload));
        Assert.Contains("different update", exception.Message);
        Assert.Equal(2, await context.Set<VppRequest>().CountAsync());
    }

    [Fact]
    public async Task Cancel_IdempotencyRetryDoesNotCreateRevision_AndDifferentPayloadConflicts()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var original = await CreateRegularAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, original.Id);
        var command = new VppRequestCancelReqDTO
        {
            Reason = "No longer required",
            IdempotencyKey = "cancel-retry-1",
            RowVersion = rowVersion
        };

        await service.CancelOrderAsync(original.Id, RequesterId, command);
        await service.CancelOrderAsync(original.Id, RequesterId, command);

        Assert.Equal(2, await context.Set<VppRequest>().CountAsync());
        Assert.Single(context.Set<RequestLog>(), x => x.Action == "CANCEL");

        var changedPayload = new VppRequestCancelReqDTO
        {
            Reason = "A different cancellation reason",
            IdempotencyKey = command.IdempotencyKey
        };
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.CancelOrderAsync(original.Id, RequesterId, changedPayload));
        Assert.Contains("different command", exception.Message);
        Assert.Equal(2, await context.Set<VppRequest>().CountAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SupplementDecision_RequesterCannotDecideOwnRequest(bool approve)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var supplement = await CreateSupplementAsync(service, vppId);

        Task Decision() => approve
            ? service.ApproveAdditionalOrderAsync(
                supplement.Id, RequesterId, new byte[] { 1 }, "self-approve", Department, false, Company)
            : service.RejectAdditionalOrderAsync(
                supplement.Id, RequesterId, "Rejected by requester", new byte[] { 1 },
                "self-reject", Department, false, Company);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(Decision);
        Assert.Contains("own supplement", exception.Message);
        Assert.Equal((int)VPPStatus.Pending,
            (await context.Set<VppRequest>().SingleAsync(x => x.Id == supplement.Id)).Status);
    }

    [Theory]
    [InlineData("HR", false, "77500")]
    [InlineData("IT", true, "88000")]
    public async Task Approve_OutsideDepartmentOrCompanyScope_IsBlocked(
        string actorDepartment,
        bool canApproveCrossDepartment,
        string actorCompany)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var supplement = await CreateSupplementAsync(service, vppId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAdditionalOrderAsync(
                supplement.Id, ApproverId, new byte[] { 1 }, "scope-check", actorDepartment,
                canApproveCrossDepartment, actorCompany));

        Assert.Equal((int)VPPStatus.Pending,
            (await context.Set<VppRequest>().SingleAsync(x => x.Id == supplement.Id)).Status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SupplementDecision_WithStaleRowVersion_IsBlocked(bool approve)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var supplement = await CreateSupplementAsync(service, vppId);
        var header = await context.Set<VppRequest>().SingleAsync(x => x.Id == supplement.Id);
        header.RowVersion = new byte[] { 1, 2, 3 };
        await context.SaveChangesAsync();
        var stale = new byte[] { 7, 8, 9 };

        Task Decision() => approve
            ? service.ApproveAdditionalOrderAsync(
                supplement.Id, ApproverId, stale, "stale-approve", Department, false, Company)
            : service.RejectAdditionalOrderAsync(
                supplement.Id, ApproverId, "Valid rejection reason", stale,
                "stale-reject", Department, false, Company);

        await Assert.ThrowsAsync<ConflictException>(Decision);
        Assert.Equal((int)VPPStatus.Pending, header.Status);
    }

    [Fact]
    public async Task Approve_WhenApprovedQuotaIsFull_IsBlocked()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var pending = await CreateSupplementAsync(service, vppId);
        var pendingHeader = await context.Set<VppRequest>()
            .SingleAsync(x => x.Id == pending.Id);
        pendingHeader.RowVersion = new byte[] { 4, 5, 6 };

        context.Set<VppRequest>().AddRange(Enumerable.Range(0, 3).Select(index =>
            CreateApprovedSupplement(pendingHeader, index + 2)));
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ApproveAdditionalOrderAsync(
                pending.Id, ApproverId, pendingHeader.RowVersion, "quota-check", Department, false, Company));

        Assert.Contains("quota is full", exception.Message);
        Assert.Equal((int)VPPStatus.Pending, pendingHeader.Status);
    }

    [Fact]
    public async Task CreateSupplement_WithoutCurrentBase_IsBlocked()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CreateOrderAsync(new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                IsAdditionalOrder = true,
                BaseRequestId = Guid.NewGuid(),
                SupplementReason = "Needed for a new employee",
                Items =
                [
                    new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1, Description = "Paper" }
                ]
            }, RequesterId, Department, Company));

        Assert.Contains("current regular order", exception.Message);
        Assert.Empty(context.Set<VppRequest>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abcd")]
    public async Task CreateSupplement_WithInvalidReason_IsBlocked(string? reason)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var regular = await CreateRegularAsync(service, vppId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateSupplementForBaseAsync(service, vppId, regular.Id, reason));

        Assert.Contains("5 to 500", exception.Message);
        Assert.Single(context.Set<VppRequest>());
    }

    [Fact]
    public async Task CreateSupplement_AfterSubmissionClosed_IsBlocked()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var regular = await CreateRegularAsync(service, vppId);
        var period = Assert.Single(context.Set<VppPeriod>());
        period.State = VppPeriodState.SubmissionClosed;
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateSupplementForBaseAsync(
                service, vppId, regular.Id, "Needed after the deadline"));

        Assert.Contains("submission window", exception.Message);
        Assert.Single(context.Set<VppRequest>());
    }

    [Theory]
    [InlineData(VppPeriodState.SubmissionClosed, true)]
    [InlineData(VppPeriodState.SubmissionClosed, false)]
    [InlineData(VppPeriodState.Pricing, true)]
    [InlineData(VppPeriodState.Pricing, false)]
    [InlineData(VppPeriodState.Settled, true)]
    [InlineData(VppPeriodState.Settled, false)]
    public async Task RegularMutation_AfterPeriodLeavesOpen_IsBlocked(
        VppPeriodState state,
        bool update)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var regular = await CreateRegularAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, regular.Id);
        var period = Assert.Single(context.Set<VppPeriod>());
        period.State = state;
        await context.SaveChangesAsync();

        Task Mutation()
        {
            if (!update)
            {
                return service.CancelOrderAsync(regular.Id, RequesterId, new VppRequestCancelReqDTO
                {
                    RowVersion = rowVersion,
                    Reason = "No longer required"
                });
            }

            var command = CreateUpdate(
                regular.Id, vppId, qty: 2, description: "Late replacement");
            command.RowVersion = rowVersion;
            return service.UpdateOrderAsync(command);
        }

        await Assert.ThrowsAsync<ConflictException>(Mutation);
        Assert.Single(context.Set<VppRequest>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SupplementDecision_AfterApprovalDeadline_IsBlocked(bool approve)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var supplement = await CreateSupplementAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, supplement.Id);
        var period = Assert.Single(context.Set<VppPeriod>());
        period.State = VppPeriodState.SubmissionClosed;
        period.SupplementApprovalDeadlineUtc =
            PeriodCalculator.NormalizeNowUtc(OpenPeriodNow).AddMinutes(-1);
        await context.SaveChangesAsync();

        Task Decision() => approve
            ? service.ApproveAdditionalOrderAsync(
                supplement.Id, ApproverId, rowVersion, "late-approve",
                Department, false, Company)
            : service.RejectAdditionalOrderAsync(
                supplement.Id, ApproverId, "Late rejection", rowVersion,
                "late-reject", Department, false, Company);

        await Assert.ThrowsAsync<ConflictException>(Decision);
        Assert.Equal((int)VPPStatus.Pending,
            (await context.Set<VppRequest>().SingleAsync(x => x.Id == supplement.Id)).Status);
    }

    [Fact]
    public async Task RejectedAndCancelledSupplements_ConsumeAttemptsButNotApprovedQuota()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        await SeedRequesterAsync(context);
        var service = CreateService(context);
        var regular = await CreateRegularAsync(service, vppId);

        for (var attempt = 1; attempt <= 6; attempt++)
        {
            var supplement = await CreateSupplementForBaseAsync(
                service,
                vppId,
                regular.Id,
                $"Attempt {attempt} for changing office needs",
                $"supplement-attempt-{attempt}");
            var rowVersion = await SetRowVersionAsync(context, supplement.Id);
            if (attempt % 2 == 0)
            {
                await service.CancelOrderAsync(
                    supplement.Id,
                    RequesterId,
                    new VppRequestCancelReqDTO
                    {
                        RowVersion = rowVersion,
                        Reason = "Requester cancelled this attempt",
                        IdempotencyKey = $"cancel-attempt-{attempt}"
                    });
            }
            else
            {
                await service.RejectAdditionalOrderAsync(
                    supplement.Id,
                    ApproverId,
                    "Rejected for this attempt",
                    rowVersion,
                    $"reject-attempt-{attempt}",
                    Department,
                    false,
                    Company);
            }
        }

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateSupplementForBaseAsync(
                service,
                vppId,
                regular.Id,
                "Seventh attempt must be blocked",
                "supplement-attempt-7"));
        Assert.Contains("attempt limit", exception.Message);

        var periodInfo = await service.GetCurrentPeriodInfoAsync(RequesterId);
        Assert.Equal(0, periodInfo.ApprovedSupplementCount);
        Assert.Equal(6, periodInfo.SupplementAttemptCount);
        Assert.Equal(0, periodInfo.RemainingSupplementAttempts);
        Assert.False(periodInfo.CanCreateAdditional);
        Assert.Equal(6, await context.Set<VppRequest>()
            .Where(x => x.IsAdditionalOrder && x.IsCurrentRevision && !x.IsDeleted)
            .Select(x => x.RequestSeriesId)
            .Distinct()
            .CountAsync());
    }

    [Fact]
    public async Task RejectDecision_IdempotencyReplayIsStable_AndDifferentReasonConflicts()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var supplement = await CreateSupplementAsync(service, vppId);
        var rowVersion = await SetRowVersionAsync(context, supplement.Id);

        await service.RejectAdditionalOrderAsync(
            supplement.Id, ApproverId, "Budget exceeded", rowVersion,
            "reject-replay-1", Department, false, Company);
        await service.RejectAdditionalOrderAsync(
            supplement.Id, ApproverId, "Budget exceeded", rowVersion,
            "reject-replay-1", Department, false, Company);

        Assert.Single(context.Set<RequestLog>(), x => x.Action == "REJECT");
        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.RejectAdditionalOrderAsync(
                supplement.Id, ApproverId, "Different rejection reason", rowVersion,
                "reject-replay-1", Department, false, Company));
        Assert.Contains("different supplement decision", exception.Message);
    }

    [Fact]
    public async Task MutationWithoutRowVersion_IsRejectedAtServiceBoundary()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);
        var regular = await CreateRegularAsync(service, vppId);
        var update = CreateUpdate(regular.Id, vppId, qty: 2, description: "Missing token");

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateOrderAsync(update));

        Assert.Contains("RowVersion is required", exception.Message);
        Assert.Single(context.Set<VppRequest>());
    }

    [Fact]
    public async Task PendingSupplementListing_IsDepartmentScopedUnlessActorHasViewAll()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var service = CreateService(context);

        var itRegular = await service.CreateOrderAsync(
            new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                Description = "IT regular",
                Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1 }]
            },
            5615,
            "IT",
            Company);
        await service.CreateOrderAsync(
            new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                IsAdditionalOrder = true,
                BaseRequestId = itRegular.Id,
                SupplementReason = "IT supplement for scope test",
                Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1 }]
            },
            5615,
            "IT",
            Company);

        var hrRegular = await service.CreateOrderAsync(
            new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                Description = "HR regular",
                Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1 }]
            },
            7777,
            "HR",
            Company);
        await service.CreateOrderAsync(
            new VppRequestCreateReqDTO
            {
                Year = 2026,
                Month = 4,
                IsAdditionalOrder = true,
                BaseRequestId = hrRegular.Id,
                SupplementReason = "HR supplement for scope test",
                Items = [new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1 }]
            },
            7777,
            "HR",
            Company);

        var departmentRows = await service.GetPendingAdditionalOrdersAsync(
            Company,
            "IT",
            canViewAllDepartments: false);
        var allRows = await service.GetPendingAdditionalOrdersAsync(
            Company,
            "IT",
            canViewAllDepartments: true);

        Assert.Single(departmentRows);
        Assert.Equal("IT", departmentRows[0].DepartmentCode);
        Assert.Equal(2, allRows.Count);
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
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static async Task SeedRequesterAsync(VPPContext context)
    {
        context.Set<AppUser>().Add(new AppUser
        {
            Id = RequesterId,
            UserName = "requester",
            NormalizedUserName = "REQUESTER",
            FullName = "Test Requester",
            MemberCompanyCode = 77500,
            CreatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(OpenPeriodNow, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
    }

    private static async Task<gtas_vpp_shared.DTOs.Res.VPP.VppRequestResDTO> CreateRegularAsync(
        VPPRequestService service,
        Guid vppId,
        int qty = 1)
        => await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Description = "Regular request",
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId, Qty = qty, Description = "Paper" }
            }
        }, RequesterId, Department, Company);

    private static async Task<gtas_vpp_shared.DTOs.Res.VPP.VppRequestResDTO> CreateSupplementAsync(
        VPPRequestService service,
        Guid vppId)
    {
        var regular = await CreateRegularAsync(service, vppId);
        return await service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            IsAdditionalOrder = true,
            BaseRequestId = regular.Id,
            SupplementReason = "Needed for a new employee",
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId, Qty = 1, Description = "Paper" }
            }
        }, RequesterId, Department, Company);
    }

    private static Task<gtas_vpp_shared.DTOs.Res.VPP.VppRequestResDTO>
        CreateSupplementForBaseAsync(
            VPPRequestService service,
            Guid vppId,
            Guid baseRequestId,
            string? reason,
            string? idempotencyKey = null)
        => service.CreateOrderAsync(new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            IsAdditionalOrder = true,
            BaseRequestId = baseRequestId,
            SupplementReason = reason,
            IdempotencyKey = idempotencyKey,
            Items =
            [
                new VppRequestDetailItemReqDTO { VppId = vppId, Qty = 1, Description = "Paper" }
            ]
        }, RequesterId, Department, Company);

    private static VppRequestUpdateReqDTO CreateUpdate(
        Guid id,
        Guid vppId,
        int qty,
        string description)
        => new()
        {
            Id = id,
            Description = description,
            UpdatedByUserId = RequesterId,
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = vppId, Qty = qty, Description = "Paper" }
            }
        };

    private static async Task<byte[]> SetRowVersionAsync(VPPContext context, Guid requestId)
    {
        var rowVersion = Guid.NewGuid().ToByteArray();
        var header = await context.Set<VppRequest>()
            .SingleAsync(x => x.Id == requestId);
        header.RowVersion = rowVersion;
        await context.SaveChangesAsync();
        return rowVersion;
    }

    private static VppRequest CreateApprovedSupplement(
        VppRequest pending,
        int attempt)
        => new()
        {
            Id = Guid.NewGuid(),
            Year = pending.Year,
            Month = pending.Month,
            PeriodId = pending.PeriodId,
            RequestSeriesId = Guid.NewGuid(),
            RevisionNumber = 1,
            IsCurrentRevision = true,
            VppCode = $"VPP-APPROVED-{attempt}",
            Status = (int)VPPStatus.Approved,
            IsAdditionalOrder = true,
            BaseRequestId = pending.BaseRequestId,
            BaseRequestSeriesId = pending.BaseRequestSeriesId,
            SupplementSequence = attempt,
            SupplementAttemptNumber = attempt,
            SupplementReason = "Previously approved supplement",
            DepartmentCode = pending.DepartmentCode,
            MemberCompanyCode = pending.MemberCompanyCode,
            CreatedByUserId = pending.CreatedByUserId,
            CreatedAtUtc = OpenPeriodNow.AddDays(-attempt),
            UpdatedByUserId = ApproverId,
            UpdatedAtUtc = OpenPeriodNow.AddDays(-attempt),
            SubmittedDate = OpenPeriodNow.AddDays(-attempt),
            ApprovedById = ApproverId,
            ApprovedAt = OpenPeriodNow.AddDays(-attempt)
        };
}
