/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Collections.Generic;
using System.Linq;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.Items.Shapes;

/// <inheritdoc/>
public class Polygon : Shape, IPolygon
{
    public const double DefaultHitThreshold = 5.0;

    public override Rect2D BoundingBox
    {
        get {
            if (Vertices == null || Vertices.Count == 0)
            {
                return new Rect2D(0, 0, 0, 0);
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

    public override void MoveBy(double dX, double dY)
    {
        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] = Vertices[i] with { X = Vertices[i].X + dX, Y = Vertices[i].Y + dY };
        }
    }

    public override bool HitTest(Point2D point)
    {
        if (Vertices == null || Vertices.Count < 2)
        {
            return false;
        }

        double threshold = Math.Max(DefaultHitThreshold, BorderWidth / 2.0);

        // Fast return path, return if the point is outside the bounding box (with padding)
        Rect2D box = BoundingBox;
        if (point.X < box.Left - threshold ||
            point.X > box.Left + box.Width + threshold ||
            point.Y < box.Top - threshold ||
            point.Y > box.Top + box.Height + threshold)
        {
            return false;
        }

        // Boundary edge hit test
        for (int i = 0; i < Vertices.Count; i++)
        {
            Point2D p1 = Vertices[i];
            Point2D p2 = Vertices[(i + 1) % Vertices.Count];
            if (PerpDist(point, p1, p2) <= threshold)
            {
                return true;
            }
        }

        // Interior hit test using Jordan curve theorem (ray-casting algorithm)
        // Return true iff the point intersects with the edges of the polygon
        // an odd number of times
        if (Vertices.Count >= 3)
        {
            bool inside = false;
            for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count; j = i++)
            {
                Point2D vi = Vertices[i];
                Point2D vj = Vertices[j];

                if ((vi.Y > point.Y) != (vj.Y > point.Y) &&
                    point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
                {
                    inside = !inside;
                }
            }

            if (inside)
            {
                return true;
            }
        }

        return false;
    }

    public List<Point2D> Vertices { get; set; } = [];
}
