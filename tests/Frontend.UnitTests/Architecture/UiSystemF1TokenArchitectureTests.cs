using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiSystemF1TokenArchitectureTests
{
    private static readonly Regex VppVariableDefinition = new(
        @"(?m)^\s*(--vpp-[a-z0-9-]+)\s*:",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex VppVariableReference = new(
        @"var\((--vpp-[a-z0-9-]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void SemanticThemeTokens_AreDefinedForLightAndDarkModes()
    {
        var tokens = ReadCss("vpp-tokens.css");
        var requiredModeTokens = new[]
        {
            "--vpp-neutral-50",
            "--vpp-neutral-900",
            "--vpp-surface-canvas",
            "--vpp-surface-raised",
            "--vpp-surface-control",
            "--vpp-surface-grid-header",
            "--vpp-text-disabled",
            "--vpp-text-tooltip",
            "--vpp-info-strong",
            "--vpp-action-primary",
            "--vpp-action-primary-hover",
            "--vpp-action-primary-active",
            "--vpp-action-primary-subtle",
            "--vpp-grid-stripe-bg",
            "--vpp-grid-hover-bg",
            "--vpp-grid-selected-bg",
            "--vpp-elevation-dialog"
        };

        foreach (var token in requiredModeTokens)
        {
            var definitionCount = Regex.Matches(
                tokens,
                $@"(?m)^\s*{Regex.Escape(token)}\s*:").Count;
            Assert.Equal(2, definitionCount);
        }

        Assert.Contains("--vpp-bg-base: var(--vpp-surface-canvas);", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-bg-elevated: var(--vpp-surface-raised);", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-bg-surface: var(--vpp-surface-panel);", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void RadzenBridge_UsesOnlyDefinedVppTokensForColorAndThemeValues()
    {
        var tokens = ReadCss("vpp-tokens.css");
        var bridge = ReadCss("vpp-radzen-theme.css");
        var definitions = VppVariableDefinition
            .Matches(tokens)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        var missingReferences = VppVariableReference
            .Matches(bridge)
            .Select(match => match.Groups[1].Value)
            .Where(reference => !definitions.Contains(reference))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missingReferences);
        Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}", bridge);
        Assert.DoesNotMatch(@"\b(?:rgb|rgba|hsl|hsla)\(", bridge);
        Assert.Contains("File này chỉ ánh xạ Radzen CSS variables", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-header-background-color: var(--vpp-surface-grid-header);", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-button-primary-background-color: var(--vpp-action-primary);", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyGlobalVisualExperiments_AreNotPartOfTheFoundation()
    {
        var root = GetFrontendRoot();
        var tokens = ReadCss("vpp-tokens.css");
        var polish = ReadCss("vpp-polish.css");
        var app = File.ReadAllText(Path.Combine(root, "wwwroot", "app.css"));
        var appShell = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var authoredStyleOffenders = Directory
            .EnumerateFiles(root, "*.css", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("::-webkit-scrollbar", StringComparison.Ordinal)
                    || source.Contains("scrollbar-width:", StringComparison.Ordinal)
                    || source.Contains("scrollbar-color:", StringComparison.Ordinal)
                    || source.Contains("-ms-overflow-style:", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.DoesNotContain(".vpp-glass", tokens, StringComparison.Ordinal);
        Assert.DoesNotContain("--vpp-shadow-glass", tokens, StringComparison.Ordinal);
        Assert.DoesNotContain("--ppj-logo-", tokens, StringComparison.Ordinal);
        Assert.DoesNotContain("--ppj-logo-", app, StringComparison.Ordinal);
        Assert.DoesNotContain("::-webkit-scrollbar", tokens, StringComparison.Ordinal);
        Assert.DoesNotContain("::-webkit-scrollbar", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("scrollbar-color:", polish, StringComparison.Ordinal);
        Assert.Empty(authoredStyleOffenders);
        Assert.Contains("<body class=\"rz-default-scrollbars\">", appShell, StringComparison.Ordinal);
        Assert.Contains("LEGACY COMPATIBILITY", app, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenAiVisualContract_AllowsGradientsOnlyForFunctionalFeedback()
    {
        var root = GetFrontendRoot();
        var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine("Components", "Pages", "VPPRequest", "Components", "HistoryWorkspaceShell.razor.css"),
            Path.Combine("wwwroot", "app.css"),
            Path.Combine("wwwroot", "css", "vpp-datagrid.css"),
            Path.Combine("wwwroot", "css", "vpp-tabs.css"),
            Path.Combine("wwwroot", "css", "vpp-tokens.css")
        };
        var offenders = Directory
            .EnumerateFiles(root, "*.css", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"(?:linear|radial|conic)-gradient\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Select(path => Path.GetRelativePath(root, path))
            .Where(path => !allowedFiles.Contains(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(offenders);
        Assert.DoesNotContain("gradient(", ReadCss("vpp-login.css"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient(", ReadCss("vpp-wizard.css"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient(", ReadCss("vpp-layout.css"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gradient(", ReadCss("vpp-polish.css"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalMotionTokens_DriveSharedTransientAndLoadingFeedback()
    {
        var tokens = ReadCss("vpp-tokens.css");
        var layout = ReadCss("vpp-layout.css");
        var polish = ReadCss("vpp-polish.css");
        var wizard = ReadCss("vpp-wizard.css");
        var history = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components", "Pages", "VPPRequest", "Components", "HistoryWorkspaceShell.razor.css"));

        foreach (var token in new[]
                 {
                     "--vpp-motion-fast-duration: 120ms;",
                     "--vpp-motion-base-duration: 180ms;",
                     "--vpp-motion-layout-duration: 220ms;",
                     "--vpp-motion-skeleton-duration: 1200ms;",
                     "--vpp-motion-spinner-duration: 900ms;",
                     "--vpp-motion-easing-standard: cubic-bezier(0.2, 0, 0, 1);"
                 })
        {
            Assert.Contains(token, tokens, StringComparison.Ordinal);
        }

        Assert.Contains("var(--vpp-motion-spinner-duration)", layout, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-motion-skeleton-duration)", polish, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-motion-base-duration)", wizard, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-motion-spinner-duration)", history, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-motion-skeleton-duration)", history, StringComparison.Ordinal);
    }

    private static string ReadCss(string fileName)
        => File.ReadAllText(Path.Combine(GetFrontendRoot(), "wwwroot", "css", fileName));

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
