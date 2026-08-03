using System.Security.Claims;
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AuthHelperTests
{
    [Fact]
    public async Task AnonymousUser_DoesNotLoadCurrentProfile()
    {
        var api = new StubApiServices();
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())),
            api,
            new CurrentUserState(api));

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.False(result.IsAuthenticated);
        Assert.Empty(result.Claims);
        Assert.Equal(0, api.GetCallCount);
    }

    [Fact]
    public async Task AuthenticatedUser_LoadsCanonicalCurrentUserStateAndReturnsClaims()
    {
        var groupId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var profile = new CurrentUserResDTO
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
        };
        var api = new StubApiServices
        {
            GetAsync = (endpoint, _) =>
            {
                Assert.Equal("api/Auth/me", endpoint);
                return Task.FromResult<object?>(profile);
            }
        };
        var principal = CreateAuthenticatedPrincipal();
        var currentUserState = new CurrentUserState(api);
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(principal),
            api,
            currentUserState);

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.True(result.IsAuthenticated);
        Assert.Equal(principal.Claims, result.Claims);
        Assert.Same(profile, currentUserState.Current);
        Assert.True(currentUserState.IsLoaded);
        Assert.Equal(1, api.GetCallCount);
    }

    [Fact]
    public async Task MissingProfile_ReturnsFalseAndKeepsCurrentUserStateUnloaded()
    {
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(null)
        };
        var currentUserState = new CurrentUserState(api);
        var helper = new AuthHelper(
            new MutableAuthenticationStateProvider(CreateAuthenticatedPrincipal()),
            api,
            currentUserState);

        var result = await helper.EnsureAuthenticatedAsync();

        Assert.False(result.IsAuthenticated);
        Assert.Empty(result.Claims);
        Assert.Null(currentUserState.Current);
        Assert.False(currentUserState.IsLoaded);
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
