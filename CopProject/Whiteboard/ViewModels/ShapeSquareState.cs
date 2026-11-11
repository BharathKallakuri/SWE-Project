/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Shape Square tool is selected
/// </summary>
// Temporary exclusion from code coverage
public class ShapeSquareState : IViewModelState
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
    /// Start point of the square
    /// </summary>
    private Point2D _startPoint = new(0, 0);

    /// <summary>
    /// Active square being previewed
    /// </summary>
    private ISquare? _square;

    public ShapeSquareState(IWhiteboardClient whiteboardClient, ICanvasPreview preview, ToolbarInfo toolbarInfo)
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

        // Record the location where the pointer was pressed
        _startPoint = point;

        // Create a new square with brush and fill properties taken from the toolbar
        _square = new Square {
            CreatedBy = _whiteboardClient.UserId,

            BorderColor = _toolbarInfo.BorderColor,
            BorderWidth = _toolbarInfo.BorderWidth,
            FillColor = _toolbarInfo.FillColor,

            Center = new(_startPoint.X, _startPoint.Y),
            SideLegth = 0
        };

        // Preview the square
        _preview.ShowPreview(_square);
    }

    public void PointerMoved(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _square is null)
        {
            return;
        }

        // Compute side length as the minimum of the X and Y distances
        double side = Math.Min(
            Math.Abs(point.X - _startPoint.X),
            Math.Abs(point.Y - _startPoint.Y)
        );

        // Calculate center X and Y
        double centerX = point.X >= _startPoint.X ? _startPoint.X + (side / 2.0) : _startPoint.X - (side / 2.0);
        double centerY = point.Y >= _startPoint.Y ? _startPoint.Y + (side / 2.0) : _startPoint.Y - (side / 2.0);

        // Set square center
        _square.MoveBy(centerX - _square.Center.X, centerY - _square.Center.Y);

        // Set square side length
        _square.SideLegth = side;

        // Preview the square
        _preview.ShowPreview(_square);
    }

    public void PointerReleased(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _square is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Stop preview
        _preview.ClearPreview();

        // Send to whiteboard client state manager off the UI thread
        // But only if it has a non zero length
        if (_square.SideLegth > 0)
        {
            ISquare item = _square;
            Task.Run(() => _whiteboardClient.AddItem(item));
        }

        // Reset square state back to null
        _square = null;
    }

    public void PointerCaptureLost(Point2D point)
    {
        // Return early if the user is not drawing
        if (!_isDrawing || _square is null)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Remove partially formed square from the canvas
        _preview.ClearPreview();

        // Reset square state back to null
        _square = null;
    }
}
