/// LICENSE : MIT

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Items.Shapes;
using Whiteboard.Models;
using Whiteboard.Services;

namespace Whiteboard.Test.Services;

public class ItemSnapshotTests
{
    [Fact]
    public void Copy_CopiesCircleAndPreservesProperties()
    {
        Circle circle = new() { Center = new Point2D(15, 25), Radius = 12, CreatedBy = "alice" };

        Circle copy = Assert.IsType<Circle>(ItemSnapshot.Copy(circle));

        Assert.Equal(circle.Id, copy.Id);
        Assert.Equal(15, copy.Center.X);
        Assert.Equal(25, copy.Center.Y);
        Assert.Equal(12, copy.Radius);
        Assert.NotSame(circle, copy);
    }

    [Fact]
    public void Copy_HandlesNullCenterAndNullPoints()
    {
        Circle nullCenter = new() { Center = null! };
        FreehandItem nullPoints = new() { Points = null! };

        Circle copiedCircle = Assert.IsType<Circle>(ItemSnapshot.Copy(nullCenter));
        FreehandItem copiedFreehand = Assert.IsType<FreehandItem>(ItemSnapshot.Copy(nullPoints));

        Assert.Null(copiedCircle.Center);
        Assert.Null(copiedFreehand.Points);
    }

    [Fact]
    public void Copy_ThrowsNotSupportedExceptionForUnknownItemType()
    {
        Assert.Throws<NotSupportedException>(() => ItemSnapshot.Copy(new UnsupportedItem()));
    }

    private sealed class UnsupportedItem : Item
    {
        public override Rect2D BoundingBox => new(0, 0, 0, 0);
        public override bool HitTest(Point2D point) => false;
        public override void MoveBy(double dx, double dy) { }
    }
}
