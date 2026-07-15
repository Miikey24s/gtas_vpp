using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_fe.State;

public class GlobalClass
{
    public sp_Authentication_Login UserInfo = new();

    private int _busyCounter;

    public bool isBusyPage
    {
        get => Interlocked.CompareExchange(ref _busyCounter, 0, 0) > 0;
        set
        {
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
        }
    }

    public string BaseUrl { get; set; } = string.Empty;
    public string CurrentLanguage { get; set; } = "vi";
}
