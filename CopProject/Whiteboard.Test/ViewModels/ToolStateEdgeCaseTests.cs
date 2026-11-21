/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Models;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;

namespace Whiteboard.Test.ViewModels;

public class ToolStateEdgeCaseTests
{
    private static readonly ToolbarInfo s_toolbar = new(
        Color.FromArgb(255, 0, 0, 0),
        2,
        Color.FromArgb(255, 255, 255, 255));

    [Fact]
    public void LineState_IdlePointerMovedAndReleased_NoOp()
    {
        MockWhiteboardClient client = new();
        MockCanvasPreview preview = new();
        LineState state = new(client, preview, s_toolbar);

        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));

        Assert.Empty(client.AddedItems);
        Assert.True(state.HandlesPointerOnTextbox);
        state.PointerDoubleTapped(new Point2D(10, 10));
    }

    [Fact]
    public void PolygonState_IdlePointerEventsAndSingleVertexCaptureLost_NoOp()
    {
        MockWhiteboardClient client = new();
        MockCanvasPreview preview = new();
        PolygonState state = new(client, preview, s_toolbar);

        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));
        state.PointerDoubleTapped(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        // Single vertex + capture lost should reset without adding a polygon
        state.PointerPressed(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(20, 20));

        Assert.Empty(client.AddedItems);
    }

    [Fact]
    public void IViewModelState_DefaultInterfaceMethods_DoNotThrow()
    {
        IViewModelState state = new MinimalState();

        state.PointerDoubleTapped(new Point2D(0, 0));
        state.Reset();
        Assert.False(state.HandlesPointerOnTextbox);
    }

    private sealed class MinimalState : IViewModelState
    {
        public bool HandlesPointerOnTextbox => false;
        public void PointerPressed(Point2D point) { }
        public void PointerMoved(Point2D point) { }
        public void PointerReleased(Point2D point) { }
        public void PointerCaptureLost(Point2D point) { }
    }
}
