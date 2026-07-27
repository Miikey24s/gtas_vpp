using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class FrontendDeploymentRenderSafetyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ProductionNginx_PreservesBlazorWebSocketUpgrade()
    {
        var nginx = ReadRepositoryFile("nginx", "gtas-vpp.conf");

        Assert.Contains("location /_blazor", nginx, StringComparison.Ordinal);
        Assert.Contains("proxy_http_version 1.1", nginx, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header   Upgrade", nginx, StringComparison.Ordinal);
        Assert.Contains("proxy_set_header   Connection", nginx, StringComparison.Ordinal);
        Assert.Contains("proxy_buffering    off", nginx, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendSmoke_ValidatesRenderedLoginAndSignalRWebSockets()
    {
        var smoke = ReadRepositoryFile("deploy", "smoke-frontend.sh");

        Assert.Contains("vpp-login-card", smoke, StringComparison.Ordinal);
        Assert.Contains("_framework/blazor.web", smoke, StringComparison.Ordinal);
        Assert.Contains("/_blazor/negotiate?negotiateVersion=1", smoke, StringComparison.Ordinal);
        Assert.Contains("\"WebSockets\"", smoke, StringComparison.Ordinal);
    }

    [Fact]
    public void DeploymentAndRunner_BothExecuteFrontendSmoke()
    {
        var deploy = ReadRepositoryFile("deploy", "deploy.sh");
        var workflow = ReadRepositoryFile(".github", "workflows", "deploy.yml");

        Assert.Contains("bash deploy/smoke-frontend.sh", deploy, StringComparison.Ordinal);
        Assert.Contains("bash deploy/smoke-frontend.sh", workflow, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([RepositoryRoot, .. pathSegments]));

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
