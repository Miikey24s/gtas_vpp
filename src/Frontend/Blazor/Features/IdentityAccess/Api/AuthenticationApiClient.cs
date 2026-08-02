using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using gtas_vpp_fe.Platform.Api;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_fe.Features.IdentityAccess.Api;

public sealed class AuthenticationApiClient(HttpClient publicClient)
{
    private const string SignInEndpoint = "/api/Auth/login";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _publicClient = publicClient;

    public async Task<AuthenticationResultDTO?> SignInAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        using var response = await _publicClient.PostAsJsonAsync(
            SignInEndpoint,
            new AuthenticationLoginRequest(username, password),
            cancellationToken);
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
            : JsonSerializer.Deserialize<AuthenticationResultDTO>(contentBytes, JsonOptions);
    }
}
