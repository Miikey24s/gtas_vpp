using System.Net;
using System.Text;
using System.Text.Json;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Services;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.IdentityAccess;

public sealed class AuthenticationApiClientTests
{
    [Fact]
    public async Task SignInAsync_SendsCanonicalWebJsonWithoutBearer()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = new AuthenticationApiClient(httpClient);

        var result = await client.SignInAsync(
            "employee42",
            "StrongPassword42!",
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(42, result.UserID);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("/api/Auth/login", handler.LastRequestUri?.AbsolutePath);
        Assert.Null(handler.LastAuthorization);
        using var document = JsonDocument.Parse(Assert.IsType<string>(handler.LastRequestBody));
        Assert.Equal("employee42", document.RootElement.GetProperty("username").GetString());
        Assert.Equal("StrongPassword42!", document.RootElement.GetProperty("password").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task SignInAsync_ThrowsStatusAwareApiException(HttpStatusCode statusCode)
    {
        using var handler = new RecordingHttpMessageHandler(
            statusCode,
            """{"code":"LOGIN_FAILED"}""");
        using var httpClient = CreateHttpClient(handler);
        var client = new AuthenticationApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() =>
            client.SignInAsync(
                "employee42",
                "wrong-password",
                TestContext.Current.CancellationToken));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal("LOGIN_FAILED", exception.ErrorCode);
    }

    [Fact]
    public async Task SignInAsync_ThrowsJsonExceptionForMalformedSuccessBody()
    {
        using var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK, "not-json");
        using var httpClient = CreateHttpClient(handler);
        var client = new AuthenticationApiClient(httpClient);

        await Assert.ThrowsAsync<JsonException>(() =>
            client.SignInAsync(
                "employee42",
                "StrongPassword42!",
                TestContext.Current.CancellationToken));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://localhost/")
    };

    private sealed class RecordingHttpMessageHandler(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = """{"userID":42,"userLogin":"employee42"}""")
        : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }
        public System.Net.Http.Headers.AuthenticationHeaderValue? LastAuthorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastAuthorization = request.Headers.Authorization;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
