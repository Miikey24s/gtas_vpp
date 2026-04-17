using gtas_vpp_shared.DTOs.Res.Auth;
using System.Collections.Concurrent;

namespace gtas_vpp_fe.Helpers
{
    public class LoginTicketCache
    {
        private readonly ConcurrentDictionary<string, (sp_Authentication_Login loginData, string server, bool rememberMe)> _cache = new();

        public string Add(sp_Authentication_Login loginData, string server, bool rememberMe)
        {
            var id = Guid.NewGuid().ToString();
            _cache.TryAdd(id, (loginData, server, rememberMe));
            return id;
        }

        public (sp_Authentication_Login loginData, string server, bool rememberMe)? Get(string id)
        {
            if (_cache.TryRemove(id, out var data)) return data;
            return null;
        }
    }
}
