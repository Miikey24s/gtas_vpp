using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.Auth;
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

        var result = await sut.PostFromApiAsync<AuthenticationResultDTO>(
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

    [Fact]
    public async Task PostFileFromApiAsync_SendsMultipartFileWithOriginalName()
    {
        using var handler = new RecordingHttpMessageHandler("{\"status\":\"Ready\"}");
        using var client = CreateClient(handler);
        var sut = CreateSut(client);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("ItemCode,UnitPrice\nA001,100"));

        var result = await sut.PostFileFromApiAsync<Dictionary<string, string>>(
            "api/vpppricelist/list-id/imports/preview",
            stream,
            "bang-gia.csv",
            "text/csv",
            TestContext.Current.CancellationToken);

        Assert.Equal("Ready", result?["status"]);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Contains("bang-gia.csv", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("ItemCode,UnitPrice", handler.LastRequestBody, StringComparison.Ordinal);
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

        var result = await sut.GetFromApiAsync<AuthenticationResultDTO>("api/auth/me");

        Assert.NotNull(result);
        Assert.Equal(expectedUserId, result.UserID);
        Assert.Equal(expectedUserLogin, result.UserLogin);
    }

    [Fact]
    public async Task GetFromApiAsync_AddsBearerTokenFromAuthenticationState()
    {
        using var handler = new RecordingHttpMessageHandler("{}");
        using var client = CreateClient(handler);
        var authProvider = new StaticAuthenticationStateProvider(
            new Claim(ClaimKeys.AccessToken, "access-token-42"));
        var sut = CreateSut(client, authProvider: authProvider);

        await sut.GetFromApiAsync<object>("api/secure");

        Assert.Equal("Bearer", handler.LastAuthorization?.Scheme);
        Assert.Equal("access-token-42", handler.LastAuthorization?.Parameter);
    }

    [Fact]
    public async Task GetFromApiWithTotalCountAsync_UsesCollectionCountWhenHeaderIsMissing()
    {
        using var handler = new RecordingHttpMessageHandler("[{\"id\":1},{\"id\":2}]");
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var result = await sut.GetFromApiWithTotalCountAsync<List<Dictionary<string, int>>>("api/items");

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task NoContentResponse_ReturnsDefaultWithoutParsing()
    {
        using var handler = new NoContentHttpMessageHandler();
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var result = await sut.GetFromApiAsync<AuthenticationResultDTO>("api/empty");

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenFileFromApiAsync_PreservesSafeFileMetadataAndBytes()
    {
        var expectedBytes = Encoding.UTF8.GetBytes("report-content");
        using var handler = new FileHttpMessageHandler(expectedBytes);
        using var client = CreateClient(handler);
        var sut = CreateSut(client);

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var result = await sut.OpenFileFromApiAsync(
            "api/reports/export.pdf",
            cancellationToken);
        using var buffer = new MemoryStream();
        await result.Content.CopyToAsync(buffer, cancellationToken);

        Assert.Equal("report thang 8.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(expectedBytes.Length, result.ContentLength);
        Assert.Equal(expectedBytes, buffer.ToArray());
    }

    [Fact]
    public async Task UnauthorizedResponse_RequestsSessionInvalidation()
    {
        const string rejectedToken = "rejected-session-token";
        using var handler = new StatusHttpMessageHandler(HttpStatusCode.Unauthorized);
        using var client = CreateClient(handler);
        var coordinator = new RecordingSessionInvalidationCoordinator();
        var sut = CreateSut(
            client,
            coordinator,
            authProvider: new StaticAuthenticationStateProvider(
                new Claim(ClaimKeys.AccessToken, rejectedToken)));

        await Assert.ThrowsAsync<ApiRequestException>(() => sut.GetFromApiAsync<object>("api/one"));

        Assert.Single(coordinator.Reasons);
        Assert.Equal("session-invalid", coordinator.Reasons[0]);
        Assert.Equal(rejectedToken, Assert.Single(coordinator.RejectedAccessTokens));
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
        PermissionRefreshSignal? signal = null,
        AuthenticationStateProvider? authProvider = null) => new(
        client,
        authProvider ?? new AnonymousAuthenticationStateProvider(),
        signal ?? new PermissionRefreshSignal(),
        coordinator ?? new NoOpSessionInvalidationCoordinator());

    private sealed class NoOpSessionInvalidationCoordinator : IAuthSessionInvalidationCoordinator
    {
        public Task InvalidateAsync(string reason, string? rejectedAccessToken = null) => Task.CompletedTask;
    }

    private sealed class RecordingSessionInvalidationCoordinator : IAuthSessionInvalidationCoordinator
    {
        public List<string> Reasons { get; } = [];
        public List<string?> RejectedAccessTokens { get; } = [];

        public Task InvalidateAsync(string reason, string? rejectedAccessToken = null)
        {
            Reasons.Add(reason);
            RejectedAccessTokens.Add(rejectedAccessToken);
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

    private sealed class StaticAuthenticationStateProvider(params Claim[] claims) : AuthenticationStateProvider
    {
        private readonly Task<AuthenticationState> _state = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state;
    }

    private sealed class RecordingHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        public string? LastRequestBody { get; private set; }

        public AuthenticationHeaderValue? LastAuthorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            LastAuthorization = request.Headers.Authorization;

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class NoContentHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)
            {
                Content = new ByteArrayContent([])
            });
    }

    private sealed class FileHttpMessageHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var responseContent = new ByteArrayContent(content);
            responseContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            responseContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = "report%20thang%208.pdf"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = responseContent
            });
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
