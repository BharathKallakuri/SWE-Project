/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Test.Mocks;
using Whiteboard.ViewModels;

namespace Whiteboard.Test.ViewModels;

/// <summary>
/// Eraser state tests
/// </summary>
public class EraserStateTests
{
    /// <summary>
    /// Mock whiteboard client
    /// </summary>
    private readonly MockWhiteboardClient _client = new();

    /// <summary>
    /// Assert that constructor throws <see cref="ArgumentNullException"/> if any of the arguments is null
    /// </summary>
    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EraserState(null!));
    }

    /// <summary>
    /// Assert that pointer pressed when an item is hit, deletes said item
    /// </summary>
    [Fact]
    public void PointerPressed_WhenItemHit_DeletesItem()
    {
        Square square = new() {
            Center = new Point2D(50, 50),
            SideLegth = 40
        };
        _client.Canvas.AddItem(square);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 50));

        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(square.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that pointer pressed when a polygon is hit, deletes said polygon
    /// </summary>
    [Fact]
    public void PointerPressed_WhenPolygonHit_DeletesPolygon()
    {
        Polygon polygon = new() {
            BorderWidth = 4.0,
            Vertices = [
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(100, 100),
                new Point2D(0, 100)
            ]
        };
        _client.Canvas.AddItem(polygon);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 50));

        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(polygon.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that pointer pressed when a line is hit, deletes said line
    /// </summary>
    [Fact]
    public void PointerPressed_WhenLineHit_DeletesLine()
    {
        Line line = new() {
            Begin = new Point2D(10, 10),
            End = new Point2D(100, 10),
            Width = 4.0
        };
        _client.Canvas.AddItem(line);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 10));

        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(line.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that pointer pressed when clicking outside a polygon does not delete it
    /// </summary>
    [Fact]
    public void PointerPressed_WhenPolygonMissed_DoesNotDelete()
    {
        Polygon polygon = new() {
            BorderWidth = 4.0,
            Vertices = [
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(100, 100),
                new Point2D(0, 100)
            ]
        };
        _client.Canvas.AddItem(polygon);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(200, 200));

        Assert.Empty(_client.DeletedItemIds);
    }

    /// <summary>
    /// Assert that pointer pressed when clicking outside a line does not delete it
    /// </summary>
    [Fact]
    public void PointerPressed_WhenLineMissed_DoesNotDelete()
    {
        Line line = new() {
            Begin = new Point2D(10, 10),
            End = new Point2D(100, 10),
            Width = 4.0
        };
        _client.Canvas.AddItem(line);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 50));

        Assert.Empty(_client.DeletedItemIds);
    }

    /// <summary>
    /// Assert that pointer pressed deletes the topmost item between overlapping line and polygon
    /// </summary>
    [Fact]
    public void PointerPressed_WhenLineOverlapsPolygon_DeletesHigherZIndex()
    {
        Polygon polygon = new() {
            Zindex = 1,
            Vertices = [
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(100, 100),
                new Point2D(0, 100)
            ]
        };
        Line line = new() {
            Zindex = 2,
            Begin = new Point2D(0, 50),
            End = new Point2D(100, 50),
            Width = 4.0
        };
        _client.Canvas.AddItem(polygon);
        _client.Canvas.AddItem(line);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(50, 50));

        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(line.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that moving a polygon updates hit testing for the eraser
    /// </summary>
    [Fact]
    public void PointerPressed_AfterPolygonMoved_DeletesAtNewPosition()
    {
        Polygon polygon = new() {
            Vertices = [
                new Point2D(0, 0),
                new Point2D(100, 0),
                new Point2D(100, 100),
                new Point2D(0, 100)
            ]
        };
        _client.Canvas.AddItem(polygon);

        polygon.MoveBy(100, 100);

        EraserState state = new(_client);

        // Old location should miss
        state.PointerPressed(new Point2D(50, 50));
        Assert.Empty(_client.DeletedItemIds);

        // New location should hit and delete
        state.PointerPressed(new Point2D(150, 150));
        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(polygon.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that moving a line updates hit testing for the eraser
    /// </summary>
    [Fact]
    public void PointerPressed_AfterLineMoved_DeletesAtNewPosition()
    {
        Line line = new() {
            Begin = new Point2D(10, 10),
            End = new Point2D(100, 10),
            Width = 4.0
        };
        _client.Canvas.AddItem(line);

        line.MoveBy(0, 50);

        EraserState state = new(_client);

        // Old location should miss
        state.PointerPressed(new Point2D(50, 10));
        Assert.Empty(_client.DeletedItemIds);

        // New location should hit and delete
        state.PointerPressed(new Point2D(50, 60));
        Assert.Single(_client.DeletedItemIds);
        Assert.Equal(line.Id, _client.DeletedItemIds[0]);
    }

    /// <summary>
    /// Assert that pointer pressed, when no item is hit, does not delete anything
    /// </summary>
    [Fact]
    public void PointerPressed_WhenNoItemHit_DoesNotDelete()
    {
        Square square = new() {
            Center = new Point2D(50, 50),
            SideLegth = 20
        };
        _client.Canvas.AddItem(square);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(100, 100));

        Assert.Empty(_client.DeletedItemIds);
    }

    /// <summary>
    /// Assert that the eraser receives presses landing on textboxes
    /// </summary>
    [Fact]
    public void HandlesPointerOnTextbox_IsTrue()
    {
        Assert.True(new EraserState(_client).HandlesPointerOnTextbox);
    }

    /// <summary>
    /// Assert that pointer pressed on a textbox erases it
    /// </summary>
    [Fact]
    public void PointerPressed_WhenTextboxHit_DeletesTextbox()
    {
        Whiteboard.Items.Textbox textbox = new() {
            Center = new Point2D(100, 100),
            Content = "hello"
        };
        _client.Canvas.AddItem(textbox);

        EraserState state = new(_client);
        state.PointerPressed(new Point2D(100, 100));

        Assert.Equal(textbox.Id, Assert.Single(_client.DeletedItemIds));
        Assert.False(_client.Canvas.Items.ContainsKey(textbox.Id));
    }

    /// <summary>
    /// Assert that all other pointer events do nothing
    /// </summary>
    [Fact]
    public void PointerMoved_Released_CaptureLost_DoNothing()
    {
        EraserState state = new(_client);

        state.PointerMoved(new Point2D(10, 10));
        state.PointerReleased(new Point2D(10, 10));
        state.PointerCaptureLost(new Point2D(10, 10));

        Assert.Empty(_client.DeletedItemIds);
    }
}
