using System.Globalization;
using System.Net;
using System.Text;
using gtas_vpp_fe;
using gtas_vpp_fe.Components;
using gtas_vpp_fe.Helpers;
using Microsoft.Extensions.Localization;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AccountLifecycleUiMapperTests
{
    [Fact]
    public async Task ReadErrorMessageAsync_UsesLocalizedCodeInsteadOfBackendMessage()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"code":"PASSWORD_RESET_INVALID","message":"raw backend message"}""",
                Encoding.UTF8,
                "application/json")
        };

        var message = await AccountLifecycleUiMapper.ReadErrorMessageAsync(
            response,
            new StubLocalizer());

        Assert.Equal("Liên kết đặt lại không hợp lệ hoặc đã hết hạn.", message);
        Assert.DoesNotContain("raw backend", message);
    }

    [Fact]
    public void GetMessage_UsesStatusFallbackWhenCodeIsUnknown()
    {
        var message = AccountLifecycleUiMapper.GetMessage(
            "UNKNOWN",
            HttpStatusCode.TooManyRequests,
            new StubLocalizer());

        Assert.Equal("Vui lòng thử lại sau.", message);
    }

    private sealed class StubLocalizer : IStringLocalizer<App>
    {
        public LocalizedString this[string name]
        {
            get
            {
                var value = name switch
                {
                    "ResetLinkInvalid" => "Liên kết đặt lại không hợp lệ hoặc đã hết hạn.",
                    "RateLimited" => "Vui lòng thử lại sau.",
                    "RequestFailed" => "Không thể hoàn tất yêu cầu.",
                    _ => name
                };
                return new LocalizedString(name, value, resourceNotFound: value == name);
            }
        }

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
