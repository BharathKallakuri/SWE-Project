// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces.Shapes;
using Whiteboard.Test.RandomGenerators;

namespace Whiteboard.Test.Items;

public class BoundingBoxTests : BaseGenerator
{
    /// <summary>
    /// Test bounding box for circle is calculated properly
    /// </summary>
    [Fact]
    public void Circle()
    {
        var circle = new Whiteboard.Items.Shapes.Circle() {
            Center = RandomPointGenerator.Generate(),
            Radius = Rng.NextDouble() * 100
        };

        Assert.Equal(circle.Center.X - circle.Radius, circle.BoundingBox.Left);
        Assert.Equal(circle.Center.Y + circle.Radius, circle.BoundingBox.Top);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Width);
        Assert.Equal(circle.Radius * 2, circle.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for ellipse is calculated properly
    /// </summary>
    [Fact]
    public void Ellipse()
    {
        var ellipse = new Whiteboard.Items.Shapes.Ellipse() {
            Center = RandomPointGenerator.Generate(),
            Width = Rng.NextDouble() * 100,
            Height = Rng.NextDouble() * 100
        };

        Assert.Equal(ellipse.Center.X - (ellipse.Width / 2), ellipse.BoundingBox.Left);
        Assert.Equal(ellipse.Center.Y + (ellipse.Height / 2), ellipse.BoundingBox.Top);
        Assert.Equal(ellipse.Width, ellipse.BoundingBox.Width);
        Assert.Equal(ellipse.Height, ellipse.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for equilateral triangle is calculated properly
    /// </summary>
    [Fact]
    public void EqTriangle()
    {
        var eqTriangle = new Whiteboard.Items.Shapes.EqTriangle() {
            Center = RandomPointGenerator.Generate(),
            SideLength = Rng.NextDouble() * 100
        };

        double height = (Math.Sqrt(3.0) / 2.0) * eqTriangle.SideLength;

        Assert.Equal(eqTriangle.Center.X - (eqTriangle.SideLength / 2), eqTriangle.BoundingBox.Left);
        Assert.Equal(eqTriangle.Center.Y + ((2.0 / 3.0) * height), eqTriangle.BoundingBox.Top);
        Assert.Equal(eqTriangle.SideLength, eqTriangle.BoundingBox.Width);
        Assert.Equal(height, eqTriangle.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for square is calculated properly
    /// </summary>
    [Fact]
    public void Square()
    {
        var square = new Whiteboard.Items.Shapes.Square() {
            Center = RandomPointGenerator.Generate(),
            SideLegth = Rng.NextDouble() * 100
        };

        Assert.Equal(square.Center.X - (square.SideLegth / 2), square.BoundingBox.Left);
        Assert.Equal(square.Center.Y + (square.SideLegth / 2), square.BoundingBox.Top);
        Assert.Equal(square.SideLegth, square.BoundingBox.Width);
        Assert.Equal(square.SideLegth, square.BoundingBox.Height);
    }

    /// <summary>
    /// Test bounding box for rectangle is calculated properly
    /// </summary>
    [Fact]
    public void Rectangle()
    {
        var rectangle = new Whiteboard.Items.Shapes.Rectangle() {
            Center = RandomPointGenerator.Generate(),
            Width = Rng.NextDouble() * 100,
            Height = Rng.NextDouble() * 100
        };

        Assert.Equal(rectangle.Center.X - (rectangle.Width / 2), rectangle.BoundingBox.Left);
        Assert.Equal(rectangle.Center.Y + (rectangle.Height / 2), rectangle.BoundingBox.Top);
        Assert.Equal(rectangle.Width, rectangle.BoundingBox.Width);
        Assert.Equal(rectangle.Height, rectangle.BoundingBox.Height);
    }
}
