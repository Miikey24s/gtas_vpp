using System.Security.Cryptography;
using System.Text;

namespace gtas_vpp_fe.Platform.Auth;

/// <summary>
/// Tạo dấu vân tay một chiều để phân biệt phiên đã bị backend từ chối với phiên
/// mới vừa đăng nhập. Không đưa bearer token thô vào URL hoặc log.
/// </summary>
public static class AuthSessionFingerprint
{
    public static string? Create(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)));
    }

    public static bool MatchesOrIsUnspecified(string? expectedFingerprint, string? currentAccessToken)
    {
        if (string.IsNullOrWhiteSpace(expectedFingerprint))
        {
            return true;
        }

        var currentFingerprint = Create(currentAccessToken);
        return currentFingerprint is not null
            && string.Equals(
                expectedFingerprint.Trim(),
                currentFingerprint,
                StringComparison.OrdinalIgnoreCase);
    }
}
