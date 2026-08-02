namespace gtas_vpp_fe.Platform.State;

public sealed class UiBusyState
{
    private int _activeLeaseCount;

    public event Action? Changed;

    public bool IsBusy => Volatile.Read(ref _activeLeaseCount) > 0;

    public IDisposable Begin()
    {
        if (Interlocked.Increment(ref _activeLeaseCount) == 1)
        {
            Changed?.Invoke();
        }

        return new BusyLease(this);
    }

    private void End()
    {
        if (Interlocked.Decrement(ref _activeLeaseCount) == 0)
        {
            Changed?.Invoke();
        }
    }

    private sealed class BusyLease(UiBusyState owner) : IDisposable
    {
        private UiBusyState? _owner = owner;

        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.End();
    }
}
