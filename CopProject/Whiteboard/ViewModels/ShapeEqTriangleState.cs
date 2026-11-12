using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Windows.Foundation;
using Windows.UI;
using XamlPolygon = Microsoft.UI.Xaml.Shapes.Polygon;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the equilateral triangle tool is selected.
/// </summary>
public class ShapeEqTriangleState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private bool _isDrawing;
    private Point _startPoint;
    private XamlPolygon? _triangle;

    public ShapeEqTriangleState(
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
        _triangle = new XamlPolygon {
            Stroke = _toolbarInfo.BorderBrush,
            StrokeThickness = _toolbarInfo.StrokeThickness,
            Fill = _toolbarInfo.FillBrush,
            IsHitTestVisible = false
        };
        _canvas.Children.Add(_triangle);
        UpdateTriangle(_startPoint);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _triangle is null)
        {
            return;
        }

        UpdateTriangle(e.GetCurrentPoint(_canvas).Position);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        _canvas.ReleasePointerCapture(e.Pointer);

        if (_triangle is not null)
        {
            double left = Canvas.GetLeft(_triangle);
            double top = Canvas.GetTop(_triangle);
            double side = _triangle.Width;
            double height = side * Math.Sqrt(3) / 2;
            Color borderColor = _toolbarInfo.BorderBrush is SolidColorBrush borderBrush
                ? borderBrush.Color
                : Color.FromArgb(255, 0, 0, 0);
            Color fillColor = _toolbarInfo.FillBrush is SolidColorBrush fillBrush
                ? fillBrush.Color
                : Color.FromArgb(0, 0, 0, 0);
            Whiteboard.Items.Shapes.EqTriangle triangle = new() {
                Left = left,
                Top = top,
                Center = new Point2D(left + side / 2, top + height / 2),
                SideLength = side,
                BorderColor = borderColor,
                BorderWidth = _toolbarInfo.StrokeThickness,
                FillColor = fillColor,
                CreatedBy = _whiteboardClient.UserId
            };

            _whiteboardClient.AddItem(triangle);
            _canvas.Children.Remove(_triangle);
        }

        _triangle = null;
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (_triangle is not null)
        {
            _canvas.Children.Remove(_triangle);
        }

        _triangle = null;
    }

    private void UpdateTriangle(Point currentPoint)
    {
        if (_triangle is null)
        {
            return;
        }

        double horizontal = Math.Abs(currentPoint.X - _startPoint.X);
        double vertical = Math.Abs(currentPoint.Y - _startPoint.Y);
        double side = Math.Min(horizontal, vertical * 2 / Math.Sqrt(3));
        double left = currentPoint.X >= _startPoint.X ? _startPoint.X : _startPoint.X - side;
        double top = currentPoint.Y >= _startPoint.Y ? _startPoint.Y : _startPoint.Y - side * Math.Sqrt(3) / 2;
        double height = side * Math.Sqrt(3) / 2;

        Canvas.SetLeft(_triangle, left);
        Canvas.SetTop(_triangle, top);
        _triangle.Width = side;
        _triangle.Height = height;
        _triangle.Points.Clear();
        _triangle.Points.Add(new Point(side / 2, 0));
        _triangle.Points.Add(new Point(0, height));
        _triangle.Points.Add(new Point(side, height));
    }
}
