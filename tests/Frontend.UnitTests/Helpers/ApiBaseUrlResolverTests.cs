using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public class ApiBaseUrlResolverTests
{
    [Fact]
    public void Resolve_ThrowsForMissingBaseUrl()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ApiBaseUrlResolver.Resolve(null));
        Assert.Equal("ApiSettings:BaseUrl is not configured.", exception.Message);
    }

    [Fact]
    public void Resolve_LeavesConcreteHostUntouched()
    {
        var result = ApiBaseUrlResolver.Resolve("http://backend:8080");
        Assert.Equal("http://backend:8080/", result.ToString());
    }

    [Theory]
    [InlineData("http://0.0.0.0:5092", "http://127.0.0.1:5092/")]
    [InlineData("http://+:5092", "http://127.0.0.1:5092/")]
    [InlineData("http://*:5092", "http://127.0.0.1:5092/")]
    public void Resolve_NormalizesWildcardHttpHosts(string rawBaseUrl, string expected)
    {
        var result = ApiBaseUrlResolver.Resolve(rawBaseUrl);
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData("https://0.0.0.0:7267", "https://localhost:7267/")]
    [InlineData("https://[::]:7267", "https://localhost:7267/")]
    public void Resolve_NormalizesWildcardHttpsHosts(string rawBaseUrl, string expected)
    {
        var result = ApiBaseUrlResolver.Resolve(rawBaseUrl);
        Assert.Equal(expected, result.ToString());
    }

    [Theory]
    [InlineData(true, "https://localhost:7267", true)]
    [InlineData(true, "https://127.0.0.1:7267", true)]
    [InlineData(true, "http://127.0.0.1:5092", false)]
    [InlineData(true, "https://backend:8080", false)]
    [InlineData(false, "https://localhost:7267", false)]
    public void ShouldBypassServerCertificateValidation_MatchesEnvironmentAndAddress(
        bool isDevelopment,
        string rawBaseUrl,
        bool expected)
    {
        var baseUri = new Uri(rawBaseUrl);
        var result = ApiBaseUrlResolver.ShouldBypassServerCertificateValidation(isDevelopment, baseUri);
        Assert.Equal(expected, result);
    }
}
