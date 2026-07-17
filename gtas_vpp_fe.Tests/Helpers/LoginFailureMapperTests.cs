using System.Globalization;
using System.Net;
using gtas_vpp_fe.Helpers;
using Microsoft.Extensions.Localization;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class LoginFailureMapperTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Thông tin đăng nhập chưa hợp lệ.")]
    [InlineData(HttpStatusCode.Unauthorized, "Tên đăng nhập hoặc mật khẩu không hợp lệ.")]
    [InlineData(HttpStatusCode.TooManyRequests, "Vui lòng chờ rồi thử lại.")]
    [InlineData(HttpStatusCode.InternalServerError, "Chưa thể xử lý đăng nhập.")]
    public void GetMessage_MapsStatusToSafeLocalizedText(HttpStatusCode statusCode, string expected)
    {
        var message = LoginFailureMapper.GetMessage(statusCode, new StubLocalizer());

        Assert.Equal(expected, message);
        Assert.DoesNotContain('{', message);
        Assert.DoesNotContain("message", message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class StubLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, name switch
        {
            "LoginRequestInvalid" => "Thông tin đăng nhập chưa hợp lệ.",
            "LoginInvalidCredentials" => "Tên đăng nhập hoặc mật khẩu không hợp lệ.",
            "LoginRateLimited" => "Vui lòng chờ rồi thử lại.",
            "LoginRequestFailed" => "Chưa thể xử lý đăng nhập.",
            "RequestFailed" => "Yêu cầu thất bại.",
            _ => name
        }, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
