using gtas_vpp_be.Service.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gtas_vpp_be.Tests.BaseServicesTests;

public class EnvironmentResolverTests
{
    [Theory]
    [InlineData(DatabaseBinding.TestEnvironment)]
    [InlineData(DatabaseBinding.LiveEnvironment)]
    public void Resolve_ReturnsDeploymentBinding(string environmentName)
    {
        var resolver = new EnvironmentResolver(CreateBinding(environmentName));

        var result = resolver.Resolve();

        Assert.Equal(environmentName, result);
    }

    [Fact]
    public void Resolve_HasNoClaimOrFallbackParameters()
    {
        var method = typeof(IEnvironmentResolver).GetMethod(nameof(IEnvironmentResolver.Resolve));

        Assert.NotNull(method);
        Assert.Empty(method.GetParameters());
    }

    private static DatabaseBinding CreateBinding(string environmentName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultEnvironment"] = environmentName,
                [$"ConnectionStrings:{environmentName}"] =
                    $"Server=localhost;Database=GTAS_{environmentName};Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();

        return DatabaseBinding.Create(configuration);
    }
}
