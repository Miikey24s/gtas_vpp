using System.Security.Claims;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Notifications;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class PostSettlementOrderCorrectionsControllerTests
{
    [Fact]
    public async Task Create_notifies_other_period_managers_with_current_dashboard_route()
    {
        var correctionId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var corrections = new Mock<IPostSettlementOrderCorrectionService>();
        var notifications = new Mock<IAppNotificationService>();
        corrections.Setup(x => x.CreateAsync(
                "77500",
                5615,
                It.IsAny<PostSettlementOrderCorrectionCreateReqDTO>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostSettlementOrderCorrectionResDTO
            {
                Id = correctionId,
                PeriodId = periodId,
                MemberCompanyCode = "77500",
                RequestCode = "VPP-202608-TEST",
                Action = "Adjust"
            });
        notifications.Setup(x => x.GetRecipientsWithPermissionAsync(
                "77500", Permissions.PeriodSettle, It.IsAny<CancellationToken>()))
            .ReturnsAsync([5615, 5616]);
        notifications.Setup(x => x.PublishAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(corrections.Object, notifications.Object);

        var response = await controller.Create(
            new PostSettlementOrderCorrectionCreateReqDTO(),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        notifications.Verify(x => x.PublishAsync(
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 5616 })),
            "77500",
            "period.order-correction.pending",
            It.IsAny<string>(),
            It.IsAny<string>(),
            $"/dashboard?tab=5&periodTab=periods&periodId={periodId}",
            correctionId.ToString("N"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_notifies_order_owner_with_history_deep_link()
    {
        var correctionId = Guid.NewGuid();
        var resultRequestId = Guid.NewGuid();
        var corrections = new Mock<IPostSettlementOrderCorrectionService>();
        var notifications = new Mock<IAppNotificationService>();
        corrections.Setup(x => x.ConfirmAsync(
                correctionId,
                5615,
                It.IsAny<PostSettlementOrderCorrectionDecisionReqDTO>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostSettlementOrderCorrectionResDTO
            {
                Id = correctionId,
                MemberCompanyCode = "77500",
                RequestId = Guid.NewGuid(),
                ResultRequestId = resultRequestId,
                RequestOwnerUserId = 100,
                RequestCode = "VPP-202608-TEST",
                Action = "Adjust",
                Status = "Confirmed",
                EmployeeNote = "Đơn đã được cập nhật theo biên bản."
            });
        notifications.Setup(x => x.PublishAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(corrections.Object, notifications.Object);

        var response = await controller.Confirm(
            correctionId,
            new PostSettlementOrderCorrectionDecisionReqDTO(),
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(response);
        notifications.Verify(x => x.PublishAsync(
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 100 })),
            "77500",
            "period.order-correction.confirmed",
            It.IsAny<string>(),
            It.IsAny<string>(),
            $"/dashboard?tab=1&orderId={resultRequestId}",
            $"{correctionId:N}:Confirmed",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PostSettlementOrderCorrectionsController CreateController(
        IPostSettlementOrderCorrectionService corrections,
        IAppNotificationService notifications)
    {
        var controller = new PostSettlementOrderCorrectionsController(corrections, notifications);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("UserID", "5615"),
                    new Claim("MemberCompanyCode", "77500")
                ], "test"))
            }
        };
        return controller;
    }
}
