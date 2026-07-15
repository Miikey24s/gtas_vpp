using System.Text;
using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Helpers;

/// <summary>
/// Immutable JWT issuance and validation settings captured once at startup.
/// </summary>
public sealed class JwtDeploymentSettings
{
    private JwtDeploymentSettings(
        string key,
        string issuer,
        string audience,
        int accessTokenMinutes,
        int clockSkewMinutes)
    {
        Key = key;
        Issuer = issuer;
        Audience = audience;
        AccessTokenMinutes = accessTokenMinutes;
        ClockSkewMinutes = clockSkewMinutes;
    }

    public string Key { get; }

    public string Issuer { get; }

    public string Audience { get; }

    public int AccessTokenMinutes { get; }

    public int ClockSkewMinutes { get; }

    public static JwtDeploymentSettings Create(
        IConfiguration configuration,
        DatabaseBinding databaseBinding)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(databaseBinding);

        var key = GetRequiredValue(
            configuration,
            "JwtSettings:Key",
            "JwtSettings:Key must be configured via environment variable or user secrets.");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:Key must contain at least 32 UTF-8 bytes.");
        }

        var issuer = GetRequiredValue(
            configuration,
            "JwtSettings:Issuer",
            "JwtSettings:Issuer must be configured explicitly for this deployment.");
        var audience = GetRequiredValue(
            configuration,
            "JwtSettings:Audience",
            "JwtSettings:Audience must be configured explicitly for this deployment.");
        var expectedAudience = databaseBinding.IsTestEnvironment
            ? "gtas_vpp_test_clients"
            : "gtas_vpp_live_clients";
        if (!string.Equals(audience, expectedAudience, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"JwtSettings:Audience must be '{expectedAudience}' for database binding " +
                $"'{databaseBinding.EnvironmentName}'.");
        }

        return new JwtDeploymentSettings(
            key,
            issuer,
            audience,
            Math.Clamp(configuration.GetValue<int?>("JwtSettings:AccessTokenMinutes") ?? 60, 5, 480),
            clockSkewMinutes: 2);
    }

    private static string GetRequiredValue(
        IConfiguration configuration,
        string key,
        string errorMessage)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value;
    }
}
