using System.Security.Claims;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.State;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AuthHelperTests
{
    [Fact]
    public async Task AnonymousUser_DoesNotLoadProfileOrMutateGlobalProjection()
    {
        var api = new StubApiServices();
        var global = new GlobalClass
        {
            UserInfo = new AuthenticationResultDTO { UserID = 7, UserLogin = "existing" }
        };
        var existingProjection = global.UserInfo;
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())),
            api,
            global,
            new CurrentUserState(api));

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.False(result.IsAuthenticated);
        Assert.Empty(result.Claims);
        Assert.Equal(0, api.GetCallCount);
        Assert.Same(existingProjection, global.UserInfo);
    }

    [Fact]
    public async Task AuthenticatedUser_ProjectsServerProfileAndAccessTokenIntoGlobalClass()
    {
        var groupId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var api = new StubApiServices
        {
            GetAsync = (endpoint, _) =>
            {
                Assert.Equal("api/Auth/me", endpoint);
                return Task.FromResult<object?>(new CurrentUserResDTO
                {
                    UserId = 42,
                    UserLogin = "admin42",
                    FullName = "Nguyen Van Admin",
                    Email = "admin@example.edu.vn",
                    AccountStatus = "Active",
                    MustChangePassword = true,
                    SessionVersion = 9,
                    GroupId = groupId,
                    GroupCode = CanonicalRbac.SystemAdmin.GroupCode,
                    GroupName = "System Administrator",
                    MemberCompanyCode = 1001,
                    PrimaryDepartmentCode = "IT",
                    PrimaryDepartmentName = "Information Technology"
                });
            }
        };
        var principal = CreateAuthenticatedPrincipal();
        var global = new GlobalClass();
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(principal),
            api,
            global,
            new CurrentUserState(api));

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.True(result.IsAuthenticated);
        Assert.Equal(principal.Claims, result.Claims);
        Assert.Equal(42, global.UserInfo.UserID);
        Assert.Equal("admin42", global.UserInfo.UserLogin);
        Assert.Equal("Nguyen Van Admin", global.UserInfo.FullName);
        Assert.Equal("token-42", global.UserInfo.AccessToken);
        Assert.Equal(groupId, global.UserInfo.GroupId);
        Assert.Equal("IT", global.UserInfo.DepartmentCode);
        Assert.Equal(9, global.UserInfo.SessionVersion);
        Assert.True(global.UserInfo.IsAdmin);
        Assert.True(global.UserInfo.MustChangePassword);
    }

    [Fact]
    public async Task MissingProfile_ReturnsFalseWithoutOverwritingExistingProjection()
    {
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(null)
        };
        var global = new GlobalClass
        {
            UserInfo = new AuthenticationResultDTO { UserID = 7, UserLogin = "existing" }
        };
        var existingProjection = global.UserInfo;
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(CreateAuthenticatedPrincipal()),
            api,
            global,
            new CurrentUserState(api));

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.False(result.IsAuthenticated);
        Assert.Empty(result.Claims);
        Assert.Same(existingProjection, global.UserInfo);
    }

    [Fact]
    public async Task GetMyPermissionsAsync_ReturnsEmptySnapshotWhenApiReturnsNull()
    {
        var api = new StubApiServices
        {
            GetAsync = (endpoint, _) =>
            {
                Assert.Equal("api/Auth/me/permissions", endpoint);
                return Task.FromResult<object?>(null);
            }
        };
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(CreateAuthenticatedPrincipal()),
            api,
            new GlobalClass(),
            new CurrentUserState(api));

        var snapshot = await helper.GetMyPermissionsAsync();

        Assert.Empty(snapshot.Pages);
        Assert.Empty(snapshot.Permissions);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal() => new(new ClaimsIdentity(
    [
        new Claim(ClaimKeys.UserID, "42"),
        new Claim(ClaimKeys.UserLogin, "admin42"),
        new Claim(ClaimKeys.AccessToken, "token-42")
    ], "test"));
}
