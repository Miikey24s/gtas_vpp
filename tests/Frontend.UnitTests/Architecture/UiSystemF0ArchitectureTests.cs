using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiSystemF0ArchitectureTests
{
    [Fact]
    public void AppStylesheets_LoadRadzenBaseBeforeProjectOverrides()
    {
        var app = File.ReadAllText(Path.Combine(GetFrontendRoot(), "Components", "App.razor"));
        var radzenThemeIndex = app.IndexOf("<RadzenTheme", StringComparison.Ordinal);
        var darkBaseIndex = app.IndexOf("material-dark-base.css", StringComparison.Ordinal);
        var lightBaseIndex = app.IndexOf("material-base.css", StringComparison.Ordinal);

        Assert.True(radzenThemeIndex >= 0, "App.razor must render RadzenTheme globally.");
        Assert.True(
            darkBaseIndex > radzenThemeIndex && lightBaseIndex > radzenThemeIndex,
            "Explicit Radzen base styles must follow RadzenTheme before project overrides.");

        foreach (var projectStylesheet in new[]
                 {
                     "css/vpp-tokens.css",
                     "css/vpp-radzen-theme.css",
                     "css/vpp-layout.css",
                     "gtas_vpp_fe.styles.css",
                     "css/vpp-casing.css",
                     "css/vpp-toast.css"
                 })
        {
            var projectIndex = app.IndexOf(projectStylesheet, StringComparison.Ordinal);
            Assert.True(projectIndex > lightBaseIndex, $"Load {projectStylesheet} after the Radzen base theme.");
        }

        var tokenIndex = app.IndexOf("css/vpp-tokens.css", StringComparison.Ordinal);
        var bridgeIndex = app.IndexOf("css/vpp-radzen-theme.css", StringComparison.Ordinal);
        Assert.True(tokenIndex < bridgeIndex, "Foundation tokens must load before the Radzen integration bridge.");
        Assert.DoesNotContain("app.css", app, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellBreakpoint_MatchesRadzenResponsiveBoundary()
    {
        var root = GetFrontendRoot();
        var layout = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var responsive = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-responsive.css"));
        var interactions = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));

        Assert.Contains("@media (max-width: 768px)", layout, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout::after {\n        display: none;", layout.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 769px)", layout, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout:has(> .rz-sidebar.vpp-sidebar)", layout, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 768px)", responsive, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 769px) and (max-width: 1199px)", responsive, StringComparison.Ordinal);
        Assert.Contains("window.matchMedia(\"(min-width: 769px)\")", interactions, StringComparison.Ordinal);
        Assert.DoesNotContain("@media (max-width: 767px)", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("@media (max-width: 767px)", responsive, StringComparison.Ordinal);
    }

    [Fact]
    public void ToastClose_RemainsAFlatIconWithoutBorderOrFocusLine()
    {
        var toastStyles = File.ReadAllText(Path.Combine(GetFrontendRoot(), "wwwroot", "css", "vpp-toast.css"));
        var normalized = toastStyles.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains(".rz-notification-close {", normalized, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", normalized, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0 !important;", normalized, StringComparison.Ordinal);
        Assert.Contains(".rz-notification-close:focus-visible {", normalized, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain(".rz-notification-close:focus-visible {\n    box-shadow: inset", normalized, StringComparison.Ordinal);
    }

    private static string GetFrontendRoot()
        => Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");

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
