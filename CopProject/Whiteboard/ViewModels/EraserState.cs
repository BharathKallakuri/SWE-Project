using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;

namespace Whiteboard.ViewModels;

public class EraserState : IViewModelState
{
    private readonly IWhiteboardClient _whiteboardClient;
    private readonly Canvas _canvas;

    public EraserState(IWhiteboardClient whiteboardClient, Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);
        ArgumentNullException.ThrowIfNull(canvas);

        _whiteboardClient = whiteboardClient;
        _canvas = canvas;
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Windows.Foundation.Point point = e.GetCurrentPoint(_canvas).Position;
        IItem? item = _whiteboardClient.Canvas.HitTestTopmost(new Point2D(point.X, point.Y));
        if (item is null)
        {
            return;
        }

        _whiteboardClient.DeleteItem(item.Id);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
    }
}