/// Owner   : Raagam Hitesh Parmar
/// LICENSE : MIT

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;

namespace Whiteboard.ViewModels;

public enum WhiteboardTool
{
    Pen,
    Square,
    Eraser
}

public class WhiteBoardViewModel
{
    private IViewModelState _penState;
    private IViewModelState _squareState;
    private IViewModelState _eraserState;
    private readonly IWhiteboardClient _whiteboardClient;

    private IViewModelState _currentState;

    public WhiteBoardViewModel(IWhiteboardClient whiteboardClient)
    {
        ArgumentNullException.ThrowIfNull(whiteboardClient);

        _whiteboardClient = whiteboardClient;
    }

    public void InitializeViewModel(Canvas canvas, ToolbarInfo toolbarInfo)
    {
        _penState = new PenState(_whiteboardClient, canvas, toolbarInfo);
        _squareState = new ShapeSquareState(_whiteboardClient, canvas, toolbarInfo);
        _eraserState = new EraserState(_whiteboardClient, canvas);

        _currentState = _penState;

        // Render any existing items already on the client whiteboard
        foreach (IItem item in _whiteboardClient.Canvas.Items.Values)
        {
            RenderItem(item, canvas);
        }

        // Listen for new items added
        _whiteboardClient.Canvas.ItemAdded += item => {
            canvas.DispatcherQueue?.TryEnqueue(() => {
                RenderItem(item, canvas);
            });
        };
    }

    private static void RenderItem(IItem item, Canvas canvas)
    {
        // Skip if this item was already rendered locally
        foreach (UIElement child in canvas.Children)
        {
            if (child is FrameworkElement fe && fe.Tag is Guid id && id == item.Id)
            {
                return;
            }
        }

        if (item is IFreehand freehand)
        {
            var polyline = new Polyline {
                Stroke = new SolidColorBrush(freehand.StrokeColor),
                StrokeThickness = freehand.StrokeWidth,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = item.Id
            };

            foreach (Point2D pt in freehand.Points)
            {
                polyline.Points.Add(new Windows.Foundation.Point(pt.X, pt.Y));
            }

            ToolTip toolTip = new(item.CreatedBy, item.CreatedDateTime);
            ToolTipService.SetToolTip(polyline, toolTip);

            canvas.Children.Add(polyline);
        }
        else if (item is ISquare square)
        {
            var rect = new Rectangle {
                Width = square.SideLegth,
                Height = square.SideLegth,
                Stroke = new SolidColorBrush(square.BorderColor),
                StrokeThickness = square.BorderWidth,
                Fill = new SolidColorBrush(square.FillColor),
                Tag = item.Id
            };

            double left = square.Left;
            double top = square.Top;
            if (left == 0 && top == 0 && (square.Center.X != 0 || square.Center.Y != 0))
            {
                left = square.Center.X - (square.SideLegth / 2);
                top = square.Center.Y - (square.SideLegth / 2);
            }

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);

            ToolTip toolTip = new(item.CreatedBy, item.CreatedDateTime);
            ToolTipService.SetToolTip(rect, toolTip);

            canvas.Children.Add(rect);
        }
    }

    /// <summary>
    /// Switches the active state
    /// </summary>
    public void SelectTool(WhiteboardTool tool)
    {
        _currentState = tool switch {
            WhiteboardTool.Pen => _penState,
            WhiteboardTool.Square => _squareState,
            WhiteboardTool.Eraser => _eraserState,
            _ => throw new ArgumentOutOfRangeException(nameof(tool))
        };
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerPressed(sender, e);
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerMoved(sender, e);
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerReleased(sender, e);
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _currentState.PointerCaptureLost(sender, e);
    }
}
