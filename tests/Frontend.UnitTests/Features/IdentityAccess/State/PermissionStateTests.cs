using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.Api;
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.IdentityAccess.State;

public sealed class PermissionStateTests
{
    [Fact]
    public async Task RefreshAsync_LoadsSnapshotAndSelectsFirstAccessibleRoute()
    {
        var fixture = CreateFixture();
        var notifications = 0;
        fixture.State.Changed += () => notifications++;

        await fixture.State.RefreshAsync();

        Assert.True(fixture.State.IsLoaded);
        Assert.Equal(42, fixture.State.CurrentUserId);
        Assert.Equal(fixture.GroupId, fixture.State.CurrentGroupId);
        Assert.Equal(7, fixture.State.Version);
        Assert.True(fixture.State.HasPermission(Permissions.ReportExport));
        Assert.True(fixture.State.HasVisibleComponent(
            Config.Page_ComponentCode.PageCode.Dashboard,
            Permissions.RequestProductCatalog));
        Assert.Equal(
            "/dashboard?tab=2",
            fixture.State.GetFirstAccessibleRouteForPage(Config.Page_ComponentCode.PageCode.Dashboard));
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task PermissionRefreshSignal_ReloadsCurrentSnapshot()
    {
        var fixture = CreateFixture();
        await fixture.State.RefreshAsync();
        fixture.Snapshot.Version = 8;
        fixture.Snapshot.Permissions.Add(Permissions.PermissionManage);

        await fixture.Signal.RequestAsync();

        Assert.Equal(8, fixture.State.Version);
        Assert.True(fixture.State.HasPermission(Permissions.PermissionManage));
    }

    [Fact]
    public async Task RefreshAsync_ResetsStateWhenAuthenticationBecomesAnonymous()
    {
        var fixture = CreateFixture();
        await fixture.State.RefreshAsync();
        var notifications = 0;
        fixture.State.Changed += () => notifications++;
        fixture.AuthProvider.SetPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));

        await fixture.State.RefreshAsync();

        Assert.False(fixture.State.IsLoaded);
        Assert.Empty(fixture.State.IdentityClaims);
        Assert.Empty(fixture.State.PagePermissions);
        Assert.Empty(fixture.State.EffectivePermissions);
        Assert.Equal(Guid.Empty, fixture.State.CurrentGroupId);
        Assert.Equal(0, fixture.State.Version);
        Assert.Null(fixture.State.GetFirstAccessibleRoute());
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task Dispose_UnsubscribesPermissionRefreshSignal()
    {
        var fixture = CreateFixture();
        await fixture.State.RefreshAsync();
        fixture.State.Dispose();
        fixture.Snapshot.Version = 8;

        await fixture.Signal.RequestAsync();

        Assert.Equal(7, fixture.State.Version);
    }

    [Fact]
    public async Task RefreshAsync_WhenPermissionsEndpointReturnsForbidden_DoesNotDeadlock()
    {
        var signal = new PermissionRefreshSignal();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimKeys.UserID, "42"),
            new Claim(ClaimKeys.AccessToken, "token-42")
        ], "test"));
        var authProvider = new MutableAuthenticationStateProvider(principal);
        using var handler = new PermissionForbiddenHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        var api = new APIServices(client, authProvider, signal, new NoOpSessionInvalidationCoordinator());
        var currentUserState = new CurrentUserState(api);
        var state = new PermissionState(new AuthHelper(authProvider, api, currentUserState), signal);

        await Assert.ThrowsAsync<ApiRequestException>(
            () => state.RefreshAsync().WaitAsync(
                TimeSpan.FromSeconds(2),
                TestContext.Current.CancellationToken));

        state.Dispose();
    }

    private static PermissionFixture CreateFixture()
    {
        var groupId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var snapshot = new PermissionSnapshotResDTO
        {
            Version = 7,
            GroupId = groupId,
            Permissions = [Permissions.ReportExport],
            Pages =
            [
                new PermissionSnapshotPageResDTO
                {
                    PageCode = Config.Page_ComponentCode.PageCode.Dashboard,
                    Components =
                    [
                        new PermissionComponentResDTO
                        {
                            ComponentCode = Permissions.RequestProductCatalog,
                            IsVisible = true,
                            IsEnable = true
                        }
                    ]
                }
            ]
        };
        var currentUser = new CurrentUserResDTO
        {
            UserId = 42,
            UserLogin = "employee42",
            FullName = "Nguyen Van A",
            GroupId = groupId,
            GroupCode = "EMPLOYEE",
            GroupName = "Employee"
        };
        var api = new StubApiServices
        {
            GetAsync = (endpoint, _) => Task.FromResult<object?>(endpoint switch
            {
                "api/Auth/me" => currentUser,
                "api/Auth/me/permissions" => snapshot,
                _ => throw new InvalidOperationException($"Unexpected endpoint: {endpoint}")
            })
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimKeys.UserID, "42"),
            new Claim(ClaimKeys.AccessToken, "token-42")
        ], "test"));
        var authProvider = new MutableAuthenticationStateProvider(principal);
        var currentUserState = new CurrentUserState(api);
        var authHelper = new AuthHelper(authProvider, api, currentUserState);
        var signal = new PermissionRefreshSignal();
        var state = new PermissionState(authHelper, signal);
        return new PermissionFixture(state, signal, authProvider, snapshot, groupId);
    }

    private sealed record PermissionFixture(
        PermissionState State,
        PermissionRefreshSignal Signal,
        MutableAuthenticationStateProvider AuthProvider,
        PermissionSnapshotResDTO Snapshot,
        Guid GroupId);

    private sealed class NoOpSessionInvalidationCoordinator : IAuthSessionInvalidationCoordinator
    {
        public Task InvalidateAsync(string reason) => Task.CompletedTask;
    }

    private sealed class PermissionForbiddenHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = request.RequestUri?.AbsolutePath switch
            {
                "/api/Auth/me" => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new CurrentUserResDTO
                    {
                        UserId = 42,
                        UserLogin = "employee42",
                        FullName = "Nguyen Van A"
                    })
                },
                "/api/Auth/me/permissions" => new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = JsonContent.Create(new
                    {
                        detail = "Permission snapshot is stale.",
                        errorCode = "Forbidden"
                    })
                },
                _ => throw new InvalidOperationException($"Unexpected endpoint: {request.RequestUri}")
            };

            return Task.FromResult(response);
        }
    }
}
