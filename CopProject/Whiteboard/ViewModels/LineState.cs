/// Owner   : Raagam Hitesh Parmar
/// Roll No : 112301024
/// LICENSE : MIT

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Line tool is selected,
/// follows a click-move-click user interaction pattern
/// </summary>
/// <inheritdoc/>
public class LineState : ViewModelStateBase
{
    /// <summary>
    /// Kinds of substates
    /// </summary>
    private enum SubState
    {
        /// <summary>
        /// Waiting for user to begin drawing the line by pressing down
        /// </summary>
        WaitingForStart,

        /// <summary>
        /// Waiting for the user to end drawing the line by pressing down again or lifting up the first press
        /// </summary>
        WaitingForEnd
    }

    /// <summary>
    /// Active substate
    /// </summary>
    private SubState _subState = SubState.WaitingForStart;

    /// <summary>
    /// Start point of the line
    /// </summary>
    private Point2D _startPoint = new(0, 0);

    /// <summary>
    /// Active line
    /// </summary>
    private ILine? _line;

    public LineState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo) : base(whiteboardClient, preview, toolbarInfo)
    {
    }

    public override void PointerPressed(Point2D point)
    {
        if (_subState == SubState.WaitingForStart)
        {
            // set flag to begin drawing
            _isDrawing = true;

            // Record the location where the pointer was pressed
            _startPoint = point;

            // Await user to end the drawing
            _subState = SubState.WaitingForEnd;

            // Create a new line item
            _line = new Line {
                CreatedBy = WhiteboardClient.UserId,

                Color = ToolbarInfo.BorderColor,
                Width = ToolbarInfo.BorderWidth,

                Begin = _startPoint,
                End = _startPoint,

                // Add the Z index information
                Zindex = GetNextZIndex()
            };

            Trace.TraceInformation($"[Whiteboard][LineState] Line started at ({_startPoint.X}, {_startPoint.Y}).");
            // Preview the line
            Preview.ShowPreview(_line);
        }
        else if (_subState == SubState.WaitingForEnd)
        {
            // User pressed again to register second endpoint
            if (_line is not null)
            {
                // Set the line endpoint to current pointer location
                _line.End = point;

                // Send to whiteboard client state manager off the UI thread
                // But only if it has a non zero length
                if (HasNonZeroLength(_startPoint, point))
                {
                    ILine item = _line;
                    Trace.TraceInformation($"[Whiteboard][LineState] Line completed: from ({_startPoint.X}, {_startPoint.Y}) to ({point.X}, {point.Y}).");
                    DrawingTask = Task.Run(() => WhiteboardClient.AddItem(item));
                }
            }

            Reset();
        }
    }

    public override void PointerMoved(Point2D point)
    {
        // Return early if the state machine is waiting for the first pressed event
        if (_subState != SubState.WaitingForEnd || _line is null)
        {
            return;
        }

        // Set the line endpoint to the current pointer location
        _line.End = point;

        // Preview the line
        Preview.ShowPreview(_line);
    }

    public override void PointerReleased(Point2D point)
    {
        /*
         * Support both click-move-click and click-drag-release
         * If the pointer was dragged significantly beyond the start point (> 5px),
         * complete the line on release. A normal click will have near-zero distance
         * and remain in WaitingForEnd for the second click.
         */

        if (_subState == SubState.WaitingForEnd && _line is not null)
        {
            double dx = point.X - _startPoint.X;
            double dy = point.Y - _startPoint.Y;

            if ((dx * dx + dy * dy) > 25.0)
            {
                _line.End = point;
                ILine item = _line;
                DrawingTask = Task.Run(() => WhiteboardClient.AddItem(item));
                Reset();
            }
        }
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
    /// Reset the state machine and canvas preview
    /// </summary>
    public override void Reset()
    {
        _subState = SubState.WaitingForStart;
        _line = null;
        base.Reset();
    }

    /// <summary>
    /// Check if the distance between given points is non-zero with some buffer
    /// </summary>
    /// <param name="p1">First point</param>
    /// <param name="p2">Second point</param>
    private static bool HasNonZeroLength(Point2D p1, Point2D p2)
    {
        return Math.Abs(p1.X - p2.X) > 0.5 || Math.Abs(p1.Y - p2.Y) > 0.5;
    }
}
