using gtas_vpp_fe.Platform.Composition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace gtas_vpp_fe.Tests.Platform.Composition;

public sealed class FrontendRuntimeSettingsTests
{
    [Fact]
    public void Resolve_UsesConfiguredDevelopmentValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = "https://localhost:7267",
                ["ApiSettings:ConnectTimeoutSeconds"] = "7",
                ["ApiSettings:RequestTimeoutSeconds"] = "45",
                ["DataProtection:KeysPath"] = "C:\\gtas-test-keys"
            })
            .Build();

        var settings = FrontendRuntimeSettings.Resolve(
            configuration,
            new TestWebHostEnvironment(Environments.Development));

        Assert.Equal(new Uri("https://localhost:7267"), settings.ApiBaseUri);
        Assert.Equal(TimeSpan.FromSeconds(7), settings.ApiConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(45), settings.ApiRequestTimeout);
        Assert.Equal("C:\\gtas-test-keys", settings.DataProtectionKeysPath);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, settings.AuthCookieSecurePolicy);
        Assert.True(settings.BypassApiServerCertificateValidation);
    }

    [Fact]
    public void Resolve_UsesSecureProductionDefaults()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = "https://api.example.com"
            })
            .Build();

        var settings = FrontendRuntimeSettings.Resolve(
            configuration,
            new TestWebHostEnvironment(Environments.Production));

        Assert.Equal(TimeSpan.FromSeconds(5), settings.ApiConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.ApiRequestTimeout);
        Assert.Equal("/app/keys", settings.DataProtectionKeysPath);
        Assert.Equal(CookieSecurePolicy.Always, settings.AuthCookieSecurePolicy);
        Assert.False(settings.BypassApiServerCertificateValidation);
    }

    private sealed class TestWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "gtas_vpp_fe.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
