using gtas_vpp_fe.Platform.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace gtas_vpp_fe.Tests.Platform.Auth;

public sealed class AuthSessionInvalidationCoordinatorTests
{
    [Theory]
    [InlineData("", "/logoutprocess?reason=session-invalid")]
    [InlineData("  expired token/401  ", "/logoutprocess?reason=expired%20token%2F401")]
    public async Task InvalidateAsync_NormalizesReasonAndForcesFullLogoutNavigation(
        string reason,
        string expectedUri)
    {
        var navigation = new RecordingNavigationManager();
        var sut = new AuthSessionInvalidationCoordinator(
            navigation,
            NullLogger<AuthSessionInvalidationCoordinator>.Instance);

        await sut.InvalidateAsync(reason);

        var result = Assert.Single(navigation.Navigations);
        Assert.Equal(expectedUri, result.Uri);
        Assert.True(result.ForceLoad);
    }

    [Fact]
    public async Task InvalidateAsync_NavigatesOnlyOncePerScopedCoordinator()
    {
        var navigation = new RecordingNavigationManager();
        var sut = new AuthSessionInvalidationCoordinator(
            navigation,
            NullLogger<AuthSessionInvalidationCoordinator>.Instance);

        await sut.InvalidateAsync("first-rejection");
        await sut.InvalidateAsync("second-rejection");

        var result = Assert.Single(navigation.Navigations);
        Assert.Equal("/logoutprocess?reason=first-rejection", result.Uri);
    }

    private sealed class RecordingNavigationManager : NavigationManager
    {
        public RecordingNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        public List<NavigationRecord> Navigations { get; } = [];

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Navigations.Add(new NavigationRecord(uri, forceLoad));
        }
    }

    private sealed record NavigationRecord(string Uri, bool ForceLoad);
}
