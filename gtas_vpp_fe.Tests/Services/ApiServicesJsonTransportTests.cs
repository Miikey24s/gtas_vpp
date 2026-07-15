using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Xunit;

namespace gtas_vpp_fe.Tests.Services;

public sealed class ApiServicesJsonTransportTests
{
    [Fact]
    public async Task PostFromApiAsync_SerializesLoginRequestWithWebCamelCaseContract()
    {
        using var handler = new RecordingHttpMessageHandler(
            """{"userID":5615,"userLogin":"response-user"}""");
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var result = await sut.PostFromApiAsync<sp_Authentication_Login>(
            "api/auth/login",
            new AuthenticationLoginRequest("tester", "secret"));

        Assert.NotNull(result);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal(new Uri("https://localhost/api/auth/login"), handler.LastRequestUri);
        Assert.NotNull(handler.LastRequestBody);

        using var document = JsonDocument.Parse(handler.LastRequestBody);
        var properties = document.RootElement
            .EnumerateObject()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "password", "username" }, properties.Select(property => property.Name));
        Assert.Equal("secret", document.RootElement.GetProperty("password").GetString());
        Assert.Equal("tester", document.RootElement.GetProperty("username").GetString());
    }

    [Theory]
    [InlineData("""{"UserID":5615,"UserLogin":"legacy-user"}""", 5615, "legacy-user")]
    [InlineData("""{"userID":5616,"userLogin":"camel-user"}""", 5616, "camel-user")]
    public async Task GetFromApiAsync_DeserializesPascalCaseAndCamelCaseLoginResponses(
        string responseJson,
        int expectedUserId,
        string expectedUserLogin)
    {
        using var handler = new RecordingHttpMessageHandler(responseJson);
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var result = await sut.GetFromApiAsync<sp_Authentication_Login>("api/auth/me");

        Assert.NotNull(result);
        Assert.Equal(expectedUserId, result.UserID);
        Assert.Equal(expectedUserLogin, result.UserLogin);
        Assert.Equal(string.Empty, result.PasswordChar);
    }

    private static APIServices CreateSut(HttpClient client) => new(
        client,
        new AnonymousAuthenticationStateProvider(),
        new PermissionRefreshSignal());

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://localhost/")
    };

    private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly Task<AuthenticationState> AnonymousState = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => AnonymousState;
    }

    private sealed class RecordingHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }
}
