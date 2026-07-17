namespace gtas_vpp_fe.Components.Pages.VPPRequest;

/// <summary>
/// Centralized state shared between the 3 wizard steps.
/// Parent Page_OrderCreate owns this instance and passes it as CascadingValue.
/// </summary>
public class OrderCreateContext
{
    public string Mode { get; set; } = "new";       // "new" | "edit" | "copy" | "additional"
    public Guid? EditOrderId { get; set; }
    public Guid? CopyFromId { get; set; }
    public bool IsAdditional { get; set; }
    public bool IsEdit => EditOrderId.HasValue;
    public bool IsCopy => string.Equals(Mode, "copy", StringComparison.OrdinalIgnoreCase) || CopyFromId.HasValue;

    public string? Description { get; set; }
    public Guid? BaseRequestId { get; set; }
    public string? BaseRequestCode { get; set; }
    public string? SupplementReason { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<SelectedItem> SelectedItems { get; set; } = new();
    public bool DraftRecovered { get; set; }

    public event Action? OnStateChanged;
    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    public int TotalQty => SelectedItems.Sum(x => x.Qty);

    public sealed class SelectedItem
    {
        public Guid VppId { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public int Qty { get; set; } = 1;
        public string? Description { get; set; }
    }
}
