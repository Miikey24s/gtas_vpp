using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_fe.Services;

/// <summary>
/// Danh tính hiển thị do server quyết định. Cookie claim chủ động chỉ chứa dữ liệu
/// session ổn định; role, phòng ban và hồ sơ được lấy từ /me.
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
