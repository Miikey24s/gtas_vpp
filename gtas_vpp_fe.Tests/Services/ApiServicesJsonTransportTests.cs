using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Req.VPP;
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

    [Fact]
    public async Task PostFromApiAsync_SerializesApprovalConcurrencyEnvelope()
    {
        using var handler = new RecordingHttpMessageHandler("{}");
        using var client = CreateClient(handler);
        var sut = CreateSut(client);
        var rowVersion = new byte[] { 1, 3, 5, 7, 9 };

        await sut.PostFromApiAsync<object>(
            "api/VPPRequest/additional-orders/order-id/approve",
            new ApproveOrderReqDTO
            {
                RowVersion = rowVersion,
                IdempotencyKey = "approval-command-42"
            });

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.NotNull(handler.LastRequestBody);
        using var document = JsonDocument.Parse(handler.LastRequestBody);
        Assert.Equal(
            Convert.ToBase64String(rowVersion),
            document.RootElement.GetProperty("rowVersion").GetString());
        Assert.Equal(
            "approval-command-42",
            document.RootElement.GetProperty("idempotencyKey").GetString());
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
    }

    [Fact]
    public async Task UnauthorizedResponse_RequestsSessionInvalidation()
    {
        using var handler = new StatusHttpMessageHandler(HttpStatusCode.Unauthorized);
        using var client = CreateClient(handler);
        var coordinator = new RecordingSessionInvalidationCoordinator();
        var sut = CreateSut(client, coordinator);

        await Assert.ThrowsAsync<ApiRequestException>(() => sut.GetFromApiAsync<object>("api/one"));

        Assert.Single(coordinator.Reasons);
        Assert.Equal("session-invalid", coordinator.Reasons[0]);
    }

    [Fact]
    public async Task ForbiddenDelete_RequestsPermissionRefresh()
    {
        using var handler = new StatusHttpMessageHandler(HttpStatusCode.Forbidden);
        using var client = CreateClient(handler);
        var signal = new PermissionRefreshSignal();
        var refreshCount = 0;
        signal.Requested += () =>
        {
            refreshCount++;
            return Task.CompletedTask;
        };
        var sut = CreateSut(client, new NoOpSessionInvalidationCoordinator(), signal);

        await Assert.ThrowsAsync<ApiRequestException>(() => sut.DeleteFromApiAsync("api/item"));

        Assert.Equal(1, refreshCount);
    }

    [Fact]
    public async Task ProblemDetailsResponse_MapsSafeMetadataWithoutRawBody()
    {
        const string responseJson = """{"type":"about:blank","title":"Conflict","status":409,"detail":"Order 42 is already locked","errorCode":"Conflict","traceId":"trace-42","safeDetail":true}""";
        using var handler = new StatusBodyHttpMessageHandler(HttpStatusCode.Conflict, responseJson);
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(
            () => sut.GetFromApiAsync<object>("api/one"));

        Assert.Equal("Conflict", exception.ErrorCode);
        Assert.Equal("trace-42", exception.TraceId);
        Assert.Equal("Order 42 is already locked", exception.SafeDetail);
        Assert.DoesNotContain("extensions", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static APIServices CreateSut(
        HttpClient client,
        IAuthSessionInvalidationCoordinator? coordinator = null,
        PermissionRefreshSignal? signal = null) => new(
        client,
        new AnonymousAuthenticationStateProvider(),
        signal ?? new PermissionRefreshSignal(),
        coordinator ?? new NoOpSessionInvalidationCoordinator());

    private sealed class NoOpSessionInvalidationCoordinator : IAuthSessionInvalidationCoordinator
    {
        public Task InvalidateAsync(string reason) => Task.CompletedTask;
    }

    private sealed class RecordingSessionInvalidationCoordinator : IAuthSessionInvalidationCoordinator
    {
        public List<string> Reasons { get; } = [];

        public Task InvalidateAsync(string reason)
        {
            Reasons.Add(reason);
            return Task.CompletedTask;
        }
    }

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

    private sealed class StatusHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"message\":\"denied\"}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class StatusBodyHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/problem+json")
            });
        }
    }
}
