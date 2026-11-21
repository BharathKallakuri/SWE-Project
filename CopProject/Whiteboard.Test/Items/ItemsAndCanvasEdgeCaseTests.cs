/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;

namespace Whiteboard.Test.Items;

public class ItemsAndCanvasEdgeCaseTests
{
    [Fact]
    public void FreehandItem_NullOrEmptyPoints_ReturnsEmptyBoundingBoxAndFalseHitTest()
    {
        FreehandItem empty = new();
        Assert.Equal(new Rect2D(0, 0, 0, 0), empty.BoundingBox);
        Assert.False(empty.HitTest(new Point2D(0, 0)));

        FreehandItem nullPoints = new() { Points = null! };
        Assert.Equal(new Rect2D(0, 0, 0, 0), nullPoints.BoundingBox);
        Assert.False(nullPoints.HitTest(new Point2D(0, 0)));
    }

    [Fact]
    public void Item_PerpDist_ZeroLengthSegment_ComputesEuclideanDistance()
    {
        FreehandItem zeroSeg = new() {
            Points = [new Point2D(10, 10), new Point2D(10, 10)],
            StrokeWidth = 2
        };

        Assert.True(zeroSeg.HitTest(new Point2D(10, 10)));
        Assert.False(zeroSeg.HitTest(new Point2D(50, 50)));
    }

    [Fact]
    public void Polygon_NullOrEmptyVertices_ReturnsEmptyBoundingBoxAndFalseHitTest()
    {
        Polygon empty = new() { Vertices = [] };
        Assert.Equal(new Rect2D(0, 0, 0, 0), empty.BoundingBox);
        Assert.False(empty.HitTest(new Point2D(0, 0)));

        Polygon nullVertices = new() { Vertices = null! };
        Assert.Equal(new Rect2D(0, 0, 0, 0), nullVertices.BoundingBox);
        Assert.False(nullVertices.HitTest(new Point2D(0, 0)));
    }

    [Fact]
    public void SquareAndTextbox_HitTest_CoversBoundaryBranches()
    {
        Square square = new() { Center = new Point2D(50, 50), SideLegth = 20 };
        Assert.True(square.HitTest(new Point2D(50, 50)));
        Assert.False(square.HitTest(new Point2D(50, 100)));
        Assert.False(square.HitTest(new Point2D(100, 50)));

        Textbox textbox = new() { Center = new Point2D(50, 50), Width = 40, Height = 20 };
        Assert.True(textbox.HitTest(new Point2D(50, 50)));
        Assert.False(textbox.HitTest(new Point2D(50, 100)));
        Assert.False(textbox.HitTest(new Point2D(100, 50)));
    }

    [Fact]
    public void WhiteboardCanvas_InRect_CoversAllNonOverlappingSides()
    {
        WhiteboardCanvas canvas = new();
        Rectangle rect = new() { Center = new Point2D(50, 50), Width = 20, Height = 20 }; // [40..60, 40..60]
        canvas.AddItem(rect);

        Assert.Single(canvas.InRect(new Rect2D(45, 45, 10, 10))); // overlapping
        Assert.Empty(canvas.InRect(new Rect2D(0, 45, 10, 10)));   // left of item
        Assert.Empty(canvas.InRect(new Rect2D(80, 45, 10, 10)));  // right of item
        Assert.Empty(canvas.InRect(new Rect2D(45, 0, 10, 10)));   // above item
        Assert.Empty(canvas.InRect(new Rect2D(45, 80, 10, 10)));  // below item
    }
}
