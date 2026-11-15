/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Threading;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;
using Windows.UI;
using Xunit;

namespace Whiteboard.Test.ViewModels;

public class PolygonStateTests
{
    private readonly MockWhiteboardClient _client = new();
    private readonly MockCanvasPreview _preview = new();
    private readonly ToolbarInfo _toolbar = new(Color.FromArgb(255, 0, 0, 0), 2.0, Color.FromArgb(255, 255, 0, 0));

    [Fact]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PolygonState(null!, _preview, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PolygonState(_client, null!, _toolbar));
        Assert.Throws<ArgumentNullException>(() => new PolygonState(_client, _preview, null!));
    }

    [Fact]
    public void PointerPressed_FirstClick_AddsVertexAndShowsPreview()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);

        state.PointerPressed(p1);

        Assert.Single(_preview.ShownItems);
        IPolygon poly = Assert.IsAssignableFrom<IPolygon>(_preview.LastShownItem);
        Assert.Equal(_toolbar.FillColor, poly.FillColor);
        Assert.Equal(_toolbar.BorderColor, poly.BorderColor);
    }

    [Fact]
    public void PointerMoved_WhileDrawing_ShowsLivePreviewWithActiveCursor()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);
        Point2D current = new(50, 10);

        state.PointerPressed(p1);
        state.PointerMoved(current);

        Assert.Equal(2, _preview.ShownItems.Count);
        IPolygon poly = Assert.IsAssignableFrom<IPolygon>(_preview.LastShownItem);
        // Includes p1 and the live moving point
        Assert.Equal(2, poly.Vertices.Count);
        Assert.Equal(current.X, poly.Vertices[1].X);
        Assert.Equal(current.Y, poly.Vertices[1].Y);
    }

    [Fact]
    public void PointerDoubleTapped_WithThreeOrMoreVertices_SubmitsPolygon()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(0, 0);
        Point2D p2 = new(50, 0);
        Point2D p3 = new(25, 50);

        state.PointerPressed(p1);
        state.PointerPressed(p2);
        state.PointerPressed(p3);
        state.PointerDoubleTapped(p3); // Complete

        Assert.Equal(1, _preview.ClearCount);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(15));
        Assert.True(wasAdded, "Item was not added in time.");

        Assert.Single(_client.AddedItems);
        IPolygon submitted = Assert.IsAssignableFrom<IPolygon>(_client.AddedItems[0]);
        Assert.Equal(3, submitted.Vertices.Count);
        Assert.Equal(_toolbar.FillColor, submitted.FillColor);
        Assert.Equal(_toolbar.BorderColor, submitted.BorderColor);
    }

    [Fact]
    public void PointerDoubleTapped_WithLessThanThreeVertices_DoesNotSubmit()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(0, 0);
        Point2D p2 = new(50, 0);

        state.PointerPressed(p1);
        state.PointerPressed(p2);
        state.PointerDoubleTapped(p2); // Double tap with only 2 vertices

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }

    [Fact]
    public void PointerPressed_ClickNearStartWithThreeVertices_ClosesAndSubmits()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);
        Point2D p2 = new(50, 10);
        Point2D p3 = new(30, 40);

        state.PointerPressed(p1);
        state.PointerPressed(p2);
        state.PointerPressed(p3);
        // Click very close to start (within 8px)
        state.PointerPressed(new Point2D(12, 11));

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(15));
        Assert.True(wasAdded, "Item was not added in time.");

        Assert.Single(_client.AddedItems);
        IPolygon submitted = Assert.IsAssignableFrom<IPolygon>(_client.AddedItems[0]);
        Assert.Equal(3, submitted.Vertices.Count);
    }

    [Fact]
    public void PointerCaptureLost_DoesNotResetPolygonSession()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);

        state.PointerPressed(p1);
        state.PointerCaptureLost(new Point2D(30, 40));

        // Drawing session preserved
        Assert.Equal(0, _preview.ClearCount);

        // Can continue adding vertices and complete
        Point2D p2 = new(50, 10);
        Point2D p3 = new(30, 40);
        state.PointerPressed(p2);
        state.PointerPressed(p3);
        state.PointerDoubleTapped(p3);

        Assert.Equal(1, _preview.ClearCount);
        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(15));
        Assert.True(wasAdded, "Item was not added in time.");
        Assert.Single(_client.AddedItems);
    }

    [Fact]
    public void PointerPressed_DuplicateAdjacentClicks_Ignored()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);

        state.PointerPressed(p1);
        state.PointerPressed(new Point2D(10.5, 10.5)); // Duplicate/near click ignored

        Point2D p2 = new(50, 10);
        Point2D p3 = new(30, 40);
        state.PointerPressed(p2);
        state.PointerPressed(p3);
        state.PointerDoubleTapped(p3);

        bool wasAdded = _client.ItemAddedEvent.Wait(TimeSpan.FromSeconds(15));
        Assert.True(wasAdded, "Item was not added in time.");
        Assert.Single(_client.AddedItems);
        IPolygon submitted = Assert.IsAssignableFrom<IPolygon>(_client.AddedItems[0]);
        Assert.Equal(3, submitted.Vertices.Count);
    }

    [Fact]
    public void Reset_ClearsPreviewAndResets()
    {
        PolygonState state = new(_client, _preview, _toolbar);
        Point2D p1 = new(10, 10);

        state.PointerPressed(p1);
        state.Reset();

        Assert.Equal(1, _preview.ClearCount);
        Assert.Empty(_client.AddedItems);
    }
}
