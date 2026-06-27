using System.Security.Claims;

namespace gtas_vpp_be.Service.Helpers
{
    public interface IEnvironmentResolver
    {
        string Resolve(IEnumerable<Claim>? claims, string? fallbackEnv = null);
    }
}
