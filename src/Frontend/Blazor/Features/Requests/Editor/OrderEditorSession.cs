using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Features.Requests.Editor;

public enum OrderEditorStep
{
    Products,
    Review
}

public enum OrderEditorValidationError
{
    None,
    SupplementReasonRequired,
    EmptySelection,
    InvalidItem,
    QuantityLimitExceeded
}

public sealed class OrderEditorSession
{
    private readonly List<SelectedItem> _selectedItems = [];

    public bool IsAdditional { get; set; }
    public string? Description { get; set; }
    public Guid? BaseRequestId { get; set; }
    public string? BaseRequestCode { get; set; }
    public string? SupplementReason { get; set; }
    public byte[]? RowVersion { get; set; }
    public IReadOnlyList<SelectedItem> SelectedItems => _selectedItems;
    public OrderEditorStep CurrentStep { get; private set; } = OrderEditorStep.Products;

    public int SelectedItemCount => _selectedItems.Count;
    public int TotalQuantity => _selectedItems.Sum(item => item.Quantity);

    public event Action? Changed;

    public void SelectStep(OrderEditorStep step)
    {
        if (!Enum.IsDefined(step))
        {
            return;
        }

        CurrentStep = step;
        NotifyChanged();
    }

    public void MoveNext()
    {
        if (CurrentStep == OrderEditorStep.Review)
        {
            return;
        }

        CurrentStep = OrderEditorStep.Review;
        NotifyChanged();
    }

    public void MovePrevious()
    {
        if (CurrentStep == OrderEditorStep.Products)
        {
            return;
        }

        CurrentStep = OrderEditorStep.Products;
        NotifyChanged();
    }

    public bool IsSelected(Guid vppId) => _selectedItems.Any(item => item.VppId == vppId);

    public SelectedItem? FindSelectedItem(Guid vppId) =>
        _selectedItems.FirstOrDefault(item => item.VppId == vppId);

    public bool TryAddItem(SelectedItem item)
    {
        if (IsSelected(item.VppId))
        {
            return false;
        }

        _selectedItems.Add(item);
        NotifyChanged();
        return true;
    }

    public bool RemoveItem(SelectedItem item)
    {
        if (!_selectedItems.Remove(item))
        {
            return false;
        }

        NotifyChanged();
        return true;
    }

    public void ReplaceItems(IEnumerable<SelectedItem> items)
    {
        _selectedItems.Clear();
        _selectedItems.AddRange(items);
    }

    public void ChangeQuantity(SelectedItem item, int delta)
    {
        item.Quantity = Math.Clamp(item.Quantity + delta, 1, item.EffectiveMaxQuantityPerOrder);
        NotifyChanged();
    }

    public void SetQuantity(SelectedItem item, object? value)
    {
        var parsed = int.TryParse(value?.ToString(), out var quantity) ? quantity : 1;
        item.Quantity = Math.Clamp(parsed, 1, item.EffectiveMaxQuantityPerOrder);
        NotifyChanged();
    }

    public void UpdateItemDescription(SelectedItem item, string? value)
    {
        item.Description = value;
        NotifyChanged();
    }

    public void UpdateOrderNote(string? value)
    {
        if (IsAdditional)
        {
            SupplementReason = value;
        }
        else
        {
            Description = value;
        }

        NotifyChanged();
    }

    public int GetItemNumber(SelectedItem item) => _selectedItems.IndexOf(item) + 1;

    public OrderEditorValidationError ValidateForSubmission()
    {
        if (IsAdditional
            && (string.IsNullOrWhiteSpace(SupplementReason)
                || SupplementReason.Trim().Length is < 5 or > 500))
        {
            return OrderEditorValidationError.SupplementReasonRequired;
        }

        if (_selectedItems.Count == 0)
        {
            return OrderEditorValidationError.EmptySelection;
        }

        if (_selectedItems.Any(item => item.VppId == Guid.Empty || item.Quantity <= 0))
        {
            return OrderEditorValidationError.InvalidItem;
        }

        return _selectedItems.Any(ExceedsQuantityLimit)
            ? OrderEditorValidationError.QuantityLimitExceeded
            : OrderEditorValidationError.None;
    }

    public bool ExceedsQuantityLimit(SelectedItem item)
        => item.Quantity > item.EffectiveMaxQuantityPerOrder;

    public bool IsAtQuantityLimit(SelectedItem item)
        => item.Quantity >= item.EffectiveMaxQuantityPerOrder;

    public void NotifyChanged() => Changed?.Invoke();

    public sealed class SelectedItem
    {
        public Guid VppId { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public int Quantity { get; set; } = 1;
        public int MaxQuantityPerOrder { get; set; } = VppOrderQuantityLimits.Default;
        public int EffectiveMaxQuantityPerOrder => Math.Clamp(
            MaxQuantityPerOrder,
            VppOrderQuantityLimits.Minimum,
            VppOrderQuantityLimits.Maximum);
        public string? Description { get; set; }
    }
}
