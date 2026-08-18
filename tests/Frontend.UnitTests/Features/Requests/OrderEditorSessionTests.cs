using gtas_vpp_fe.Features.Requests.Editor;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderEditorSessionTests
{
    [Fact]
    public void StepNavigation_StaysWithinTwoStepsAndPublishesValidSelections()
    {
        var editor = new OrderEditorSession();
        var changeCount = 0;
        editor.Changed += () => changeCount++;

        editor.MovePrevious();
        Assert.Equal(OrderEditorStep.Products, editor.CurrentStep);
        Assert.Equal(0, changeCount);

        editor.MoveNext();
        editor.MoveNext();
        Assert.Equal(OrderEditorStep.Review, editor.CurrentStep);
        Assert.Equal(1, changeCount);

        editor.MovePrevious();
        editor.SelectStep(OrderEditorStep.Review);
        editor.SelectStep((OrderEditorStep)99);

        Assert.Equal(OrderEditorStep.Review, editor.CurrentStep);
        Assert.Equal(3, changeCount);
    }

    [Fact]
    public void SelectionMutations_PreserveOrderClampQuantityAndRejectDuplicates()
    {
        var editor = new OrderEditorSession();
        var changeCount = 0;
        editor.Changed += () => changeCount++;
        var first = new OrderEditorSession.SelectedItem
        {
            VppId = Guid.NewGuid(),
            VppName = "Giấy A4",
            Quantity = 2,
            MaxQuantityPerOrder = 10
        };
        var second = new OrderEditorSession.SelectedItem
        {
            VppId = Guid.NewGuid(),
            VppName = "Bút xanh",
            Quantity = 3
        };

        Assert.True(editor.TryAddItem(first));
        Assert.False(editor.TryAddItem(new OrderEditorSession.SelectedItem { VppId = first.VppId }));
        Assert.True(editor.TryAddItem(second));
        Assert.Equal([first, second], editor.SelectedItems);
        Assert.Equal(5, editor.TotalQuantity);

        editor.ChangeQuantity(first, -10);
        Assert.Equal(1, first.Quantity);
        editor.SetQuantity(first, "12000");
        Assert.Equal(10, first.Quantity);
        editor.SetQuantity(first, "not-a-number");
        Assert.Equal(1, first.Quantity);

        editor.UpdateItemDescription(second, "  dùng cho phòng họp  ");
        Assert.Equal("  dùng cho phòng họp  ", second.Description);
        Assert.True(editor.RemoveItem(first));
        Assert.False(editor.RemoveItem(first));
        Assert.Equal([second], editor.SelectedItems);
        Assert.Equal(7, changeCount);
    }

    [Fact]
    public void UpdateOrderNote_RoutesValueByAdditionalMode()
    {
        var editor = new OrderEditorSession();
        var changeCount = 0;
        editor.Changed += () => changeCount++;

        editor.UpdateOrderNote("ghi chú thường");
        Assert.Equal("ghi chú thường", editor.Description);
        Assert.Null(editor.SupplementReason);

        editor.IsAdditional = true;
        editor.UpdateOrderNote("lý do bổ sung");
        Assert.Equal("ghi chú thường", editor.Description);
        Assert.Equal("lý do bổ sung", editor.SupplementReason);
        Assert.Equal(2, changeCount);
    }

    [Fact]
    public void Validation_PreservesReasonSelectionAndItemPrecedence()
    {
        var editor = new OrderEditorSession { IsAdditional = true };

        Assert.Equal(
            OrderEditorValidationError.SupplementReasonRequired,
            editor.ValidateForSubmission());

        editor.SupplementReason = "đủ lý do";
        Assert.Equal(OrderEditorValidationError.EmptySelection, editor.ValidateForSubmission());

        editor.ReplaceItems(
        [
            new OrderEditorSession.SelectedItem { VppId = Guid.Empty, Quantity = 1 }
        ]);
        Assert.Equal(OrderEditorValidationError.InvalidItem, editor.ValidateForSubmission());

        editor.ReplaceItems(
        [
            new OrderEditorSession.SelectedItem { VppId = Guid.NewGuid(), Quantity = 1 }
        ]);
        Assert.Equal(OrderEditorValidationError.None, editor.ValidateForSubmission());

        editor.SupplementReason = new string('x', 501);
        Assert.Equal(
            OrderEditorValidationError.SupplementReasonRequired,
            editor.ValidateForSubmission());
    }

    [Fact]
    public void ReplaceItems_HydratesWithoutPublishingAnEditEvent()
    {
        var editor = new OrderEditorSession();
        var changeCount = 0;
        editor.Changed += () => changeCount++;

        editor.ReplaceItems(
        [
            new OrderEditorSession.SelectedItem { VppId = Guid.NewGuid(), Quantity = 4 }
        ]);

        Assert.Equal(0, changeCount);
        Assert.Equal(1, editor.SelectedItemCount);
        Assert.Equal(4, editor.TotalQuantity);
    }

    [Fact]
    public void Validation_RejectsHydratedDraftAboveCurrentItemLimit()
    {
        var editor = new OrderEditorSession();
        editor.ReplaceItems(
        [
            new OrderEditorSession.SelectedItem
            {
                VppId = Guid.NewGuid(),
                Quantity = 6,
                MaxQuantityPerOrder = 5
            }
        ]);

        Assert.Equal(
            OrderEditorValidationError.QuantityLimitExceeded,
            editor.ValidateForSubmission());
    }
}
