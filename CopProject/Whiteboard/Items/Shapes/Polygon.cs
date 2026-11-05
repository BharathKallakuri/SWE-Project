// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Polygon : Shape, IPolygon
{
    public override Rect2D BoundingBox
    {
        get {
            if (Vertices == null || Vertices.Count == 0)
            {
                return new Rect2D(Left, Top, 0, 0);
            }

            double minX = Vertices.Min(v => v.X);
            double maxX = Vertices.Max(v => v.X);
            double minY = Vertices.Min(v => v.Y);
            double maxY = Vertices.Max(v => v.Y);

            return new Rect2D(
                minX,
                minY,
                maxX - minX,
                maxY - minY
            );
        }
    }

    public override bool HitTest(Point2D point)
    {
        throw new NotImplementedException();
    }

    public List<Point2D> Vertices { get; set; } = [];
}
