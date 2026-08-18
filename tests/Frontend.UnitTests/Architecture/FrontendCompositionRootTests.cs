using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class FrontendCompositionRootTests
{
    [Fact]
    public void Program_ReadsAsAStartupOutline()
    {
        var program = ReadFrontendSource("Program.cs");
        var expectedOrder = new[]
        {
            "FrontendRuntimeSettings.Resolve",
            ".AddFrontendPlatform(settings)",
            ".AddFrontendFeatures(settings)",
            ".AddFrontendAuthenticationAndLocalization(settings)",
            "builder.Build()",
            ".UseFrontendPipeline(settings)",
            ".MapFrontendEndpoints()",
            "app.Run()"
        };

        AssertMarkersAppearInOrder(program, expectedOrder);
        Assert.DoesNotContain("AddScoped<", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHttpClient", program, StringComparison.Ordinal);
        Assert.DoesNotContain("UseAuthentication", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapRazorComponents", program, StringComparison.Ordinal);
        Assert.DoesNotContain("#region", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceRegistration_KeepsExplicitLifetimesAndHttpClientFactories()
    {
        var services = ReadFrontendSource(
            "Platform/Composition/FrontendServiceCollectionExtensions.cs");

        foreach (var registration in new[]
                 {
                     "AddScoped<UiBusyState>",
                     "AddScoped<ThemeState>",
                     "AddScoped<IAuthSessionInvalidationCoordinator, AuthSessionInvalidationCoordinator>",
                     "AddScoped<CurrentUserState>",
                     "AddScoped<INotificationRealtimeClient, NotificationRealtimeClient>",
                     "AddScoped<NotificationInboxState>",
                     "AddScoped<OrderDraftStore>",
                     "AddScoped<PeriodSettlementState>",
                     "AddSingleton(new SettlementFeatureOptions(settings.EnableMultiSupplierSettlement))",
                     "AddSingleton<LoginTicketCache>",
                     "AddHttpClient<IAPIServices, APIServices>",
                     "AddHttpClient<AccountApiClient>",
                     "AddHttpClient<AuthenticationApiClient>"
                 })
        {
            Assert.Contains(registration, services, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Scrutor", services, StringComparison.Ordinal);
        Assert.DoesNotContain("Scan(", services, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionInvalidation_IsOwnedByPlatformAuth()
    {
        var owner = ReadFrontendSource(
            "Platform/Auth/AuthSessionInvalidationCoordinator.cs");
        var legacyPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Services",
            "AuthSessionInvalidationCoordinator.cs");

        Assert.Contains("namespace gtas_vpp_fe.Platform.Auth;", owner, StringComparison.Ordinal);
        Assert.False(File.Exists(legacyPath), "Legacy flat session invalidation owner remains.");
    }

    [Fact]
    public void Pipeline_PreservesSecurityAndStaticAssetOrder()
    {
        var application = ReadFrontendSource(
            "Platform/Composition/FrontendApplicationExtensions.cs");

        AssertMarkersAppearInOrder(
            application,
            [
                "UseForwardedHeaders()",
                "UseRequestLocalization",
                "UseExceptionHandler",
                "UseStatusCodePagesWithReExecute",
                "UseStaticFiles()",
                "MapStaticAssets()",
                "UseCookiePolicy()",
                "UseAuthentication()",
                "UseAuthorization()",
                "UseAntiforgery()"
            ]);
        AssertMarkersAppearInOrder(
            application,
            [
                "MapRazorComponents<App>()",
                "AddInteractiveServerRenderMode()",
                "MapLoginEndpoints()",
                "MapHealthChecks(\"/health\").AllowAnonymous()",
                "MapGet("
            ]);
    }

    [Fact]
    public void RuntimeSettings_PreserveConfigurationDefaultsAndEnvironmentBoundaries()
    {
        var settings = ReadFrontendSource(
            "Platform/Composition/FrontendRuntimeSettings.cs");

        Assert.Contains("ApiSettings:BaseUrl", settings, StringComparison.Ordinal);
        Assert.Contains("ApiSettings:ConnectTimeoutSeconds\", 5", settings, StringComparison.Ordinal);
        Assert.Contains("ApiSettings:RequestTimeoutSeconds\", 30", settings, StringComparison.Ordinal);
        Assert.Contains("DataProtection:KeysPath", settings, StringComparison.Ordinal);
        Assert.Contains("\"/app/keys\"", settings, StringComparison.Ordinal);
        Assert.Contains("CookieSecurePolicy.SameAsRequest", settings, StringComparison.Ordinal);
        Assert.Contains("CookieSecurePolicy.Always", settings, StringComparison.Ordinal);
        Assert.Contains("ShouldBypassServerCertificateValidation", settings, StringComparison.Ordinal);
        Assert.Contains("Features:Settlement:MultiSupplierEnabled\", false", settings, StringComparison.Ordinal);
    }

    private static void AssertMarkersAppearInOrder(string source, IReadOnlyList<string> markers)
    {
        var previousIndex = -1;
        foreach (var marker in markers)
        {
            var currentIndex = source.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(currentIndex >= 0, $"Expected marker '{marker}' was not found.");
            Assert.True(
                currentIndex > previousIndex,
                $"Expected marker '{marker}' to appear after the previous startup step.");
            previousIndex = currentIndex;
        }
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var frontendRoot = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor");
        return File.ReadAllText(Path.Combine(
            frontendRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
