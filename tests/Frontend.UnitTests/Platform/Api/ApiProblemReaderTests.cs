using System.Net;
using gtas_vpp_fe.Platform.Api;
using Xunit;

namespace gtas_vpp_fe.Tests.Platform.Api;

public sealed class ApiProblemReaderTests
{
    [Fact]
    public void Read_UsesPublicAccountCodeFromRoot()
    {
        var problem = ApiProblemReader.Read(
            """{"code":"ACCOUNT_UNAVAILABLE"}""",
            HttpStatusCode.Unauthorized);

        Assert.Equal("ACCOUNT_UNAVAILABLE", problem.ErrorCode);
        Assert.Null(problem.SafeDetail);
    }

    [Fact]
    public void Read_UsesNestedExtensionsAndOnlyExposesMarkedSafeDetail()
    {
        var problem = ApiProblemReader.Read(
            """{"detail":"Safe explanation","extensions":{"errorCode":"Conflict","traceId":"trace-42","safeDetail":true}}""",
            HttpStatusCode.Conflict);

        Assert.Equal("Conflict", problem.ErrorCode);
        Assert.Equal("trace-42", problem.TraceId);
        Assert.Equal("Safe explanation", problem.SafeDetail);
    }

    [Fact]
    public void Read_DoesNotExposeUnmarkedDetail()
    {
        var problem = ApiProblemReader.Read(
            """{"detail":"Internal database detail","errorCode":"ServerError"}""",
            HttpStatusCode.InternalServerError);

        Assert.Equal("ServerError", problem.ErrorCode);
        Assert.Null(problem.SafeDetail);
    }

    [Fact]
    public void Read_UsesStatusFallbackForMalformedBody()
    {
        var problem = ApiProblemReader.Read("not-json", HttpStatusCode.TooManyRequests);

        Assert.Equal("RateLimited", problem.ErrorCode);
        Assert.Null(problem.TraceId);
        Assert.Null(problem.SafeDetail);
    }
}
