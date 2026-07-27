using YamlDotNet.Serialization;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class ComposeConfigurationSyntaxTests
{
    [Theory]
    [InlineData("docker-compose.yml")]
    [InlineData("docker-compose.prod.yml")]
    public void ComposeFile_IsValidYamlWithServices(string fileName)
    {
        var path = Path.Combine(FindRepositoryRoot(), fileName);
        using var reader = File.OpenText(path);
        var document = new DeserializerBuilder()
            .Build()
            .Deserialize<Dictionary<object, object>>(reader);

        Assert.NotNull(document);
        Assert.Contains("name", document.Keys.Select(key => key.ToString()));
        Assert.Contains("services", document.Keys.Select(key => key.ToString()));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
