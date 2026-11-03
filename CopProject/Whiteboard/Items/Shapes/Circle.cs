// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Circle : Shape, ICircle
{
    public Point2D Center { get; init; } = new(0, 0);

    public double Radius { get; set; } = 10;

    public override Rect2D BoundingBox
    {
        get => new(
            Center.X - Radius,
            Center.Y + Radius,
            Radius * 2,
            Radius * 2
        );

        set {
            Left = value.Left + (value.Width / 2.0);
            Top = value.Top + (value.Height / 2.0);
            Radius = value.Width < value.Height ? value.Width / 2.0 : value.Height / 2.0;
        }
    }

    public override bool HitTest(Point2D point)
    {   
        
        double dx = point.X - Left;
        double dy = point.Y - Top;
        double distanceSquared = (dx * dx) + (dy * dy);
        double radiusSquared = Radius * Radius;
        return distanceSquared <= radiusSquared;
    }
}
