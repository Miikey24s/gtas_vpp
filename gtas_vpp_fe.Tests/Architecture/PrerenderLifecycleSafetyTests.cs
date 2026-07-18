using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class PrerenderLifecycleSafetyTests
{
    [Theory]
    [InlineData("Components/Routes.razor.cs")]
    [InlineData("Components/Layout/LeftSidebar.razor.cs")]
    [InlineData("Components/Pages/PermissionAwarePageBase.cs")]
    [InlineData("Components/Pages/Authen/ConfirmEmail.razor.cs")]
    [InlineData("Components/Pages/Report.razor.cs")]
    [InlineData("Components/Pages/VPPRequest/Page_OrderCreate.razor.cs")]
    public void ApiOrSideEffectLifecycle_IsGuardedUntilTheCircuitIsInteractive(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("RendererInfo.IsInteractive", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/Lib/Page_Library.razor")]
    [InlineData("Components/Pages/Permission/Page_Permission.razor")]
    [InlineData("Components/Pages/Report.razor")]
    [InlineData("Components/Pages/VPPRequest/Page_VPPRequest.razor")]
    public void ProtectedPage_RendersAStablePrerenderShell(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("!RendererInfo.IsInteractive", source, StringComparison.Ordinal);
        Assert.Contains("SkeletonPage", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractiveLayouts_ContainRecoverableErrorBoundaries()
    {
        var mainLayout = ReadFrontendSource("Components/Layout/MainLayout.razor");
        var loginLayout = ReadFrontendSource("Components/Layout/LoginLayout.razor");

        Assert.Contains("<ErrorBoundary", mainLayout, StringComparison.Ordinal);
        Assert.Contains("_errorBoundary?.Recover()", mainLayout, StringComparison.Ordinal);
        Assert.Contains("<ErrorBoundary", loginLayout, StringComparison.Ordinal);
        Assert.Contains("_errorBoundary?.Recover()", loginLayout, StringComparison.Ordinal);
    }

    [Fact]
    public void OptionalRoutePermissionGuard_CannotTerminateTheCircuit()
    {
        var routes = ReadFrontendSource("Components/Routes.razor.cs");

        Assert.Contains("catch (OperationCanceledException)", routes, StringComparison.Ordinal);
        Assert.Contains("catch (Exception exception)", routes, StringComparison.Ordinal);
        Assert.Contains("Dynamic permission guard failed", routes, StringComparison.Ordinal);
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe");
        return File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
