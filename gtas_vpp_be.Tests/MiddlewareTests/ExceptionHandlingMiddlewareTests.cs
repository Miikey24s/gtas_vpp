using System.Text.Json;
using gtas_vpp_be.Middleware;
using gtas_vpp_be.Service.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.MiddlewareTests;

public class ExceptionHandlingMiddlewareTests
{
    public static IEnumerable<object[]> ExceptionCases()
    {
        yield return new object[] { new KeyNotFoundException("missing"), StatusCodes.Status404NotFound, "Not Found", "NotFound", "missing", false };
        yield return new object[] { new UnauthorizedAccessException("forbidden"), StatusCodes.Status403Forbidden, "Forbidden", "Forbidden", "forbidden", false };
        yield return new object[] { new ConflictException("stale period"), StatusCodes.Status409Conflict, "Conflict", "Conflict", "stale period", true };
        yield return new object[] { new InvalidOperationException("internal transaction details"), StatusCodes.Status400BadRequest, "Bad Request", "OperationInvalid", "The operation is not valid in its current state.", false };
        yield return new object[] { new Exception("broken"), StatusCodes.Status500InternalServerError, "Internal Server Error", "ServerError", "broken", false };
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task InvokeAsync_Exception_MapsToProblemDetails(
        Exception exception,
        int expectedStatus,
        string expectedTitle,
        string expectedCode,
        string expectedDetail,
        bool expectedSafeDetail)
    {
        var services = new ServiceCollection()
            .AddSingleton(Mock.Of<IWebHostEnvironment>(x => x.EnvironmentName == "Development"))
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response =
            {
                Body = new MemoryStream()
            }
        };

        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(
            document.RootElement.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.Equal(expectedDetail, problemDetails.Detail);
        Assert.Equal(expectedCode, document.RootElement.GetProperty("errorCode").GetString());
        Assert.True(document.RootElement.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
        Assert.Equal(expectedSafeDetail, document.RootElement.GetProperty("safeDetail").GetBoolean());
    }

    [Theory]
    [InlineData(false, "An unexpected error occurred. Please contact support.")]
    [InlineData(true, "Kỳ đã được đóng.")]
    public async Task InvokeAsync_Production_ExposesOnlyAllowlistedBusinessDetail(
        bool businessFailure,
        string expectedDetail)
    {
        var exception = businessFailure
            ? new BusinessException("Kỳ đã được đóng.")
            : new Exception("Server=private;Password=secret");
        var services = new ServiceCollection()
            .AddSingleton(Mock.Of<IWebHostEnvironment>(x => x.EnvironmentName == "Production"))
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Response = { Body = new MemoryStream() }
        };
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(expectedDetail, document.RootElement.GetProperty("detail").GetString());
        Assert.Equal(businessFailure, document.RootElement.GetProperty("safeDetail").GetBoolean());
        Assert.DoesNotContain("Password=secret", document.RootElement.GetRawText(), StringComparison.Ordinal);
    }
}
