namespace gtas_vpp_shared.DTOs.Req;

/// <summary>
/// Payload API công khai cho xác thực. Chủ động không cho phép chọn deployment/database
/// vì backend sở hữu binding này.
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
