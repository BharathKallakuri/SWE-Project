/// Owner   : Sasi Kiran
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Selection;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.Selection;

/// <summary>
/// Verifies resizing behavior for selected whiteboard items.
/// </summary>
public class SelectionResizeTests
{
    /// <summary>
    /// Verifies that a rectangle is resized from its top-left anchor.
    /// </summary>
    [Fact]
    public void ResizeSelected_ScalesRectangleFromTopLeftAnchor()
    {
        WhiteboardCanvas canvas = new();
        Rectangle rectangle = new() {
            Center = new Point2D(50, 40),
            Width = 40,
            Height = 20
        };
        canvas.AddItem(rectangle);

        SelectionState selection = new();
        selection.SelectSingle(rectangle.Id);
        SelectionService service = new(canvas, selection);

        bool resized = service.ResizeSelected(rectangle, new Point2D(80, 70));
        Rectangle result = (Rectangle)canvas.Items[rectangle.Id];

        Assert.True(resized);
        Assert.Equal(50, result.Width);
        Assert.Equal(40, result.Height);
        Assert.Equal(30, result.Left);
        Assert.Equal(30, result.Top);
    }

    /// <summary>
    /// Verifies that resizing scales every point in a freehand item.
    /// </summary>
    [Fact]
    public void ResizeSelected_ScalesFreehandPoints()
    {
        WhiteboardCanvas canvas = new();
        FreehandItem freehand = new() {
            Points = [new Point2D(10, 20), new Point2D(30, 40)]
        };
        canvas.AddItem(freehand);

        SelectionState selection = new();
        selection.SelectSingle(freehand.Id);
        SelectionService service = new(canvas, selection);

        bool resized = service.ResizeSelected(freehand, new Point2D(50, 80));
        FreehandItem result = (FreehandItem)canvas.Items[freehand.Id];

        Assert.True(resized);
        Assert.Equal(new Point2D(10, 20), result.Points[0]);
        Assert.Equal(new Point2D(50, 80), result.Points[1]);
    }

    [Fact]
    public void SelectionService_Constructor_ThrowsOnNullArguments()
    {
        WhiteboardCanvas canvas = new();
        SelectionState selection = new();

        Assert.Throws<ArgumentNullException>(() => new SelectionService(null!, selection));
        Assert.Throws<ArgumentNullException>(() => new SelectionService(canvas, null!));
    }

    [Fact]
    public void ResizeSelected_ReturnsFalseWhenUnselectedOrMissingFromCanvas()
    {
        WhiteboardCanvas canvas = new();
        Rectangle rect = new() { Center = new Point2D(50, 50), Width = 20, Height = 20 };
        canvas.AddItem(rect);

        SelectionState selection = new();
        SelectionService service = new(canvas, selection);

        Assert.False(service.ResizeSelected(rect, new Point2D(100, 100)));

        selection.SelectSingle(rect.Id);
        canvas.DeleteItem(rect.Id);
        Assert.False(service.ResizeSelected(rect, new Point2D(100, 100)));
    }

    [Fact]
    public void ResizeSelected_HandlesZeroDimensionBoundingBox()
    {
        WhiteboardCanvas canvas = new();
        Line horizontal = new() { Begin = new Point2D(10, 20), End = new Point2D(30, 20) };
        Line vertical = new() { Begin = new Point2D(10, 20), End = new Point2D(10, 40) };
        canvas.AddItem(horizontal);
        canvas.AddItem(vertical);

        SelectionState selection = new();
        SelectionService service = new(canvas, selection);

        selection.SelectSingle(horizontal.Id);
        Assert.True(service.ResizeSelected(horizontal, new Point2D(50, 25)));

        selection.SelectSingle(vertical.Id);
        Assert.True(service.ResizeSelected(vertical, new Point2D(15, 60)));
    }

