/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Microsoft.UI;
using Whiteboard.Interfaces;
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
    private readonly ToolbarInfo _toolbar = new(Colors.Black, 3, Colors.Red);

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
}
