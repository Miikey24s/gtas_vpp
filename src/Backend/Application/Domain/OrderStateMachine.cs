namespace gtas_vpp_be.Service.Domain;

public enum OrderAction
{
    Cancel,
    Update,
    Approve,
    Reject
}

public static class OrderStateMachine
{
    public static int Transition(int fromStatus, OrderAction action)
        => (fromStatus, action) switch
        {
            (1, OrderAction.Update) => 1,
            (1, OrderAction.Cancel) => 4,
            (6, OrderAction.Update) => 6,
            (6, OrderAction.Cancel) => 4,
            (6, OrderAction.Approve) => 7,
            (6, OrderAction.Reject) => 8,
            _ => throw new InvalidOperationException($"Action '{action}' is not allowed from status '{fromStatus}'.")
        };
}