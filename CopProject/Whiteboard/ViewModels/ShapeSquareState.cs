using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.ViewModels;

public class ShapeSquareState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private Square? _square;
    private Point _startPoint;
    private bool _isDrawing;

    public ShapeSquareState(IWhiteboardClient whiteboardClient, Canvas canvas, ToolbarInfo toolbarInfo)
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
        _square = new Square {
            CreatedBy = _whiteboardClient.UserId,
            BorderColor = GetColor(_toolbarInfo.BorderBrush),
            FillColor = GetColor(_toolbarInfo.FillBrush),
            BorderWidth = _toolbarInfo.StrokeThickness,
            Left = _startPoint.X,
            Top = _startPoint.Y,
            SideLegth = 0
        };
        _isDrawing = true;
        _whiteboardClient.Canvas.AddItem(_square);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _square is null)
        {
            return;
        }

        Point currentPoint = e.GetCurrentPoint(_canvas).Position;
        double side = Math.Min(
            Math.Abs(currentPoint.X - _startPoint.X),
            Math.Abs(currentPoint.Y - _startPoint.Y));

        _square.Left = currentPoint.X >= _startPoint.X ? _startPoint.X : _startPoint.X - side;
        _square.Top = currentPoint.Y >= _startPoint.Y ? _startPoint.Y : _startPoint.Y - side;
        _square.SideLegth = side;
        _whiteboardClient.Canvas.EditItem(_square);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _square is null)
        {
            return;
        }

        _isDrawing = false;
        _canvas.ReleasePointerCapture(e.Pointer);
        _whiteboardClient.AddItem(_square);
        _square = null;
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (_square is not null)
        {
            _whiteboardClient.Canvas.DeleteItem(_square.Id);
            _square = null;
        }
    }

    private static Color GetColor(Microsoft.UI.Xaml.Media.Brush brush)
    {
        return brush is Microsoft.UI.Xaml.Media.SolidColorBrush solidBrush
            ? solidBrush.Color
            : Color.FromArgb(255, 0, 0, 0);
    }
}
