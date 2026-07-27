namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Phản chiếu quy tắc mật khẩu ASP.NET Core Identity công khai của GTAS VPP.
/// Server vẫn có thẩm quyền; helper này chỉ cung cấp feedback tức thời cho form tài khoản.
/// </summary>
public static class AccountPasswordPolicy
{
    public const int MinimumLength = 10;
    public const int MaximumLength = 128;

    public static bool IsValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password)
            || password.Length < MinimumLength
            || password.Length > MaximumLength)
        {
            return false;
        }

        return password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => !char.IsLetterOrDigit(character));
    }
}
