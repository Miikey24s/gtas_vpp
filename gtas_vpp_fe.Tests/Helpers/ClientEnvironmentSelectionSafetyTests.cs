using System.Reflection;
using System.Text.Json;
using gtas_vpp_fe.Endpoints;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class ClientEnvironmentSelectionSafetyTests
{
    [Fact]
    public void AuthenticationCookie_IsVersionedForTheEnvironmentBindingCutover()
    {
        Assert.Equal("VPP_AuthCookie_v2", Config.CookieName);
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

    private static bool IsEnvironmentSelectionMember(MemberInfo member)
        => IsEnvironmentSelectionName(member.Name);

    private static bool IsEnvironmentSelectionName(string name)
        => name.Equals("Server", StringComparison.OrdinalIgnoreCase)
            || name.Equals("selected_server", StringComparison.OrdinalIgnoreCase)
            || name.Contains("EnvironmentSelector", StringComparison.OrdinalIgnoreCase);
}
