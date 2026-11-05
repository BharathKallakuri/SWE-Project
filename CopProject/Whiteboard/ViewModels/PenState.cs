using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.ViewModels;

public class PenState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;
    private readonly ToolbarInfo _toolbarInfo;
    private FreehandItem? _freehand;
    private bool _isDrawing;

    public PenState(IWhiteboardClient whiteboardClient, Canvas canvas, ToolbarInfo toolbarInfo)
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

        Point point = e.GetCurrentPoint(_canvas).Position;
        _freehand = new FreehandItem {
            CreatedBy = _whiteboardClient.UserId,
            StrokeColor = GetColor(_toolbarInfo.BorderBrush),
            StrokeWidth = _toolbarInfo.StrokeThickness,
            Points = [new Point2D(point.X, point.Y)]
        };
        _isDrawing = true;
        _whiteboardClient.Canvas.AddItem(_freehand);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        Point point = e.GetCurrentPoint(_canvas).Position;
        _freehand.Points.Add(new Point2D(point.X, point.Y));
        UpdateBounds(_freehand);
        _whiteboardClient.Canvas.EditItem(_freehand);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _freehand is null)
        {
            return;
        }

        _isDrawing = false;
        _canvas.ReleasePointerCapture(e.Pointer);
        UpdateBounds(_freehand);
        _whiteboardClient.AddItem(_freehand);
        _freehand = null;
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (_freehand is not null)
        {
            _whiteboardClient.Canvas.DeleteItem(_freehand.Id);
            _freehand = null;
        }
    }

    private static Color GetColor(Microsoft.UI.Xaml.Media.Brush brush)
    {
        return brush is Microsoft.UI.Xaml.Media.SolidColorBrush solidBrush
            ? solidBrush.Color
            : Color.FromArgb(255, 0, 0, 0);
    }

    private static void UpdateBounds(FreehandItem item)
    {
        if (item.Points.Count == 0)
        {
            item.BoundingBox = new Rect2D(0, 0, 0, 0);
            return;
        }

        double left = double.MaxValue;
        double top = double.MaxValue;
        double right = double.MinValue;
        double bottom = double.MinValue;
        foreach (Point2D point in item.Points)
        {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        item.BoundingBox = new Rect2D(left, top, right - left, bottom - top);
    }
}
