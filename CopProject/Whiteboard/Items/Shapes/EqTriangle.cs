// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class EqTriangle : Shape, IEqTriangle
{
    public Point2D Center { get; init; } = new(0, 0);

    public override Rect2D BoundingBox => new(
        Center.X - (SideLength / 2.0),
        Center.Y - ((2.0 / 3.0) * GetTriangleHeight(SideLength)),
        SideLength,
        GetTriangleHeight(SideLength)
    );

    public override bool HitTest(Point2D point)
    {
        Rect2D box = BoundingBox;
        Point2D p1 = new Point2D(box.Left + (SideLength / 2.0), box.Top);
        Point2D p2 = new Point2D(box.Left, box.Top + GetTriangleHeight(SideLength));
        Point2D p3 = new Point2D(box.Left + SideLength, box.Top + GetTriangleHeight(SideLength));

        double d1 = Sign(point, p1, p2);
        double d2 = Sign(point, p2, p3);
        double d3 = Sign(point, p3, p1);

        bool hasNeg = (d1 < 0.0) || (d2 < 0.0) || (d3 < 0.0);
        bool hasPos = (d1 > 0.0) || (d2 > 0.0) || (d3 > 0.0);

        return !(hasNeg && hasPos);
    }
    private static double GetTriangleHeight(double sideLength)
    {
        return (Math.Sqrt(3.0) / 2.0) * sideLength;
    }

    private static double Sign(Point2D p1, Point2D p2, Point2D p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    public double SideLength { get; set; } = 10;
}
