using System.Reflection;
using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Features.Reports;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class ReportsControllerTests
{
    [Fact]
    public void GetInsights_UsesDedicatedRateLimitPolicy()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.GetInsights));

        var attribute = Assert.Single(method!.GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal("report-insights", attribute.PolicyName);
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("csv")]
    [InlineData("xlsx")]
    [InlineData("pdf")]
    [InlineData("insights")]
    public async Task Endpoint_MissingIdentityScope_ReturnsUnauthorized(string endpoint)
    {
        var reportService = new Mock<IReportService>();
        var insightService = new Mock<IReportInsightService>();
        var permissionService = new Mock<IPermissionService>();
        var controller = CreateController(
            reportService.Object,
            insightService.Object,
            permissionService.Object);

        var result = await InvokeEndpointAsync(controller, endpoint, ReportScopes.Own);

        Assert.IsType<UnauthorizedResult>(result);
        reportService.VerifyNoOtherCalls();
        insightService.VerifyNoOtherCalls();
        permissionService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("csv")]
    [InlineData("xlsx")]
    [InlineData("pdf")]
    [InlineData("insights")]
    public async Task Endpoint_MissingScopePermission_ReturnsForbid(string endpoint)
    {
        var reportService = new Mock<IReportService>();
        var insightService = new Mock<IReportInsightService>();
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(service => service.HasPermissionAsync(
                It.IsAny<ClaimsPrincipal>(),
                Permissions.ReportViewDepartment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = CreateController(
            reportService: reportService.Object,
            insightService: insightService.Object,
            permissionService: permissionService.Object,
            claims: IdentityClaims());

        var result = await InvokeEndpointAsync(controller, endpoint, ReportScopes.Department);

        Assert.IsType<ForbidResult>(result);
        reportService.VerifyNoOtherCalls();
        insightService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("csv")]
    [InlineData("xlsx")]
    [InlineData("pdf")]
    [InlineData("insights")]
    public async Task Endpoint_InvalidScope_ReturnsForbidWithoutPermissionLookup(string endpoint)
    {
        var reportService = new Mock<IReportService>();
        var insightService = new Mock<IReportInsightService>();
        var permissionService = new Mock<IPermissionService>();
        var controller = CreateController(
            reportService.Object,
            insightService.Object,
            permissionService.Object,
            IdentityClaims());

        var result = await InvokeEndpointAsync(controller, endpoint, "unknown");

        Assert.IsType<ForbidResult>(result);
        reportService.VerifyNoOtherCalls();
        insightService.VerifyNoOtherCalls();
        permissionService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ReportScopes.Own, Permissions.ReportViewOwn)]
    [InlineData(ReportScopes.Department, Permissions.ReportViewDepartment)]
    [InlineData(ReportScopes.All, Permissions.ReportViewAll)]
    public async Task GetSummary_AllowedScope_UsesClaimDerivedContext(string scope, string permission)
    {
        var expected = new ReportSummaryResDTO { Scope = scope, Year = 2026, Month = 7 };
        var reportService = new Mock<IReportService>();
        reportService
            .Setup(service => service.GetSummaryAsync(
                It.Is<ReportQueryContext>(query =>
                    query.Scope == scope
                    && query.UserId == 5615
                    && query.DepartmentCode == "IT"
                    && query.MemberCompanyCode == "77500"
                    && query.Year == 2026
                    && query.Month == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var permissionService = AllowPermission(permission);
        var controller = CreateController(
            reportService: reportService.Object,
            permissionService: permissionService.Object,
            claims: IdentityClaims());

        var result = await controller.GetSummary(
            scope,
            2026,
            7,
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        permissionService.Verify(service => service.HasPermissionAsync(
            controller.User,
            permission,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportEndpoints_ReturnServiceFileContracts()
    {
        var csv = new ReportExportResult([1, 2], "report.csv", "text/csv");
        var workbook = new ReportExportResult(
            [3, 4],
            "report.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var pdf = new ReportExportResult([5, 6], "report.pdf", "application/pdf");
        var reportService = new Mock<IReportService>();
        reportService
            .Setup(service => service.ExportCsvAsync(
                It.Is<ReportQueryContext>(query => IsAllScopeQuery(query, 2026, 7)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(csv);
        reportService
            .Setup(service => service.ExportWorkbookAsync(
                It.Is<ReportQueryContext>(query => IsAllScopeQuery(query, 2026, 7)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(workbook);
        reportService
            .Setup(service => service.ExportPdfAsync(
                It.Is<ReportQueryContext>(query => IsAllScopeQuery(query, 2026, 7)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdf);
        var controller = CreateController(
            reportService: reportService.Object,
            permissionService: AllowPermission(Permissions.ReportViewAll).Object,
            claims: IdentityClaims());
        var cancellationToken = TestContext.Current.CancellationToken;

        var csvResult = await controller.Export(ReportScopes.All, 2026, 7, cancellationToken);
        var workbookResult = await controller.ExportWorkbook(ReportScopes.All, 2026, 7, cancellationToken);
        var pdfResult = await controller.ExportPdf(ReportScopes.All, 2026, 7, cancellationToken);

        AssertFile(csvResult, csv);
        AssertFile(workbookResult, workbook);
        AssertFile(pdfResult, pdf);
    }

    [Fact]
    public async Task GetInsights_UsesAuthorizedSummaryAndLanguage()
    {
        var summary = new ReportSummaryResDTO { Scope = ReportScopes.Own };
        var insight = new ReportInsightResDTO { Summary = "Stable demand" };
        var reportService = new Mock<IReportService>();
        reportService
            .Setup(service => service.GetSummaryAsync(
                It.Is<ReportQueryContext>(query =>
                    query.Scope == ReportScopes.Own
                    && query.UserId == 5615
                    && query.DepartmentCode == "IT"
                    && query.MemberCompanyCode == "77500"
                    && query.Year == null
                    && query.Month == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        var insightService = new Mock<IReportInsightService>();
        insightService
            .Setup(service => service.GenerateAsync(summary, "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(insight);
        var controller = CreateController(
            reportService.Object,
            insightService.Object,
            AllowPermission(Permissions.ReportViewOwn).Object,
            IdentityClaims());

        var result = await controller.GetInsights(
            language: "en",
            cancellationToken: TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(insight, ok.Value);
        insightService.Verify(service => service.GenerateAsync(
            summary,
            "en",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IPermissionService> AllowPermission(string permission)
    {
        var service = new Mock<IPermissionService>();
        service
            .Setup(item => item.HasPermissionAsync(
                It.IsAny<ClaimsPrincipal>(),
                permission,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return service;
    }

    private static ReportsController CreateController(
        IReportService? reportService = null,
        IReportInsightService? insightService = null,
        IPermissionService? permissionService = null,
        params Claim[] claims)
    {
        var controller = new ReportsController(
            reportService ?? Mock.Of<IReportService>(),
            insightService ?? Mock.Of<IReportInsightService>(),
            permissionService ?? Mock.Of<IPermissionService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
        return controller;
    }

    private static Claim[] IdentityClaims() =>
    [
        new Claim("UserID", "5615"),
        new Claim(AppClaimTypes.DepartmentCode, "IT"),
        new Claim("MemberCompanyCode", "77500")
    ];

    private static Task<IActionResult> InvokeEndpointAsync(
        ReportsController controller,
        string endpoint,
        string scope) =>
        endpoint switch
        {
            "summary" => controller.GetSummary(scope, cancellationToken: TestContext.Current.CancellationToken),
            "csv" => controller.Export(scope, cancellationToken: TestContext.Current.CancellationToken),
            "xlsx" => controller.ExportWorkbook(scope, cancellationToken: TestContext.Current.CancellationToken),
            "pdf" => controller.ExportPdf(scope, cancellationToken: TestContext.Current.CancellationToken),
            "insights" => controller.GetInsights(scope, cancellationToken: TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null)
        };

    private static void AssertFile(IActionResult actionResult, ReportExportResult expected)
    {
        var file = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal(expected.Content, file.FileContents);
        Assert.Equal(expected.ContentType, file.ContentType);
        Assert.Equal(expected.FileName, file.FileDownloadName);
    }

    private static bool IsAllScopeQuery(ReportQueryContext query, int year, int month) =>
        query.Scope == ReportScopes.All
        && query.UserId == 5615
        && query.DepartmentCode == "IT"
        && query.MemberCompanyCode == "77500"
        && query.Year == year
        && query.Month == month;
}
