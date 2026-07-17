namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Small latest-response-wins primitive for interactive pages. It prevents a
/// slower request from painting stale data after the user changes filters.
/// </summary>
public sealed class AsyncLoadVersion
{
    private int _version;

    public int Begin() => Interlocked.Increment(ref _version);

    public bool IsCurrent(int version) => Volatile.Read(ref _version) == version;
}
