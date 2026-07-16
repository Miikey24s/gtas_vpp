using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using gtas_vpp_fe.Endpoints;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.State;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class ClientEnvironmentSelectionSafetyTests
{
    [Fact]
    public void AuthenticationCookie_IsVersionedForTheEnvironmentBindingCutover()
    {
        Assert.Equal("VPP_AuthCookie_v3", Config.CookieName);
        Assert.NotEqual("VPP_AuthCookie", Config.CookieName);
    }

    [Fact]
    public void AuthenticationClientContracts_DoNotExposeEnvironmentSelection()
    {
        Assert.DoesNotContain(
            typeof(sp_Authentication_LoginReqDTO).GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            IsEnvironmentSelectionMember);
        Assert.DoesNotContain(
            typeof(GlobalClass).GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            IsEnvironmentSelectionMember);
        Assert.DoesNotContain(
            typeof(ClaimKeys).GetMembers(BindingFlags.Public | BindingFlags.Static),
            IsEnvironmentSelectionMember);

        var request = new AuthenticationLoginRequest("tester", "secret");
        var requestJson = JsonSerializer.SerializeToElement(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var requestProperties = requestJson.EnumerateObject().ToArray();
        Assert.Equal(2, requestProperties.Length);
        Assert.Contains(requestProperties, property => property.NameEquals("username"));
        Assert.Contains(requestProperties, property => property.NameEquals("password"));
        Assert.DoesNotContain("secret", request.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(AuthenticationLoginRequest).GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static),
            IsEnvironmentSelectionMember);

        var claims = LoginEndpoints.CreateAuthenticationClaims(new sp_Authentication_Login());
        Assert.DoesNotContain(
            claims,
            claim => IsEnvironmentSelectionName(claim.Type));
    }

    [Fact]
    public void AuthenticationClaims_KeepOnlyStableSessionFields()
    {
        var expiry = DateTime.UtcNow.AddMinutes(5);
        var claims = LoginEndpoints.CreateAuthenticationClaims(new sp_Authentication_Login
        {
            UserID = 42,
            UserLogin = "tester",
            FullName = "Test User",
            Email = "tester@example.test",
            GroupId = Guid.NewGuid(),
            GroupName = "Admin",
            DepartmentName = "Legacy department",
            DepartmentCode = "LEGACY",
            IsAdmin = true,
            AccessToken = "jwt",
            SessionVersion = 7,
            AccessTokenExpiresAtUtc = expiry
        });

        Assert.Contains(claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == "42");
        Assert.Contains(claims, claim => claim.Type == ClaimTypes.Name && claim.Value == "tester");
        Assert.Contains(claims, claim => claim.Type == ClaimKeys.AccessToken && claim.Value == "jwt");
        Assert.Contains(claims, claim => claim.Type == ClaimKeys.SessionVersion && claim.Value == "7");
        Assert.DoesNotContain(claims, claim => claim.Type is ClaimKeys.FullName
            or ClaimKeys.Email
            or ClaimKeys.GroupId
            or ClaimKeys.GroupName
            or ClaimKeys.DepartmentName
            or ClaimKeys.DepartmentCode
            or ClaimKeys.IsAdmin);
    }

    [Theory]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("/dashboard?tab=1", "/dashboard?tab=1")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void ReturnUrl_IsRestrictedToLocalAbsolutePath(string? candidate, string expected)
    {
        Assert.Equal(expected, LoginEndpoints.GetSafeLocalReturnUrl(candidate));
    }

    private static bool IsEnvironmentSelectionMember(MemberInfo member)
        => IsEnvironmentSelectionName(member.Name);

    private static bool IsEnvironmentSelectionName(string name)
        => name.Equals("Server", StringComparison.OrdinalIgnoreCase)
            || name.Equals("selected_server", StringComparison.OrdinalIgnoreCase)
            || name.Contains("EnvironmentSelector", StringComparison.OrdinalIgnoreCase);
}
