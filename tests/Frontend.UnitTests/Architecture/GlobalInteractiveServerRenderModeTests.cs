using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class GlobalInteractiveServerRenderModeTests
{
    [Fact]
    public void App_UsesGlobalInteractiveServerForHeadAndRoutes()
    {
        var appSource = ReadSource("Components", "App.razor");

        Assert.Contains("<HeadOutlet @rendermode=\"InteractiveServer\" />", appSource, StringComparison.Ordinal);
        Assert.Contains("<Routes @rendermode=\"InteractiveServer\" />", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveWebAssembly", appSource, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveAuto", appSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DescendantComponents_InheritTheGlobalRenderMode()
    {
        var componentRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor", "Components");
        var appPath = Path.Combine(componentRoot, "App.razor");
        var offenders = Directory
            .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, appPath, StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("@rendermode", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(componentRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void ServerRegistration_DoesNotEnableWebAssemblyRendering()
    {
        var serviceRegistration = ReadSource(
            "Platform",
            "Composition",
            "FrontendServiceCollectionExtensions.cs");
        var endpointMapping = ReadSource(
            "Platform",
            "Composition",
            "FrontendApplicationExtensions.cs");
        var compositionSource = serviceRegistration + endpointMapping;

        Assert.Contains("AddInteractiveServerComponents", serviceRegistration, StringComparison.Ordinal);
        Assert.Contains("AddInteractiveServerRenderMode", endpointMapping, StringComparison.Ordinal);
        Assert.DoesNotContain("AddInteractiveWebAssemblyComponents", compositionSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AddInteractiveWebAssemblyRenderMode", compositionSource, StringComparison.Ordinal);
        Assert.Contains("options.MaximumReceiveMessageSize = 64 * 1024", serviceRegistration, StringComparison.Ordinal);
        Assert.DoesNotContain("50 * 1024 * 1024", serviceRegistration, StringComparison.Ordinal);
        Assert.Contains("options.ClientTimeoutInterval = TimeSpan.FromSeconds(60)", serviceRegistration, StringComparison.Ordinal);
        Assert.Contains("options.HandshakeTimeout = TimeSpan.FromSeconds(30)", serviceRegistration, StringComparison.Ordinal);
    }

    private static string ReadSource(params string[] relativeSegments)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");
        return File.ReadAllText(Path.Combine([projectRoot, .. relativeSegments]));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
