/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Pen tool is selected
/// </summary>
/// <inheritdoc/>
// Temporary exclusion from code coverage
public class PenState : IViewModelState
{
    /// <summary>
    /// The whiteboard client
    /// </summary>
    private readonly IWhiteboardClient _whiteboardClient;

    /// <summary>
    /// The canvas UI
    /// </summary>
    private readonly ICanvasPreview _preview;

    /// <summary>
    /// Current toolbar information
    /// </summary>
    private readonly ToolbarInfo _toolbarInfo;

    /// <summary>
    /// Flag to check whether the user is currently drawing on screen
    /// </summary>
    private bool _isDrawing = false;

    /// <summary>
    /// Active polyline
    /// </summary>
    private IFreehand? _freehand;

    public PenState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(toolbarInfo);

        _whiteboardClient = whiteboardClient;
        _preview = preview;
        _toolbarInfo = toolbarInfo;
    }

    public void PointerPressed(Point2D point)
    {
        // Set flag to begin drawing
        _isDrawing = true;

        // Create a new polyline with brush and fill properties taken from the toolbar
        _freehand = new FreehandItem() {
            CreatedBy = _whiteboardClient.UserId,

            StrokeColor = _toolbarInfo.BorderColor,
            StrokeWidth = _toolbarInfo.BorderWidth,

            // Begin the polyline with the first point
            Points = [point]
        };

        // Preview the drawing
        _preview.ShowPreview(_freehand);
    }

    public void PointerMoved(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Add the current cursor point to the polyline
        _freehand.Points.Add(point);

        // Preview the drawing
        _preview.ShowPreview(_freehand);
    }

    public void PointerReleased(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Stop preview
        _preview.ClearPreview();

        // Send to whiteboard client state manager off the UI thread
        Task.Run(() => _whiteboardClient.AddItem(_freehand));

        // Reset polyline state back to null
        _freehand = null;
    }

    public void PointerCaptureLost(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Stop preview
        _preview.ClearPreview();

        // Reset polyline state back to null
        _freehand = null;
    }
}
