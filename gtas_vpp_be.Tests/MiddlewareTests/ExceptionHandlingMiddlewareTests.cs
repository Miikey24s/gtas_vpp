using System.Text.Json;
using gtas_vpp_be.Middleware;
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
        yield return new object[] { new KeyNotFoundException("missing"), StatusCodes.Status404NotFound, "Not Found" };
        yield return new object[] { new UnauthorizedAccessException("forbidden"), StatusCodes.Status403Forbidden, "Forbidden" };
        yield return new object[] { new InvalidOperationException("invalid"), StatusCodes.Status400BadRequest, "Bad Request" };
        yield return new object[] { new Exception("broken"), StatusCodes.Status500InternalServerError, "Internal Server Error" };
    }

    [Theory]
    [MemberData(nameof(ExceptionCases))]
    public async Task InvokeAsync_Exception_MapsToProblemDetails(Exception exception, int expectedStatus, string expectedTitle)
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
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.NotNull(problemDetails);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal(expectedStatus, problemDetails.Status);
        Assert.Equal(exception.Message, problemDetails.Detail);
    }
}
