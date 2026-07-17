using gtas_vpp_shared.DTOs.Res.Auth;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace gtas_vpp_fe.Helpers
{
    public static class ClaimExtensions
    {
        public static string Get(this IEnumerable<Claim> claims, string type)
        {
            var claim = claims
                .Where(x => x.Type == type)
                .GroupBy(x => x.Type)
                .Select(g => g.LastOrDefault())
                .FirstOrDefault();

            return claim?.Value ?? "";
        }

        public static int GetInt(this IEnumerable<Claim> claims, string type)
        {
            var value = claims.Get(type);
            return int.TryParse(value, out var v) ? v : 0;
        }

        public static bool GetBool(this IEnumerable<Claim> claims, string type)
        {
            var value = claims.Get(type);
            return bool.TryParse(value, out var v) && v;
        }

        public static Guid GetGuid(this IEnumerable<Claim> claims, string type)
        {
            var value = claims.Get(type);
            return Guid.TryParse(value, out var v) ? v : Guid.Empty;
        }

        public static List<Claim> ToAuthenticationClaims(this AuthenticationResultDTO AuthenticationResultDTO)
        {
            if (AuthenticationResultDTO == null) return new List<Claim>();

            var userId = AuthenticationResultDTO.UserID.ToString(CultureInfo.InvariantCulture);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Name, AuthenticationResultDTO.UserLogin ?? string.Empty),
                new(ClaimKeys.UserID, userId),
                new(ClaimKeys.UserLogin, AuthenticationResultDTO.UserLogin ?? string.Empty),
                new(ClaimKeys.AccessToken, AuthenticationResultDTO.AccessToken ?? string.Empty),
                new(ClaimKeys.SessionVersion, AuthenticationResultDTO.SessionVersion.ToString(CultureInfo.InvariantCulture)),
                new(
                    ClaimKeys.AccessTokenExpiresAtUtc,
                    AuthenticationResultDTO.AccessTokenExpiresAtUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
                        ?? string.Empty)
            };

            return claims;
        }

        public static AuthenticationResultDTO ToAuthenticationResult(this IEnumerable<Claim> claims)
        {
            if (claims == null) return new AuthenticationResultDTO();

            return new AuthenticationResultDTO
            {
                UserID = claims.GetInt(ClaimKeys.UserID),
                UserLogin = claims.Get(ClaimKeys.UserLogin),
                AccessToken = claims.Get(ClaimKeys.AccessToken),
                SessionVersion = long.TryParse(
                    claims.Get(ClaimKeys.SessionVersion),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var sessionVersion)
                        ? sessionVersion
                        : 0,
                AccessTokenExpiresAtUtc = DateTime.TryParse(
                    claims.Get(ClaimKeys.AccessTokenExpiresAtUtc),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var expiresAtUtc)
                        ? expiresAtUtc.ToUniversalTime()
                        : null
            };
        }
    }
}
