namespace gtas_vpp_fe.Features.IdentityAccess.State;

public sealed class PermissionRefreshSignal
{
    public event Func<Task>? Requested;

    public async Task RequestAsync()
    {
        var handlers = Requested?.GetInvocationList().Cast<Func<Task>>().ToArray()
            ?? Array.Empty<Func<Task>>();

        foreach (var handler in handlers)
        {
            await handler();
        }
    }
}
