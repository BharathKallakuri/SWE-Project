/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class LineStateTests
{
    private readonly MockWhiteboardClient _client = new();
    private readonly MockCanvasPreview _preview = new();
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 2.0, Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LineState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new LineState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new LineState(_client, _preview, null!));
    }

    [Fact]
    public void PointerPressed_FirstClick_InitializesLineAndShowsPreview()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);

        state.PointerPressed(start);

        Assert.Single(_preview.ShownItems);
        ILine line = Assert.IsAssignableFrom<ILine>(_preview.LastShownItem);
        Assert.Equal(start.X, line.Begin.X);
        Assert.Equal(start.Y, line.Begin.Y);
        Assert.Equal(start.X, line.End.X);
        Assert.Equal(start.Y, line.End.Y);
        Assert.Equal(_toolbar.BorderColor, line.Color);
        Assert.Equal(_toolbar.BorderWidth, line.Width);
    }

    [Fact]
    public void PointerMoved_WhenWaitingForEnd_UpdatesPreviewEndpoint()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);
        Point2D current = new(50, 80);

        state.PointerPressed(start);
        state.PointerMoved(current);

        Assert.Equal(2, _preview.ShownItems.Count);
        ILine line = Assert.IsAssignableFrom<ILine>(_preview.LastShownItem);
        Assert.Equal(start.X, line.Begin.X);
        Assert.Equal(start.Y, line.Begin.Y);
        Assert.Equal(current.X, line.End.X);
        Assert.Equal(current.Y, line.End.Y);
    }

    [Fact]
    public async Task PointerPressed_SecondClickWithLength_SubmitsLineAndClearsPreview()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);
        Point2D end = new(100, 200);

        state.PointerPressed(start);
        state.PointerMoved(end);
        state.PointerPressed(end); // Second click commits the line

        Assert.Equal(1, _preview.ClearCount);

        await (state.DrawingTask ?? Task.CompletedTask);

        Assert.Single(_client.AddedItems);
        ILine submitted = Assert.IsAssignableFrom<ILine>(_client.AddedItems[0]);
        Assert.Equal(start.X, submitted.Begin.X);
        Assert.Equal(start.Y, submitted.Begin.Y);
        Assert.Equal(end.X, submitted.End.X);
        Assert.Equal(end.Y, submitted.End.Y);
        Assert.Equal(_toolbar.BorderColor, submitted.Color);
        Assert.Equal(_toolbar.BorderWidth, submitted.Width);
    }

    [Fact]
    public void PointerPressed_SecondClickZeroLength_DoesNotSubmit()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);

        state.PointerPressed(start);
        state.PointerPressed(start); // Click on exact same spot

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    [Fact]
    public async Task PointerReleased_DragSignificantDistance_SubmitsLine()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);
        Point2D end = new(100, 200);

        state.PointerPressed(start);
        state.PointerMoved(end);
        state.PointerReleased(end); // Drag-release commits the line

        Assert.Equal(1, _preview.ClearCount);

        await (state.DrawingTask ?? Task.CompletedTask);

        Assert.Single(_client.AddedItems);
        ILine submitted = Assert.IsAssignableFrom<ILine>(_client.AddedItems[0]);
        Assert.Equal(start.X, submitted.Begin.X);
        Assert.Equal(start.Y, submitted.Begin.Y);
        Assert.Equal(end.X, submitted.End.X);
        Assert.Equal(end.Y, submitted.End.Y);
        Assert.Equal(_toolbar.BorderColor, submitted.Color);
        Assert.Equal(_toolbar.BorderWidth, submitted.Width);
    }

    [Fact]
    public async Task PointerReleased_ClickAtSameLocation_DoesNotSubmitPrematurely()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);

        state.PointerPressed(start);
        state.PointerReleased(start); // Mouse-up of initial click

        Assert.Equal(0, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);

        // Can still move and commit with second click
        Point2D end = new(50, 60);
        state.PointerMoved(end);
        state.PointerPressed(end);

        Assert.Equal(1, _preview.ClearCount);
        await (state.DrawingTask ?? Task.CompletedTask);
        Assert.Single(_client.AddedItems);
    }

    [Fact]
    public async Task PointerCaptureLost_DoesNotResetClickMoveClick()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);

        state.PointerPressed(start);
        state.PointerCaptureLost(new Point2D(30, 40));

        // Drawing session is preserved so pointer move still tracks and second click commits
        Assert.Equal(0, _preview.ClearCount);

        Point2D end = new(60, 70);
        state.PointerMoved(end);
        state.PointerPressed(end);

        Assert.Equal(1, _preview.ClearCount);
        await (state.DrawingTask ?? Task.CompletedTask);
        Assert.Single(_client.AddedItems);
    }

    [Fact]
    public void Reset_ClearsPreviewAndResets()
    {
        LineState state = new(_client, _preview, _toolbar);
        Point2D start = new(10, 20);

        state.PointerPressed(start);
        state.Reset();

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }
}
