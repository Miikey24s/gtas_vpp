namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Mirrors the public ASP.NET Core Identity password rules used by GTAS VPP.
/// The server remains authoritative; this helper provides immediate account-form feedback.
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
