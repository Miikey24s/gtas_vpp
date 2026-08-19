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

    [Fact]
    public async Task DownloadGenericTemplate_ReturnsDefaultPriceListWorkbook()
    {
        var priceListId = Guid.NewGuid();
        var expected = new PriceListExportResult(
            [0x50, 0x4B, 0x03, 0x04],
            "GTAS-VPP-Bang-gia-DEFAULT.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var priceLists = new Mock<IPriceListService>();
        priceLists.Setup(service => service.ListAsync(false))
            .ReturnsAsync([
                new PriceListResDTO
                {
                    Id = priceListId,
                    PriceListCode = "DEFAULT",
                    PriceListName = "Mặc định",
                    IsDefault = true,
                    Status = "Published"
                }
            ]);
        var exports = new Mock<IPriceListExportService>();
        exports.Setup(service => service.ExportExcelAsync(priceListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(Mock.Of<IPriceListImportService>(), exports.Object, priceLists.Object);

        var result = await controller.DownloadGenericImportTemplate(TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expected.Content, file.FileContents);
        Assert.Equal(expected.FileName, file.FileDownloadName);
        priceLists.VerifyAll();
        exports.VerifyAll();
    }

    [Fact]
    public async Task DownloadSelectedTemplate_ReturnsExactSelectedPriceListWorkbook()
    {
        var priceListId = Guid.NewGuid();
        var expected = new PriceListExportResult(
            [0x50, 0x4B, 0x03, 0x04],
            "GTAS-VPP-Bang-gia-BG-01.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var exports = new Mock<IPriceListExportService>();
        exports.Setup(service => service.ExportExcelAsync(priceListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(Mock.Of<IPriceListImportService>(), exports.Object);

        var result = await controller.DownloadImportTemplate(priceListId, TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expected.Content, file.FileContents);
        Assert.Equal(expected.FileName, file.FileDownloadName);
        exports.VerifyAll();
    }

    [Fact]
    public async Task ExportExcel_ReturnsSelectedPriceListWorkbook()
    {
        var priceListId = Guid.NewGuid();
        var expected = new PriceListExportResult(
            [0x50, 0x4B, 0x03, 0x04],
            "GTAS-VPP-Bang-gia-BG-01.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var exports = new Mock<IPriceListExportService>();
        exports.Setup(service => service.ExportExcelAsync(priceListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = Controller(Mock.Of<IPriceListImportService>(), exports.Object);

        var result = await controller.ExportExcel(priceListId, TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expected.Content, file.FileContents);
        Assert.Equal(expected.FileName, file.FileDownloadName);
        exports.VerifyAll();
    }

    private static VPPPriceListController Controller(
        IPriceListImportService importService,
        IPriceListExportService? exportService = null,
        IPriceListService? priceListService = null)
    {
        var controller = new VPPPriceListController(
            priceListService ?? Mock.Of<IPriceListService>(),
            Mock.Of<IPriceBookWorkflowService>(),
            importService,
            exportService);
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
