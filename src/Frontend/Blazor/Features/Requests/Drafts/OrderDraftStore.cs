using System.Text.Json;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Features.Requests.Drafts;

public sealed class OrderDraftStore(IJSRuntime jsRuntime)
{
    public Task ClearOtherPeriodsAsync(string userId, Guid periodId) =>
        jsRuntime.InvokeVoidAsync(
            "vppDrafts.clearUserExceptPeriod",
            userId,
            periodId.ToString("N"))
            .AsTask();

    public Task SaveAsync(string storageKey, OrderDraftSnapshot draft)
    {
        var json = JsonSerializer.Serialize(draft);
        return jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, json).AsTask();
    }

    public async Task<OrderDraftSnapshot?> RestoreAsync(
        string storageKey,
        string currentUserId,
        Guid currentPeriodId,
        DateTime nowUtc)
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var draft = JsonSerializer.Deserialize<OrderDraftSnapshot>(json);
        if (draft is not null)
        {
            // Draft từ các phiên bản cũ có thể ghi Items=null; chuẩn hóa để giữ khả năng khôi phục.
            draft.Items ??= [];
        }

        if (draft is not null
            && OrderDraftStoragePolicy.CanRestore(
                draft.UserId,
                draft.PeriodId,
                draft.SavedAtUtc,
                currentUserId,
                currentPeriodId,
                nowUtc))
        {
            return draft;
        }

        await RemoveAsync(storageKey);
        return null;
    }

    public Task RemoveAsync(string storageKey) =>
        jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey).AsTask();
}

public sealed class OrderDraftSnapshot
{
    public string UserId { get; set; } = string.Empty;
    public Guid PeriodId { get; set; }
    public string? Description { get; set; }
    public string? SupplementReason { get; set; }
    public List<OrderDraftItemSnapshot> Items { get; set; } = [];
    public DateTime SavedAtUtc { get; set; }
}

public sealed class OrderDraftItemSnapshot
{
    public Guid VppId { get; set; }
    public string? VppCode { get; set; }
    public string? VppName { get; set; }
    public string? UomCode { get; set; }
    public string? UomName { get; set; }
    public int Qty { get; set; } = 1;
    public string? Description { get; set; }
}
