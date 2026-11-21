using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Selection;

namespace Whiteboard.Test.Selection;

public class SelectionTests
{

    [Fact]
    public void SelectionState_SelectToggleAndClear()
    {
        // check the basic selection changes.
        SelectionState state = new();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        state.SelectSingle(first);
        state.Toggle(second);
        state.Toggle(second);
        state.Clear();

        Assert.Empty(state.SelectedItemIds);
        Assert.Null(state.PrimarySelectedId);
        Assert.False(state.IsSelected(first));
    }

    [Fact]
    public void SelectionState_TogglingNonPrimaryItemKeepsPrimarySelection()
    {
        // remove a non-primary item.
        SelectionState state = new();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        state.SelectSingle(first);
        state.Toggle(second);
        state.Toggle(first);

        Assert.DoesNotContain(first, state.SelectedItemIds);
        Assert.Equal(second, state.PrimarySelectedId);
    }

    [Fact]
    public void SelectionService_SelectsHitClearsMissAndMovesItem()
    {
        // select and move the rectangle.
        WhiteboardCanvas canvas = new();
        Rectangle rectangle = new() {
            Center = new Point2D(10, 10),
            Width = 10,
            Height = 10
        };
        canvas.AddItem(rectangle);
        SelectionState state = new();
        SelectionService service = new(canvas, state);

        Assert.True(service.SelectAtPoint(rectangle.Center));
        Assert.Equal(rectangle.Id, state.PrimarySelectedId);
        Assert.True(service.MoveSelected(5, 2));
        Assert.False(service.SelectAtPoint(new Point2D(100, 100)));
        Assert.Null(state.PrimarySelectedId);
        Assert.Equal(new Point2D(15, 12), ((Rectangle)canvas.Items[rectangle.Id]).Center);
    }

    [Fact]
    public void SelectionService_MissingSelectedItemReturnsFalse()
    {
        // a missing item cannot be moved.
        SelectionState state = new();
        state.SelectSingle(Guid.NewGuid());

        Assert.False(new SelectionService(new WhiteboardCanvas(), state).MoveSelected(1, 1));
    }

}
