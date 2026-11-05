using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.ViewModels;

public class ShapeCircleState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private Circle? _circle;
    private Point _startPoint;
    private bool _isDrawing;

    public ShapeCircleState(
        IWhiteboardClient whiteboardClient,
        Canvas canvas,
        ToolbarInfo toolbarInfo)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(toolbarInfo);

        _whiteboardClient = whiteboardClient;
        _canvas = canvas;
        _toolbarInfo = toolbarInfo;
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!_canvas.CapturePointer(e.Pointer))
        {
            return;
        }

        _startPoint = e.GetCurrentPoint(_canvas).Position;
        _circle = new Circle {
            CreatedBy = _whiteboardClient.UserId,
            BorderColor = GetColor(_toolbarInfo.BorderBrush),
            FillColor = GetColor(_toolbarInfo.FillBrush),
            BorderWidth = _toolbarInfo.StrokeThickness,
            Left = _startPoint.X,
            Top = _startPoint.Y,
            Center = new Point2D(_startPoint.X, _startPoint.Y),
            Radius = 0
        };
        _isDrawing = true;

        _whiteboardClient.Canvas.AddItem(_circle);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _circle is null)
        {
            return;
        }

        Point currentPoint = e.GetCurrentPoint(_canvas).Position;
        double side = Math.Min(
            Math.Abs(currentPoint.X - _startPoint.X),
            Math.Abs(currentPoint.Y - _startPoint.Y));

        double left = currentPoint.X >= _startPoint.X
            ? _startPoint.X
            : _startPoint.X - side;
        double top = currentPoint.Y >= _startPoint.Y
            ? _startPoint.Y
            : _startPoint.Y - side;

        _circle.Left = left;
        _circle.Top = top;
        _circle.Center = new Point2D(
            left + (side / 2),
            top + (side / 2));
        _circle.Radius = side / 2;

        _whiteboardClient.Canvas.EditItem(_circle);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _circle is null)
        {
            return;
        }

        _isDrawing = false;
        _canvas.ReleasePointerCapture(e.Pointer);
        _whiteboardClient.AddItem(_circle);
        _circle = null;
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (_circle is not null)
        {
            _whiteboardClient.Canvas.DeleteItem(_circle.Id);
            _circle = null;
        }
    }

    private static Color GetColor(Microsoft.UI.Xaml.Media.Brush brush)
    {
        return brush is Microsoft.UI.Xaml.Media.SolidColorBrush solidBrush
            ? solidBrush.Color
            : Color.FromArgb(255, 0, 0, 0);
    }
}
