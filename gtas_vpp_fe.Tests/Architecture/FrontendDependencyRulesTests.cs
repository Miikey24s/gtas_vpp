using gtas_vpp_fe.Services;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class FrontendDependencyRulesTests
{
    [Fact]
    public void FrontendAssembly_DoesNotReferencePersistenceOrObjectMappingLibraries()
    {
        var referencedAssemblies = typeof(APIServices)
            .Assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            referencedAssemblies,
            name => name.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Mapster", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Mapster.", StringComparison.OrdinalIgnoreCase));
    }
}
