using gtas_vpp_be.Service.Domain;
using Xunit;

namespace gtas_vpp_be.Tests.Domain;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(1, OrderAction.Update, 1)]
    [InlineData(1, OrderAction.Cancel, 4)]
    [InlineData(6, OrderAction.Update, 6)]
    [InlineData(6, OrderAction.Cancel, 4)]
    [InlineData(6, OrderAction.Approve, 7)]
    [InlineData(6, OrderAction.Reject, 8)]
    public void Transition_ValidCombination_ReturnsExpectedStatus(int fromStatus, OrderAction action, int expectedStatus)
    {
        var result = OrderStateMachine.Transition(fromStatus, action);

        Assert.Equal(expectedStatus, result);
    }

    [Theory]
    [InlineData(1, OrderAction.Approve)]
    [InlineData(1, OrderAction.Reject)]
    [InlineData(4, OrderAction.Cancel)]
    [InlineData(4, OrderAction.Update)]
    [InlineData(4, OrderAction.Approve)]
    [InlineData(4, OrderAction.Reject)]
    [InlineData(7, OrderAction.Cancel)]
    [InlineData(7, OrderAction.Update)]
    [InlineData(7, OrderAction.Approve)]
    [InlineData(7, OrderAction.Reject)]
    [InlineData(8, OrderAction.Cancel)]
    [InlineData(8, OrderAction.Update)]
    [InlineData(8, OrderAction.Approve)]
    [InlineData(8, OrderAction.Reject)]
    public void Transition_InvalidCombination_ThrowsInvalidOperationException(int fromStatus, OrderAction action)
    {
        Assert.Throws<InvalidOperationException>(() => OrderStateMachine.Transition(fromStatus, action));
    }
}
