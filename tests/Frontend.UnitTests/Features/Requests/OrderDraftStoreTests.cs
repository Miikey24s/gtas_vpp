using System.Text.Json;
using gtas_vpp_fe.Features.Requests.Drafts;
using Microsoft.JSInterop;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderDraftStoreTests
{
    [Fact]
    public async Task SaveAsync_PreservesTheExistingBrowserJsonContract()
    {
        var periodId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var savedAtUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var js = new RecordingJsRuntime();
        var store = new OrderDraftStore(js);
        var snapshot = new OrderDraftSnapshot
        {
            UserId = "5615",
            PeriodId = periodId,
            Description = "Văn phòng phẩm tháng 7",
            SupplementReason = "Bổ sung theo yêu cầu",
            SavedAtUtc = savedAtUtc,
            Items =
            [
                new OrderDraftItemSnapshot
                {
                    VppId = itemId,
                    VppCode = "ITEM-001",
                    VppName = "Giấy A4",
                    UomCode = "REAM",
                    UomName = "Ram",
                    Qty = 3,
                    Description = "Giao đầu tháng"
                }
            ]
        };

        await store.SaveAsync("vpp.order.draft.key", snapshot);

        var call = Assert.Single(js.Calls);
        Assert.Equal("localStorage.setItem", call.Identifier);
        Assert.Equal("vpp.order.draft.key", call.Arguments[0]);
        var persistedJson = Assert.IsType<string>(call.Arguments[1]);
        using var document = JsonDocument.Parse(persistedJson);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("UserId", out _));
        Assert.True(root.TryGetProperty("PeriodId", out _));
        Assert.True(root.TryGetProperty("Description", out _));
        Assert.True(root.TryGetProperty("SupplementReason", out _));
        Assert.True(root.TryGetProperty("Items", out var items));
        Assert.True(root.TryGetProperty("SavedAtUtc", out _));
        var persistedItem = items[0];
        foreach (var propertyName in new[]
                 {
                     "VppId", "VppCode", "VppName", "UomCode", "UomName", "Qty", "Description"
                 })
        {
            Assert.True(persistedItem.TryGetProperty(propertyName, out _));
        }

        var persisted = JsonSerializer.Deserialize<OrderDraftSnapshot>(persistedJson);
        Assert.NotNull(persisted);
        Assert.Equal("5615", persisted.UserId);
        Assert.Equal(periodId, persisted.PeriodId);
        Assert.Equal(savedAtUtc, persisted.SavedAtUtc);
        var item = Assert.Single(persisted.Items);
        Assert.Equal(itemId, item.VppId);
        Assert.Equal("ITEM-001", item.VppCode);
        Assert.Equal(3, item.Qty);
    }

    [Fact]
    public async Task RestoreAsync_ValidDraft_ReturnsSnapshotWithoutDeletingIt()
    {
        var periodId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var nowUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var js = new RecordingJsRuntime
        {
            StoredJson = $$"""
                {
                  "UserId": "5615",
                  "PeriodId": "{{periodId}}",
                  "Description": "Draft hợp lệ",
                  "SupplementReason": null,
                  "Items": [
                    {
                      "VppId": "66666666-6666-6666-6666-666666666666",
                      "VppCode": "ITEM-LEGACY",
                      "VppName": "Mặt hàng cũ",
                      "UomCode": "EA",
                      "UomName": "Cái",
                      "Qty": 2,
                      "Description": null
                    }
                  ],
                  "SavedAtUtc": "2026-07-16T07:55:00Z"
                }
                """
        };
        var store = new OrderDraftStore(js);

        var restored = await store.RestoreAsync(
            "vpp.order.draft.key",
            "5615",
            periodId,
            nowUtc);

        Assert.NotNull(restored);
        Assert.Equal("Draft hợp lệ", restored.Description);
        Assert.Equal("ITEM-LEGACY", Assert.Single(restored.Items).VppCode);
        Assert.Single(js.Calls);
        Assert.Equal("localStorage.getItem", js.Calls[0].Identifier);
    }

    [Fact]
    public async Task RestoreAsync_LegacyNullItems_NormalizesToEmptyList()
    {
        var periodId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var nowUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var js = new RecordingJsRuntime
        {
            StoredJson = $$"""
                {
                  "UserId": "5615",
                  "PeriodId": "{{periodId}}",
                  "Description": "Draft không có mặt hàng",
                  "SupplementReason": null,
                  "Items": null,
                  "SavedAtUtc": "2026-07-16T07:55:00Z"
                }
                """
        };
        var store = new OrderDraftStore(js);

        var restored = await store.RestoreAsync(
            "vpp.order.draft.key",
            "5615",
            periodId,
            nowUtc);

        Assert.NotNull(restored);
        Assert.Empty(restored.Items);
        Assert.DoesNotContain(js.Calls, call => call.Identifier == "localStorage.removeItem");
    }

    [Fact]
    public async Task RestoreAsync_StaleOrWrongScopeDraft_RemovesStoredPayload()
    {
        var periodId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var nowUtc = new DateTime(2026, 7, 16, 8, 0, 0, DateTimeKind.Utc);
        var js = new RecordingJsRuntime
        {
            StoredJson = JsonSerializer.Serialize(new OrderDraftSnapshot
            {
                UserId = "9001",
                PeriodId = periodId,
                SavedAtUtc = nowUtc.AddDays(-2)
            })
        };
        var store = new OrderDraftStore(js);

        var restored = await store.RestoreAsync(
            "vpp.order.draft.key",
            "5615",
            periodId,
            nowUtc);

        Assert.Null(restored);
        Assert.Equal(
            ["localStorage.getItem", "localStorage.removeItem"],
            js.Calls.Select(call => call.Identifier));
    }

    [Fact]
    public async Task CleanupMethods_UseCanonicalDraftBrowserFunctions()
    {
        var periodId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var js = new RecordingJsRuntime();
        var store = new OrderDraftStore(js);

        await store.ClearOtherPeriodsAsync("5615", periodId);
        await store.RemoveAsync("vpp.order.draft.key");

        Assert.Equal("vppDrafts.clearUserExceptPeriod", js.Calls[0].Identifier);
        Assert.Equal("5615", js.Calls[0].Arguments[0]);
        Assert.Equal(periodId.ToString("N"), js.Calls[0].Arguments[1]);
        Assert.Equal("localStorage.removeItem", js.Calls[1].Identifier);
        Assert.Equal("vpp.order.draft.key", js.Calls[1].Arguments[0]);
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public string? StoredJson { get; set; }
        public List<JsCall> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            var arguments = args ?? [];
            Calls.Add(new JsCall(identifier, arguments));
            if (identifier == "localStorage.getItem")
            {
                return ValueTask.FromResult((TValue)(object?)StoredJson!);
            }

            if (identifier == "localStorage.setItem")
            {
                StoredJson = arguments.ElementAtOrDefault(1)?.ToString();
            }
            else if (identifier == "localStorage.removeItem")
            {
                StoredJson = null;
            }

            return ValueTask.FromResult(default(TValue)!);
        }
    }

    private sealed record JsCall(string Identifier, object?[] Arguments);
}
