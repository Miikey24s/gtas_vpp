using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Helpers
{
    public class EnvironmentResolver : IEnvironmentResolver
    {
        private readonly string _defaultEnvironment;

        public EnvironmentResolver()
            : this(null)
        {
        }

        public EnvironmentResolver(IConfiguration? configuration)
        {
            _defaultEnvironment = NormalizeEnvironment(configuration?["DatabaseSettings:DefaultEnvironment"])
                ?? "TestEnv";
        }

        public string Resolve(IEnumerable<Claim>? claims, string? fallbackEnv = null)
        {
            var server = claims?.FirstOrDefault(x => x.Type == "Server")?.Value;

            return NormalizeEnvironment(server)
                ?? NormalizeEnvironment(fallbackEnv)
                ?? _defaultEnvironment;
        }

        private static string? NormalizeEnvironment(string? value)
        {
            if (value?.Equals("Test", StringComparison.OrdinalIgnoreCase) == true
                || value?.Equals("TestEnv", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "TestEnv";
            }

            if (value?.Equals("Live", StringComparison.OrdinalIgnoreCase) == true
                || value?.Equals("LiveEnv", StringComparison.OrdinalIgnoreCase) == true)
            {
                return "LiveEnv";
            }

            return null;
        }
    }
}
