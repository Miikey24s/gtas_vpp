namespace gtas_vpp_shared.DTOs.Req;

/// <summary>
/// Public API payload for authentication. Deployment/database selection is
/// intentionally absent: the backend owns that binding.
/// </summary>
public sealed class AuthenticationLoginRequest
{
    public AuthenticationLoginRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }

    public string Username { get; }

    public string Password { get; }
}
