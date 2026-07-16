using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_fe.Services;

/// <summary>
/// Server-authoritative display identity. Cookie claims intentionally contain
/// only stable session data; role, department and profile fields come from /me.
/// </summary>
public sealed class CurrentUserState(IAPIServices apiServices)
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public CurrentUserResDTO? Current { get; private set; }

    public bool IsLoaded { get; private set; }

    public event Action? Changed;

    public async Task<CurrentUserResDTO?> EnsureLoadedAsync()
    {
        if (IsLoaded)
        {
            return Current;
        }

        await _loadLock.WaitAsync();
        try
        {
            if (!IsLoaded)
            {
                Current = await apiServices.GetFromApiAsync<CurrentUserResDTO>("api/Auth/me");
                IsLoaded = Current is not null;
                Changed?.Invoke();
            }

            return Current;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate()
    {
        Current = null;
        IsLoaded = false;
        Changed?.Invoke();
    }
}
