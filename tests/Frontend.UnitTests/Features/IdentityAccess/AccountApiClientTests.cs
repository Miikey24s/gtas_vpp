using System.Net;
using System.Text;
using System.Text.Json;
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.IdentityAccess;

public sealed class AccountApiClientTests
{
    [Theory]
    [InlineData("register", "/api/account/register", "username", "user42")]
    [InlineData("recovery", "/api/account/password/recovery", "email", "recover@example.edu.vn")]
    [InlineData("resend", "/api/account/confirm-email/resend", "email", "resend@example.edu.vn")]
    [InlineData("reset", "/api/account/password/reset", "token", "reset-token-42")]
    public async Task PublicPostMethods_SendCanonicalPathAndWebJson(
        string operation,
        string expectedPath,
        string expectedProperty,
        string expectedValue)
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = new AccountApiClient(httpClient, new StubApiServices());

        _ = operation switch
        {
            "register" => await client.RegisterAsync(new AccountRegistrationReqDTO
            {
                Username = expectedValue,
                Email = "user42@example.edu.vn",
                FullName = "Nguyen Van A",
                Password = "StrongPassword42!",
                ConfirmPassword = "StrongPassword42!"
            }, TestContext.Current.CancellationToken),
            "recovery" => await client.RequestPasswordRecoveryAsync(
                new PasswordRecoveryReqDTO { Email = expectedValue },
                TestContext.Current.CancellationToken),
            "resend" => await client.ResendConfirmationAsync(
                new EmailConfirmationResendReqDTO { Email = expectedValue },
                TestContext.Current.CancellationToken),
            "reset" => await client.ResetPasswordAsync(new PasswordResetReqDTO
            {
                UserId = 42,
                Token = expectedValue,
                NewPassword = "NewStrongPassword42!",
                ConfirmPassword = "NewStrongPassword42!"
            }, TestContext.Current.CancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal(expectedPath, handler.LastRequestUri?.AbsolutePath);
        Assert.Null(handler.LastAuthorization);
        using var document = JsonDocument.Parse(Assert.IsType<string>(handler.LastRequestBody));
        Assert.Equal(expectedValue, document.RootElement.GetProperty(expectedProperty).GetString());
    }

    [Fact]
    public async Task ConfirmEmailAsync_UsesGetAndEncodesToken()
    {
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = new AccountApiClient(httpClient, new StubApiServices());

        await client.ConfirmEmailAsync(
            42,
            "a/b+c=",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.LastMethod);
        Assert.Equal(
            "/api/account/confirm-email?userId=42&token=a%2Fb%2Bc%3D",
            handler.LastRequestUri?.PathAndQuery);
        Assert.Null(handler.LastAuthorization);
    }

    [Fact]
    public async Task PublicUnauthorized_ThrowsSafeApiExceptionWithoutUsingAuthenticatedTransport()
    {
        using var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            """{"code":"ACCOUNT_UNAVAILABLE"}""");
        using var httpClient = CreateHttpClient(handler);
        var authenticatedTransportUsed = false;
        var authenticatedApi = new StubApiServices
        {
            PostAsync = (_, _, _) =>
            {
                authenticatedTransportUsed = true;
                return Task.FromResult<object?>(null);
            }
        };
        var client = new AccountApiClient(httpClient, authenticatedApi);

        var exception = await Assert.ThrowsAsync<ApiRequestException>(() =>
            client.RequestPasswordRecoveryAsync(
                new PasswordRecoveryReqDTO { Email = "unknown@example.edu.vn" },
                TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("ACCOUNT_UNAVAILABLE", exception.ErrorCode);
        Assert.False(authenticatedTransportUsed);
    }

    [Fact]
    public async Task ChangePasswordAsync_UsesAuthenticatedTransport()
    {
        string? endpoint = null;
        PasswordChangeReqDTO? sentRequest = null;
        var authenticatedApi = new StubApiServices
        {
            PostAsync = (capturedEndpoint, body, _) =>
            {
                endpoint = capturedEndpoint;
                sentRequest = Assert.IsType<PasswordChangeReqDTO>(body);
                return Task.FromResult<object?>(new AccountLifecycleResDTO());
            }
        };
        using var handler = new RecordingHttpMessageHandler();
        using var httpClient = CreateHttpClient(handler);
        var client = new AccountApiClient(httpClient, authenticatedApi);
        var request = new PasswordChangeReqDTO
        {
            CurrentPassword = "CurrentPassword42!",
            NewPassword = "NewStrongPassword42!",
            ConfirmPassword = "NewStrongPassword42!"
        };

        await client.ChangePasswordAsync(request);

        Assert.Equal("/api/account/password/change", endpoint);
        Assert.Same(request, sentRequest);
        Assert.Null(handler.LastMethod);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) => new(handler)
    {
        BaseAddress = new Uri("https://localhost/")
    };

    private sealed class RecordingHttpMessageHandler(
        HttpStatusCode statusCode = HttpStatusCode.Accepted,
        string responseBody = """{"accountId":42,"accountStatus":"PendingApproval"}""")
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
