// Owner   : Sasi Kiran
// LICENSE : MIT

using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Windows.Foundation;
using Windows.UI;
using XamlEllipse = Microsoft.UI.Xaml.Shapes.Ellipse;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Circle tool is selected.
/// </summary>
public class ShapeCircleState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private bool _isDrawing;
    private Point _startPoint;
    private XamlEllipse? _circle;

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

        _isDrawing = true;
        _startPoint = e.GetCurrentPoint(_canvas).Position;
        _circle = new XamlEllipse {
            Stroke = _toolbarInfo.BorderBrush,
            StrokeThickness = _toolbarInfo.StrokeThickness,
            Fill = _toolbarInfo.FillBrush,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(_circle, _startPoint.X);
        Canvas.SetTop(_circle, _startPoint.Y);
        _circle.Width = 0;
        _circle.Height = 0;
        _canvas.Children.Add(_circle);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        if (_circle is not null)
        {
            Point currentPoint = e.GetCurrentPoint(_canvas).Position;
            double diameter = Math.Min(
                Math.Abs(currentPoint.X - _startPoint.X),
                Math.Abs(currentPoint.Y - _startPoint.Y));
            double left = currentPoint.X >= _startPoint.X
                ? _startPoint.X
                : _startPoint.X - diameter;
            double top = currentPoint.Y >= _startPoint.Y
                ? _startPoint.Y
                : _startPoint.Y - diameter;

            Canvas.SetLeft(_circle, left);
            Canvas.SetTop(_circle, top);
            _circle.Width = diameter;
            _circle.Height = diameter;
        }

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
            _canvas.Children.Remove(_circle);
        }

        _circle = null;
    }
}
