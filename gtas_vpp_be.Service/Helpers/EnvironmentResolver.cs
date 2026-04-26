using System.Security.Claims;

namespace gtas_vpp_be.Service.Helpers
{
    public class EnvironmentResolver : IEnvironmentResolver
    {
        public string Resolve(IEnumerable<Claim>? claims, string? fallbackEnv = null)
        {
            var server = claims?.FirstOrDefault(x => x.Type == "Server")?.Value;

            return server switch
            {
                "Test" => "TestEnv",
                "Live" => "LiveEnv",
                _ => fallbackEnv switch
                {
                    "Test" or "TestEnv" => "TestEnv",
                    "Live" or "LiveEnv" => "LiveEnv",
                    _ => "TestEnv"
                }
            };
        }
    }
}
