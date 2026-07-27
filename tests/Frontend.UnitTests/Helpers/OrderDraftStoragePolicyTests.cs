using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class OrderDraftStoragePolicyTests
{
    [Fact]
    public void BuildStorageKey_PartitionsByUserPeriodAndRequestType()
    {
        var firstPeriod = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondPeriod = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var regular = OrderDraftStoragePolicy.BuildStorageKey("5615", firstPeriod, false, null);
        var supplement = OrderDraftStoragePolicy.BuildStorageKey("5615", firstPeriod, true, null);
        var nextPeriod = OrderDraftStoragePolicy.BuildStorageKey("5615", secondPeriod, false, null);
        var otherUser = OrderDraftStoragePolicy.BuildStorageKey("9001", firstPeriod, false, null);

        Assert.NotEqual(regular, supplement);
        Assert.NotEqual(regular, nextPeriod);
        Assert.NotEqual(regular, otherUser);
        Assert.StartsWith(OrderDraftStoragePolicy.BuildUserPrefix("5615"), regular);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-23, true)]
    [InlineData(-25, false)]
    [InlineData(1, false)]
    public void CanRestore_RequiresCurrentScopeAndFreshTimestamp(
        int savedHoursFromNow,
        bool expected)
    {
        var periodId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var now = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);

        var result = OrderDraftStoragePolicy.CanRestore(
            "5615",
            periodId,
            now.AddHours(savedHoursFromNow),
            "5615",
            periodId,
            now);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanRestore_RejectsDifferentUserOrPeriod()
    {
        var periodId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var now = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);

        Assert.False(OrderDraftStoragePolicy.CanRestore(
            "9001", periodId, now, "5615", periodId, now));
        Assert.False(OrderDraftStoragePolicy.CanRestore(
            "5615", Guid.NewGuid(), now, "5615", periodId, now));
    }
}
