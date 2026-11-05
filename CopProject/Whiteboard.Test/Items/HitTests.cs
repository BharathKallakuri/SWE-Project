// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
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
}
