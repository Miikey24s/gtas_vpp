using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_fe.State;

public class GlobalClass
{
    public AuthenticationResultDTO UserInfo = new();

    private int _busyCounter;

    public event Action? BusyChanged;

    public bool isBusyPage
    {
        get => Interlocked.CompareExchange(ref _busyCounter, 0, 0) > 0;
        set
        {
            var wasBusy = Interlocked.CompareExchange(ref _busyCounter, 0, 0) > 0;
            if (value)
            {
                Interlocked.Increment(ref _busyCounter);
            }
            else
            {
                var newValue = Interlocked.Decrement(ref _busyCounter);
                if (newValue < 0)
                {
                    Interlocked.Exchange(ref _busyCounter, 0);
                }
            }

            var isBusy = Interlocked.CompareExchange(ref _busyCounter, 0, 0) > 0;
            if (wasBusy != isBusy)
            {
                BusyChanged?.Invoke();
            }
        }
    }

    public string BaseUrl { get; set; } = string.Empty;
    public string CurrentLanguage { get; set; } = "vi";
}
