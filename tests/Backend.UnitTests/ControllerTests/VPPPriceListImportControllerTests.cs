using System.Security.Claims;
using System.Text;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class VPPPriceListImportControllerTests
{
    [Fact]
    public async Task AnalyzeImport_ForwardsFileWithoutCreatingAuditBatch()
    {
        var priceListId = Guid.NewGuid();
        var expected = new PriceListImportAnalysisResDTO { FileName = "bang-gia-ncc.csv" };
        var imports = new Mock<IPriceListImportService>();
        imports.Setup(service => service.AnalyzeAsync(
                priceListId,
                "bang-gia-ncc.csv",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(imports.Object);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Ma noi bo,Gia ban\nA001,100"));
        var file = new FormFile(stream, 0, stream.Length, "file", "bang-gia-ncc.csv");

        var result = await controller.AnalyzeImport(priceListId, file, TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        imports.VerifyAll();
    }

    [Fact]
    public async Task PreviewImport_ForwardsFileAndAuthenticatedUser()
    {
        var priceListId = Guid.NewGuid();
        var expected = new PriceListImportPreviewResDTO { Id = Guid.NewGuid() };
        var imports = new Mock<IPriceListImportService>();
        imports.Setup(service => service.PreviewAsync(
                priceListId,
                "bang-gia.csv",
                It.IsAny<Stream>(),
                It.Is<IReadOnlyDictionary<int, string>?>(mapping => mapping != null
                    && mapping[0] == "ItemCode"
                    && mapping[1] == "UnitPrice"),
                5615,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(imports.Object);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("ItemCode,UnitPrice\nA001,100"));
        var file = new FormFile(stream, 0, stream.Length, "file", "bang-gia.csv");

        var result = await controller.PreviewImport(
            priceListId,
            file,
            "{\"0\":\"ItemCode\",\"1\":\"UnitPrice\"}",
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        imports.VerifyAll();
    }

    [Fact]
    public async Task ConfirmImport_ForwardsBatchAndConcurrencyToken()
    {
        var priceListId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        var expected = new PriceListImportBatchResDTO { Id = batchId, Status = "Completed" };
        var imports = new Mock<IPriceListImportService>();
        imports.Setup(service => service.ConfirmAsync(
                priceListId,
                batchId,
                rowVersion,
                5615,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(imports.Object);

        var result = await controller.ConfirmImport(
            priceListId,
            batchId,
            new PriceListImportConfirmReqDTO { RowVersion = rowVersion },
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        imports.VerifyAll();
    }

    private static VPPPriceListController Controller(IPriceListImportService importService)
    {
        var controller = new VPPPriceListController(
            Mock.Of<IPriceListService>(),
            Mock.Of<IPriceBookWorkflowService>(),
            importService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("UserID", "5615")],
                    "test"))
            }
        };
        return controller;
    }
}
