/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System.Collections.Generic;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Polygon tool is selected,
/// follows a click [move click]* double-click interaction pattern
/// </summary>
public class PolygonState : ViewModelStateBase
{
    /// <summary>
    /// List of vertices commited to be part of the polygon
    /// </summary>
    private readonly List<Point2D> _committedVertices = [];

    /// <summary>
    /// Active polygon
    /// </summary>
    private IPolygon? _previewPolygon;

    public PolygonState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo) : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        // Set the flag to begin drawing
        _isDrawing = true;

        // If clicking close to the starting point and we have at least 3 vertices, complete the polygon
        if (_committedVertices.Count >= 3 && IsNearStart(point))
        {
            CompletePolygon();
            return;
        }

        // Avoid adding redundant duplicate vertex if clicked at or very near the previous vertex
        if (_committedVertices.Count > 0)
        {
            Point2D last = _committedVertices[^1];

            double dx = point.X - last.X;
            double dy = point.Y - last.Y;

            if ((dx * dx + dy * dy) < 9.0) // within 3 pixels
            {
                return;
            }
        }

        // Commit the newly created polygon vertex
        _committedVertices.Add(point);

        UpdatePreview(point);
    }

    public override void PointerMoved(Point2D point)
    {
        // Return early if the user is not drawing or there are no committed vertices
        if (!_isDrawing || _committedVertices.Count == 0)
        {
            return;
        }

        UpdatePreview(point);
    }

    public override void PointerReleased(Point2D point)
    {
        /*
         * Deliberately no-op
         */
    }

    public override void PointerDoubleTapped(Point2D point)
    {
        // Return early if the user is not drawing or there are no committed vertices
        if (!_isDrawing || _committedVertices.Count == 0)
        {
            return;
        }

        // Complete the polygon when double tapping
        CompletePolygon();
    }

    public override void PointerCaptureLost(Point2D point)
    {
        /*
         * Deliberately no-op
         * Pointer capture is released on mouse-up after every click.
         * This must not reset the state machine
         */
    }

    /// <summary>
    /// Update the polygon preview by adding a new vertex
    /// </summary>
    /// <param name="activePoint">New vertex to be added</param>
    private void UpdatePreview(Point2D activePoint)
    {
        List<Point2D> vertices = [.. _committedVertices, activePoint];

        _previewPolygon = new Polygon {
            CreatedBy = WhiteboardClient.UserId,

            BorderColor = ToolbarInfo.BorderColor,
            BorderWidth = ToolbarInfo.BorderWidth,
            FillColor = ToolbarInfo.FillColor,

            Vertices = vertices,

            Zindex = GetNextZIndex()
        };

        Preview.ShowPreview(_previewPolygon);
    }

    /// <summary>
    /// Complete the polygon (into a closed shape) if there are at least 3 committed vertices
    /// </summary>
    private void CompletePolygon()
    {
        if (_committedVertices.Count >= 3)
        {
            IPolygon item = new Polygon {
                CreatedBy = WhiteboardClient.UserId,

                BorderColor = ToolbarInfo.BorderColor,
                BorderWidth = ToolbarInfo.BorderWidth,
                FillColor = ToolbarInfo.FillColor,

                Vertices = [.. _committedVertices],

                Zindex = GetNextZIndex()
            };

            DrawingTask = Task.Run(() => WhiteboardClient.AddItem(item));
        }

        Reset();
    }

    /// <summary>
    /// Reset the state machine and canvas preview
    /// </summary>
    public override void Reset()
    {
        _committedVertices.Clear();
        _previewPolygon = null;

        base.Reset();
    }

    /// <summary>
    /// Check if the given point is sufficiently close to the starting point of the polygon
    /// </summary>
    private bool IsNearStart(Point2D point)
    {
        if (_committedVertices.Count == 0)
        {
            return false;
        }

        Point2D start = _committedVertices[0];

        double dx = point.X - start.X;
        double dy = point.Y - start.Y;

        return (dx * dx + dy * dy) <= 144.0; // within 12 pixels
    }
}
