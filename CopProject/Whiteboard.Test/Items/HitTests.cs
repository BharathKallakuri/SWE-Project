// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
using Whiteboard.Items;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Items;

public class HitTests : BaseGenerator
{
    private const double TestTolerance = 1e-4;

    /// <summary>
    /// Test hit testing for circle for inside, boundary, and outside points.
    /// </summary>
    [Fact]
    public void Circle()
    {
        var circle = new Whiteboard.Items.Shapes.Circle() {
            Center = RandomPointGenerator.Generate(),
            Radius = (Rng.NextDouble() * 100) + 1.0
        };

        // Center point should hit
        Assert.True(circle.HitTest(circle.Center));

        // Points inside circle should hit
        var insidePoint = new Point2D(circle.Center.X + (circle.Radius * 0.5), circle.Center.Y);
        Assert.True(circle.HitTest(insidePoint));

        // Boundary points (accounting for float roundoff with test tolerance) should hit
        var boundaryRight = new Point2D(circle.Center.X + (circle.Radius - TestTolerance), circle.Center.Y);
        Assert.True(circle.HitTest(boundaryRight));

        var boundaryTop = new Point2D(circle.Center.X, circle.Center.Y - (circle.Radius - TestTolerance));
        Assert.True(circle.HitTest(boundaryTop));

        // Point outside the circle should miss
        var outsidePoint = new Point2D(circle.Center.X + circle.Radius + 1.0, circle.Center.Y);
        Assert.False(circle.HitTest(outsidePoint));

        var boundingBoxCorner = new Point2D(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius);
        Assert.False(circle.HitTest(boundingBoxCorner));
    }

    /// <summary>
    /// Test hit testing for ellipse for inside, boundary, and outside points.
    /// </summary>
    [Fact]
    public void Ellipse()
    {
        var ellipse = new Whiteboard.Items.Shapes.Ellipse() {
            Center = RandomPointGenerator.Generate(),
            Width = (Rng.NextDouble() * 100) + 2.0,
            Height = (Rng.NextDouble() * 100) + 2.0
        };

        double rx = ellipse.Width / 2.0;
        double ry = ellipse.Height / 2.0;

        // Center point should hit
        Assert.True(ellipse.HitTest(ellipse.Center));

        // Point inside ellipse should hit
        var insidePoint = new Point2D(ellipse.Center.X + (rx * 0.5), ellipse.Center.Y + (ry * 0.5));
        Assert.True(ellipse.HitTest(insidePoint));

        // Boundary points (accounting for float roundoff with test tolerance) should hit
        var boundaryRight = new Point2D(ellipse.Center.X + (rx - TestTolerance), ellipse.Center.Y);
        Assert.True(ellipse.HitTest(boundaryRight));

        var boundaryTop = new Point2D(ellipse.Center.X, ellipse.Center.Y - (ry - TestTolerance));
        Assert.True(ellipse.HitTest(boundaryTop));

        var boundaryBottom = new Point2D(ellipse.Center.X, ellipse.Center.Y + (ry - TestTolerance));
        Assert.True(ellipse.HitTest(boundaryBottom));

        // Point outside along axis should miss
        var outsidePoint = new Point2D(ellipse.Center.X + rx + 1.0, ellipse.Center.Y);
        Assert.False(ellipse.HitTest(outsidePoint));

        // Corner of the bounding box is outside the ellipse
        var boundingBoxCorner = new Point2D(ellipse.Center.X + rx, ellipse.Center.Y + ry);
        Assert.False(ellipse.HitTest(boundingBoxCorner));

        // Ellipse with non-positive dimensions should return false
        var degenerate = new Whiteboard.Items.Shapes.Ellipse() {
            Center = ellipse.Center,
            Width = 0,
            Height = 10
        };
        Assert.False(degenerate.HitTest(ellipse.Center));
    }

    /// <summary>
    /// Test hit testing for rectangle for inside, boundary, and outside points.
    /// </summary>
    [Fact]
    public void Rectangle()
    {
        var rect = new Whiteboard.Items.Shapes.Rectangle() {
            Center = RandomPointGenerator.Generate(),
            Width = (Rng.NextDouble() * 100) + 2.0,
            Height = (Rng.NextDouble() * 100) + 2.0
        };

        // Center should hit
        Assert.True(rect.HitTest(rect.Center));

        // Boundary points should hit
        Assert.True(rect.HitTest(new Point2D(rect.BoundingBox.Left, rect.BoundingBox.Top)));
        Assert.True(rect.HitTest(new Point2D(rect.BoundingBox.Left + rect.BoundingBox.Width, rect.BoundingBox.Top + rect.BoundingBox.Height)));
        Assert.True(rect.HitTest(new Point2D(rect.BoundingBox.Left, rect.Center.Y)));

        // Points outside should miss
        Assert.False(rect.HitTest(new Point2D(rect.BoundingBox.Left - 5.0, rect.Center.Y)));
        Assert.False(rect.HitTest(new Point2D(rect.Center.X, rect.BoundingBox.Top - 5.0)));
        Assert.False(rect.HitTest(new Point2D(rect.BoundingBox.Left + rect.BoundingBox.Width + 5.0, rect.Center.Y)));
    }

    /// <summary>
    /// Test hit testing for square for inside, boundary, and outside points.
    /// </summary>
    [Fact]
    public void Square()
    {
        var square = new Whiteboard.Items.Shapes.Square() {
            Center = RandomPointGenerator.Generate(),
            SideLegth = (Rng.NextDouble() * 100) + 2.0
        };

        // Center should hit
        Assert.True(square.HitTest(square.Center));

        // Boundary points should hit
        Assert.True(square.HitTest(new Point2D(square.BoundingBox.Left, square.BoundingBox.Top)));
        Assert.True(square.HitTest(new Point2D(square.BoundingBox.Left + square.BoundingBox.Width, square.BoundingBox.Top + square.BoundingBox.Height)));

        // Points outside should miss
        Assert.False(square.HitTest(new Point2D(square.BoundingBox.Left - 5.0, square.Center.Y)));
        Assert.False(square.HitTest(new Point2D(square.Center.X, square.BoundingBox.Top + square.BoundingBox.Height + 5.0)));
    }
}
