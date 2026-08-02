using gtas_vpp_fe.Services;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Services;

public sealed class CurrentUserStateTests
{
    [Fact]
    public async Task EnsureLoadedAsync_CoalescesConcurrentRequests()
    {
        var response = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new StubApiServices
        {
            GetAsync = (endpoint, _) =>
            {
                Assert.Equal("api/Auth/me", endpoint);
                return response.Task;
            }
        };
        var state = new CurrentUserState(api);
        var notifications = 0;
        state.Changed += () => notifications++;

        var firstLoad = state.EnsureLoadedAsync();
        var secondLoad = state.EnsureLoadedAsync();
        var currentUser = CreateCurrentUser();
        response.SetResult(currentUser);

        var results = await Task.WhenAll(firstLoad, secondLoad);

        Assert.Equal(1, api.GetCallCount);
        Assert.All(results, result => Assert.Same(currentUser, result));
        Assert.True(state.IsLoaded);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public async Task EnsureLoadedAsync_RetriesAfterNullResponse()
    {
        var responses = new Queue<object?>([null, CreateCurrentUser()]);
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult(responses.Dequeue())
        };
        var state = new CurrentUserState(api);
        var notifications = 0;
        state.Changed += () => notifications++;

        var first = await state.EnsureLoadedAsync();
        var second = await state.EnsureLoadedAsync();

        Assert.Null(first);
        Assert.NotNull(second);
        Assert.Equal(2, api.GetCallCount);
        Assert.True(state.IsLoaded);
        Assert.Equal(2, notifications);
    }

    [Fact]
    public async Task Invalidate_ClearsCachedCurrentUserAndNotifies()
    {
        var api = new StubApiServices
        {
            GetAsync = (_, _) => Task.FromResult<object?>(CreateCurrentUser())
        };
        var state = new CurrentUserState(api);
        var notifications = 0;
        state.Changed += () => notifications++;
        await state.EnsureLoadedAsync();

        state.Invalidate();

        Assert.Null(state.Current);
        Assert.False(state.IsLoaded);
        Assert.Equal(2, notifications);
    }

    private static CurrentUserResDTO CreateCurrentUser() => new()
    {
        UserId = 42,
        UserLogin = "employee42",
        FullName = "Nguyen Van A",
        GroupId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        GroupCode = "EMPLOYEE",
        GroupName = "Employee",
        PrimaryDepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        PrimaryDepartmentCode = "IT",
        PrimaryDepartmentName = "Information Technology"
    };
}