    [Fact]
    public void ResizeSelected_ScalesAllSupportedItemTypes()
    {
        WhiteboardCanvas canvas = new();
        SelectionState selection = new();
        SelectionService service = new(canvas, selection);

        Textbox textbox = new() { Center = new Point2D(50, 50), Width = 40, Height = 20 };
        Line line = new() { Begin = new Point2D(10, 10), End = new Point2D(30, 30) };
        Square square = new() { Center = new Point2D(50, 50), SideLegth = 20 };
        Circle circle = new() { Center = new Point2D(50, 50), Radius = 10 };
        Ellipse ellipse = new() { Center = new Point2D(50, 50), Width = 40, Height = 20 };
        EqTriangle triangle = new() { Center = new Point2D(50, 50), SideLength = 20 };
        Polygon polygon = new() { Vertices = [new Point2D(10, 10), new Point2D(30, 10), new Point2D(20, 30)] };

        IItem[] items = [textbox, line, square, circle, ellipse, triangle, polygon];
        foreach (IItem item in items)
        {
            canvas.AddItem(item);
            selection.SelectSingle(item.Id);
            Assert.True(service.ResizeSelected(item, new Point2D(120, 120), "tester"));
            Assert.True(canvas.Items.ContainsKey(item.Id));
        }
    }

    [Fact]
    public void RotateSelected_ThrowsNotImplementedException()
    {
        SelectionService service = new(new WhiteboardCanvas(), new SelectionState());
        Assert.Throws<NotImplementedException>(() => service.RotateSelected(45));
    }

    [Fact]
    public void SelectionState_ToggleWhenPrimaryIsNull_RemovesWithoutClearingPrimary()
    {
        SelectionState state = new();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        state.SelectSingle(first);
        state.Toggle(second);
        state.Toggle(second); // PrimarySelectedId becomes null
        Assert.Null(state.PrimarySelectedId);

        state.Toggle(first); // Removes first while PrimarySelectedId is null
        Assert.False(state.IsSelected(first));
        Assert.Null(state.PrimarySelectedId);
    }

    [Fact]
    public void SelectState_ResizeLifecycle_CommitsAndReverts()
    {
        MockWhiteboardClient client = new();
        MockCanvasPreview preview = new();
        ToolbarInfo toolbar = new(Color.FromArgb(255, 0, 0, 0), 2, Color.FromArgb(255, 255, 255, 255));
        SelectState state = new(client, preview, toolbar);

        // No selection -> BeginResize false, Resize/EndResize no-op
        Assert.False(state.BeginResize());
        state.Resize(new Point2D(100, 100));
        state.EndResize(commit: true);

        Rectangle rect = new() { Center = new Point2D(50, 50), Width = 40, Height = 20 };
        client.Canvas.AddItem(rect);

        // Select item
        state.PointerPressed(new Point2D(50, 50));
        state.PointerReleased(new Point2D(50, 50));

        // BeginResize + Resize + EndResize(commit: false) -> reverts
        Assert.True(state.BeginResize());
        state.Resize(new Point2D(120, 120));
        state.EndResize(commit: false);
        Assert.Equal(40, ((Rectangle)client.Canvas.Items[rect.Id]).Width);

        // BeginResize + Resize + EndResize(commit: true) -> commits via EditItem
        Assert.True(state.BeginResize());
        state.Resize(new Point2D(120, 120));
        state.EndResize(commit: true);
        Assert.Contains(client.EditedItems, i => i.Id == rect.Id);
    }

    [Fact]
    public void SelectState_PointerIdleAndNoOpBranches()
    {
        MockWhiteboardClient client = new();
        MockCanvasPreview preview = new();
        ToolbarInfo toolbar = new(Color.FromArgb(255, 0, 0, 0), 2, Color.FromArgb(255, 255, 255, 255));
        SelectState state = new(client, preview, toolbar);

        // Idle pointer events should no-op
        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        Rectangle rect = new() { Center = new Point2D(50, 50), Width = 40, Height = 20 };
        client.Canvas.AddItem(rect);

        // Press without subscriber on SelectionChanged, move with zero delta, then capture lost without move
        state.PointerPressed(new Point2D(50, 50));
        state.PointerMoved(new Point2D(50, 50));
        state.PointerCaptureLost(new Point2D(50, 50));

        // Press on same already-selected item (previous == SelectedItemId)
        state.PointerPressed(new Point2D(50, 50));
        state.PointerReleased(new Point2D(50, 50));

        // ClearSelection with no subscriber
        state.ClearSelection();
        Assert.Null(state.SelectedItemId);
    }
}
