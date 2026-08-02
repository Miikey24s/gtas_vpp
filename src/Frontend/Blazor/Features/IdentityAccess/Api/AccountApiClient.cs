using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using gtas_vpp_fe.Platform.Api;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;

namespace gtas_vpp_fe.Features.IdentityAccess.Api;

public sealed class AccountApiClient(
    HttpClient publicClient,
    IAPIServices authenticatedApi)
{
    private const string RegisterEndpoint = "/api/account/register";
    private const string ConfirmEmailEndpoint = "/api/account/confirm-email";
    private const string ResendConfirmationEndpoint = "/api/account/confirm-email/resend";
    private const string PasswordRecoveryEndpoint = "/api/account/password/recovery";
    private const string ResetPasswordEndpoint = "/api/account/password/reset";
    private const string ChangePasswordEndpoint = "/api/account/password/change";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _publicClient = publicClient;
    private readonly IAPIServices _authenticatedApi = authenticatedApi;

    public Task<AccountLifecycleResDTO?> RegisterAsync(
        AccountRegistrationReqDTO request,
        CancellationToken cancellationToken = default) =>
        PostPublicAsync(RegisterEndpoint, request, cancellationToken);

    public Task<AccountLifecycleResDTO?> ResendConfirmationAsync(
        EmailConfirmationResendReqDTO request,
        CancellationToken cancellationToken = default) =>
        PostPublicAsync(ResendConfirmationEndpoint, request, cancellationToken);

    public Task<AccountLifecycleResDTO?> RequestPasswordRecoveryAsync(
        PasswordRecoveryReqDTO request,
        CancellationToken cancellationToken = default) =>
        PostPublicAsync(PasswordRecoveryEndpoint, request, cancellationToken);

    public Task<AccountLifecycleResDTO?> ResetPasswordAsync(
        PasswordResetReqDTO request,
        CancellationToken cancellationToken = default) =>
        PostPublicAsync(ResetPasswordEndpoint, request, cancellationToken);

    public async Task<AccountLifecycleResDTO?> ConfirmEmailAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"{ConfirmEmailEndpoint}?userId={userId}&token={Uri.EscapeDataString(token)}";
        using var response = await _publicClient.GetAsync(endpoint, cancellationToken);
        return await ReadResponseAsync(response, cancellationToken);
    }

    public Task<AccountLifecycleResDTO?> ChangePasswordAsync(
        PasswordChangeReqDTO request) =>
        _authenticatedApi.PostFromApiAsync<AccountLifecycleResDTO>(ChangePasswordEndpoint, request);

    private async Task<AccountLifecycleResDTO?> PostPublicAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _publicClient.PostAsJsonAsync(endpoint, request, cancellationToken);
        return await ReadResponseAsync(response, cancellationToken);
    }

    private static async Task<AccountLifecycleResDTO?> ReadResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var problem = ApiProblemReader.Read(content, response.StatusCode);
            throw new ApiRequestException(
                response.StatusCode,
                problem.ErrorCode,
                problem.TraceId,
                problem.SafeDetail);
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return contentBytes.Length == 0
            ? null
            : JsonSerializer.Deserialize<AccountLifecycleResDTO>(contentBytes, JsonOptions);
    }
}
