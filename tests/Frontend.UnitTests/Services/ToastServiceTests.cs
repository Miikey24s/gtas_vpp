using gtas_vpp_fe.Services;
using Radzen;
using Xunit;

namespace gtas_vpp_fe.Tests.Services;

public sealed class ToastServiceTests
{
    [Fact]
    public void Notify_NormalizesTransientFeedbackBehavior()
    {
        var radzen = new NotificationService();
        var service = new ToastService(radzen);

        service.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Success,
            Summary = "Saved",
            Detail = "Order saved"
        });

        var message = Assert.Single(radzen.Messages);
        Assert.Equal(4000, message.Duration);
        Assert.True(message.ShowProgress);
        Assert.True(message.CloseOnClick);
    }

    [Fact]
    public void Error_UsesLongerDefaultDuration()
    {
        var radzen = new NotificationService();
        var service = new ToastService(radzen);

        service.Error("Error", "Try again");

        Assert.Equal(7000, Assert.Single(radzen.Messages).Duration);
    }
}
