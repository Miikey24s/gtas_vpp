using System.Globalization;
using System.Net;
using gtas_vpp_fe;
using gtas_vpp_fe.Components;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using Microsoft.Extensions.Localization;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class UiErrorMapperTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "Forbidden")]
    [InlineData(HttpStatusCode.Conflict, "Conflict")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "ValidationFailed")]
    [InlineData(HttpStatusCode.TooManyRequests, "RateLimited")]
    public void GetErrorCode_MapsTransportStatus(HttpStatusCode statusCode, string expected)
    {
        var exception = new HttpRequestException("internal", null, statusCode);

        Assert.Equal(expected, UiErrorMapper.GetErrorCode(exception));
    }

    [Fact]
    public void GetMessage_UsesSafeDetailAndTraceIdOnly()
    {
        var localizer = new StubLocalizer();
        var exception = new ApiRequestException(
            HttpStatusCode.Conflict,
            "Conflict",
            "trace-123",
            "Đơn đã được cập nhật bởi người khác.");

        var message = UiErrorMapper.GetMessage(exception, localizer);

        Assert.Contains("Đơn đã được cập nhật", message);
        Assert.Contains("trace-123", message);
        Assert.DoesNotContain("internal", message);
    }

    [Fact]
    public void GetMessage_DoesNotExposeUnsafeServerDetail()
    {
        var localizer = new StubLocalizer();
        var exception = new ApiRequestException(
            HttpStatusCode.InternalServerError,
            "ServerError",
            "trace-456",
            safeDetail: null);

        var message = UiErrorMapper.GetMessage(exception, localizer);

        Assert.Equal("Lỗi máy chủ. (Trace ID: trace-456)", message);
    }

    [Fact]
    public void GetErrorCode_DistinguishesTimeoutFromExplicitCancellation()
    {
        Assert.Equal("RequestTimeout", UiErrorMapper.GetErrorCode(new TaskCanceledException()));
        Assert.Equal("RequestCancelled", UiErrorMapper.GetErrorCode(new OperationCanceledException()));
    }

    private sealed class StubLocalizer : IStringLocalizer<App>
    {
        public LocalizedString this[string name] =>
            new(name, name switch
            {
                "Conflict" => "Dữ liệu xung đột.",
                "ServerError" => "Lỗi máy chủ.",
                "TraceId" => "Trace ID",
            "RequestFailed" => "Yêu cầu thất bại.",
                _ => name
            }, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
