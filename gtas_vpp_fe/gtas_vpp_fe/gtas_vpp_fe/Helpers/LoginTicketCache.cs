using gtas_vpp_shared.DTOs.Res.Auth;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace gtas_vpp_fe.Helpers
{
    public sealed class LoginTicketCache
    {
        private sealed record Entry(
            sp_Authentication_Login LoginData,
            bool RememberMe,
            DateTimeOffset CreatedAtUtc,
            DateTimeOffset ExpiresAtUtc);

        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(2);
        private const int DefaultCapacity = 128;
        private readonly ConcurrentDictionary<string, Entry> _cache = new(StringComparer.Ordinal);
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _lifetime;
        private readonly int _capacity;

        public LoginTicketCache()
            : this(TimeProvider.System, DefaultLifetime, DefaultCapacity)
        {
        }

        public LoginTicketCache(TimeProvider timeProvider, TimeSpan lifetime, int capacity)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _lifetime = lifetime > TimeSpan.Zero
                ? lifetime
                : throw new ArgumentOutOfRangeException(nameof(lifetime));
            _capacity = capacity > 0
                ? capacity
                : throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        public string Add(sp_Authentication_Login loginData, bool rememberMe)
        {
            ArgumentNullException.ThrowIfNull(loginData);
            var now = _timeProvider.GetUtcNow();
            RemoveExpired(now);
            TrimToCapacity();

            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var entry = new Entry(loginData, rememberMe, now, now.Add(_lifetime));
            if (!_cache.TryAdd(id, entry))
            {
                throw new InvalidOperationException("Could not create a one-time login ticket.");
            }

            return id;
        }

        public (sp_Authentication_Login loginData, bool rememberMe)? Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_cache.TryRemove(id, out var entry))
            {
                return null;
            }

            if (entry.ExpiresAtUtc <= _timeProvider.GetUtcNow())
            {
                return null;
            }

            return (entry.LoginData, entry.RememberMe);
        }

        private void RemoveExpired(DateTimeOffset now)
        {
            foreach (var item in _cache)
            {
                if (item.Value.ExpiresAtUtc <= now)
                {
                    _cache.TryRemove(item.Key, out _);
                }
            }
        }

        private void TrimToCapacity()
        {
            var overflow = _cache.Count - _capacity + 1;
            if (overflow <= 0)
            {
                return;
            }

            foreach (var item in _cache
                         .OrderBy(pair => pair.Value.CreatedAtUtc)
                         .Take(overflow))
            {
                _cache.TryRemove(item.Key, out _);
            }
        }
    }
}
