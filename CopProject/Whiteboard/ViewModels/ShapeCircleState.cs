// Owner   : Sasi Kiran
// LICENSE : MIT

using System;
using System.Threading.Tasks;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items.Shapes;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Circle tool is selected.
/// </summary>
public class ShapeCircleState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly ICanvasPreview _preview;
    private readonly ToolbarInfo _toolbarInfo;
    private bool _isDrawing;
    private Point2D _startPoint = new(0, 0);
    private ICircle? _circle;

    public ShapeCircleState(
        IWhiteboardClient whiteboardClient,
        ICanvasPreview preview,
        ToolbarInfo toolbarInfo)
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
        _isDrawing = true;

        _startPoint = point;

        _circle = new Circle {
            CreatedBy = _whiteboardClient.UserId,

            BorderColor = _toolbarInfo.BorderColor,
            BorderWidth = _toolbarInfo.BorderWidth,
            FillColor = _toolbarInfo.FillColor,

            Center = new(_startPoint.X, _startPoint.Y),
            Radius = 0
        };

        _preview.ShowPreview(_circle);
    }

    public void PointerMoved(Point2D point)
    {
        if (!_isDrawing || _circle is null)
        {
            return;
        }

        double diameter = Math.Min(
            Math.Abs(point.X - _startPoint.X),
            Math.Abs(point.Y - _startPoint.Y)
        );

        double centerX = point.X >= _startPoint.X ? _startPoint.X + (diameter / 2.0) : _startPoint.X - (diameter / 2.0);
        double centerY = point.Y >= _startPoint.Y ? _startPoint.Y + (diameter / 2.0) : _startPoint.Y - (diameter / 2.0);

        _circle.MoveBy(centerX - _circle.Center.X, centerY - _circle.Center.Y);

        _circle.Radius = diameter / 2;

        _preview.ShowPreview(_circle);
    }

    public void PointerReleased(Point2D point)
    {
        if (!_isDrawing || _circle is null)
        {
            return;
        }

        _isDrawing = false;

        _preview.ClearPreview();

        // Send to whiteboard client state manager off the UI thread
        // But only if it has a non zero length
        if (_circle.Radius > 0)
        {
            Task.Run(() => _whiteboardClient.AddItem(_circle));
        }

        _circle = null;
    }

    public void PointerCaptureLost(Point2D point)
    {
        if (!_isDrawing || _circle is null)
        {
            return;
        }

        _isDrawing = false;

        _preview.ClearPreview();

        _circle = null;
    }
}
