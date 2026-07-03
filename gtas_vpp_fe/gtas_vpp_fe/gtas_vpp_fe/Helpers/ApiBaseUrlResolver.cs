using System.Net;

namespace gtas_vpp_fe.Helpers;

public static class ApiBaseUrlResolver
{
    public static Uri Resolve(string? rawBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawBaseUrl))
        {
            throw new InvalidOperationException("ApiSettings:BaseUrl is not configured.");
        }

        var normalizedInput = NormalizeWildcardHostSyntax(rawBaseUrl.Trim());

        if (!Uri.TryCreate(normalizedInput, UriKind.Absolute, out var parsedBaseUrl))
        {
            throw new InvalidOperationException(
                $"ApiSettings:BaseUrl must be an absolute URI. Current value: '{rawBaseUrl}'.");
        }

        if (!IsWildcardBindHost(parsedBaseUrl.Host))
        {
            return parsedBaseUrl;
        }

        var normalizedHost = parsedBaseUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? "localhost"
            : IPAddress.Loopback.ToString();

        return new UriBuilder(parsedBaseUrl)
        {
            Host = normalizedHost
        }.Uri;
    }

    public static bool ShouldBypassServerCertificateValidation(bool isDevelopment, Uri baseUri)
    {
        return isDevelopment
            && baseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && baseUri.IsLoopback;
    }

    private static bool IsWildcardBindHost(string host)
    {
        return string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::]", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "+", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "*", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWildcardHostSyntax(string rawBaseUrl)
    {
        var schemeSeparatorIndex = rawBaseUrl.IndexOf("://", StringComparison.Ordinal);
        if (schemeSeparatorIndex < 0)
        {
            return rawBaseUrl;
        }

        var hostStartIndex = schemeSeparatorIndex + 3;
        if (hostStartIndex >= rawBaseUrl.Length)
        {
            return rawBaseUrl;
        }

        if (rawBaseUrl[hostStartIndex] is not ('+' or '*'))
        {
            return rawBaseUrl;
        }

        var nextCharacterIndex = hostStartIndex + 1;
        if (nextCharacterIndex < rawBaseUrl.Length
            && rawBaseUrl[nextCharacterIndex] is not (':' or '/' or '?'))
        {
            return rawBaseUrl;
        }

        return rawBaseUrl.Remove(hostStartIndex, 1).Insert(hostStartIndex, "0.0.0.0");
    }
}