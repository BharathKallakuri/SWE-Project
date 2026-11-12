/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Windows.UI;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Pen state tests
/// </summary>
public class PenStateTests
{
    /// <summary>
    /// Mock whiteboard client
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Mock canvas previewer
    /// </summary>
    private readonly MockCanvasPreview _preview = new();

    /// <summary>
    /// A random toolbar
    /// </summary>
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 3, Color.FromArgb(255, 255, 0, 0));

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PenState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PenState(_client, _preview, null!));
    }

    /// <summary>
    /// Assert that pressing the mouse pointer at position (X, Y) initiates the pen correctly
    /// </summary>
    [Fact]
    public void PointerPressed_InitializesDrawingAndShowsPreview()
    {
        PenState state = new(_client, _preview, _toolbar);

        // Simulate pointer press
        Point2D start = new(10, 20);
        state.PointerPressed(start);

        // Assert that only a single item is shown so far
        Assert.Single(_preview.ShownItems);

        // Assert that the item shown implements IFreehand
        IFreehand freehand = Assert.IsAssignableFrom<IFreehand>(_preview.LastShownItem);
        // Assert that it has only single point on it
        Assert.Single(freehand.Points);

        // Assert that the single point matches the start point
        Assert.Equal(start, freehand.Points[0]);

        // Assert that the toolbar information matches
        Assert.Equal(_toolbar.BorderColor, freehand.StrokeColor);
        Assert.Equal(_toolbar.BorderWidth, freehand.StrokeWidth);

        // Assert that the user id matches
        Assert.Equal(_client.UserId, freehand.CreatedBy);

        // Assert that the index matches
        Assert.Equal(0, freehand.Zindex);
    }

    /// <summary>
    /// Asser that the Z index is correctly instantiated when items are present on the whiteboard
    /// </summary>
    [Fact]
    public void PointerPressed_AssignsTopZIndexWhenItemsExist()
    {
        // Add some items on the whiteboard
        _client.Canvas.AddItem(new Square { Zindex = 5 });
        _client.Canvas.AddItem(new Square { Zindex = 12 });

        PenState state = new(_client, _preview, _toolbar);

        // Simluate a pointer press event
        state.PointerPressed(new Point2D(0, 0));

        IFreehand freehand = Assert.IsAssignableFrom<IFreehand>(_preview.LastShownItem);

        // Assert its Z index matches (1 + max(5, 12))
        Assert.Equal(13, freehand.Zindex);
    }
}
