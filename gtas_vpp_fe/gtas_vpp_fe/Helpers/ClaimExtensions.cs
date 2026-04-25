using gtas_vpp_shared.DTOs.Res.Auth;
using Mapster;
using System;
using System.Collections.Generic;
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

        public static bool HasPermission(this IEnumerable<Claim>? claims, string permission)
        {
            return claims?.Any(x =>
                x.Type == ClaimKeys.Permission &&
                string.Equals(x.Value, permission, StringComparison.OrdinalIgnoreCase)) == true;
        }

        public static Guid GetGuid(this IEnumerable<Claim> claims, string type)
        {
            var value = claims.Get(type);
            return Guid.TryParse(value, out var v) ? v : Guid.Empty;
        }

        public static List<Claim> sp_AuthenticationLogin_To_Claims(this sp_Authentication_Login sp_Authentication_Login)
        {
            if (sp_Authentication_Login == null) return new List<Claim>();

            var claims = new List<Claim>
            {
                new(ClaimKeys.UserID, sp_Authentication_Login.UserID.ToString()),
                new(ClaimKeys.UserLogin, sp_Authentication_Login.UserLogin ?? string.Empty),
                new(ClaimKeys.FullName, sp_Authentication_Login.FullName ?? string.Empty),
                new(ClaimKeys.Email, sp_Authentication_Login.Email ?? string.Empty),
                new(ClaimKeys.GoogleEmail, sp_Authentication_Login.GoogleEmail ?? string.Empty),
                new(ClaimKeys.IsAdmin, sp_Authentication_Login.IsAdmin.ToString()),
                new(ClaimKeys.GroupId, sp_Authentication_Login.GroupId.ToString()),
                new(ClaimKeys.GroupName, sp_Authentication_Login.GroupName ?? string.Empty),
                new(ClaimKeys.MemberCompanyCode, sp_Authentication_Login.MemberCompanyCode ?? string.Empty),
                new(ClaimKeys.MemberCompanyName, sp_Authentication_Login.MemberCompanyName ?? string.Empty),
                new(ClaimKeys.MemberCompanyShortName, sp_Authentication_Login.MemberCompanyShortName ?? string.Empty),
                new(ClaimKeys.DepartmentName, sp_Authentication_Login.DepartmentName ?? string.Empty),
                new(ClaimKeys.DepartmentCode, sp_Authentication_Login.DepartmentCode ?? string.Empty),
                new(ClaimKeys.AccessToken, sp_Authentication_Login.AccessToken ?? string.Empty)
            };

            var permissions = (sp_Authentication_Login.List_PagePermission ?? new())
                .SelectMany(x => x.List_Component ?? new())
                .Where(x => x.IsVisible && !string.IsNullOrWhiteSpace(x.ComponentCode))
                .Select(x => x.ComponentCode!)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            claims.AddRange(permissions.Select(permission => new Claim(ClaimKeys.Permission, permission)));

            return claims;
        }

        public static sp_Authentication_Login Claims_To_sp_AuthenticationLogin(this IEnumerable<Claim> claims)
        {
            if (claims == null) return new sp_Authentication_Login();

            return new sp_Authentication_Login
            {
                UserID = claims.GetInt(ClaimKeys.UserID),
                UserLogin = claims.Get(ClaimKeys.UserLogin),
                FullName = claims.Get(ClaimKeys.FullName),
                Email = claims.Get(ClaimKeys.Email),
                GoogleEmail = claims.Get(ClaimKeys.GoogleEmail),
                IsAdmin = claims.GetBool(ClaimKeys.IsAdmin),
                GroupId = claims.GetGuid(ClaimKeys.GroupId),
                GroupName = claims.Get(ClaimKeys.GroupName),
                MemberCompanyCode = claims.Get(ClaimKeys.MemberCompanyCode),
                MemberCompanyName = claims.Get(ClaimKeys.MemberCompanyName),
                MemberCompanyShortName = claims.Get(ClaimKeys.MemberCompanyShortName),
                DepartmentName = claims.Get(ClaimKeys.DepartmentName),
                DepartmentCode = claims.Get(ClaimKeys.DepartmentCode),
                AccessToken = claims.Get(ClaimKeys.AccessToken)
            };
        }
    }
}
