using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Whiteboard.Client;
using Whiteboard.Interfaces;
using Whiteboard.Items.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Shape Square tool is selected
/// </summary>
public class ShapeSquareState : IViewModelState
{
    /// <summary>
    /// The whiteboard client
    /// </summary>
    private readonly IWhiteboardClient _whiteboardClient;

    /// <summary>
    /// The whiteboard canvas UI
    /// </summary>
    private readonly Canvas _canvas;

    /// <summary>
    /// Current toolbar information
    /// </summary>
    private readonly ToolbarInfo _toolbarInfo;

    /// <summary>
    /// Active square model
    /// </summary>
    private Square? _square;

    /// <summary>
    /// Start point of the square
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// Flag to check whether the user is currently drawing on screen
    /// </summary>
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
        // Try capturing the pointer.
        if (!_canvas.CapturePointer(e.Pointer))
        {
            return;
        }

        // Record the location where the pointer was pressed.
        _startPoint = e.GetCurrentPoint(_canvas).Position;

        // Create the domain model using the selected toolbar properties.
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

        // Add the model to the board so the renderer can display it.
        _whiteboardClient.Canvas.AddItem(_square);
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _square is null)
        {
            return;
        }

        // Get the current pointer position.
        Point currentPoint = e.GetCurrentPoint(_canvas).Position;

        // Compute side length as the minimum of the X and Y distances.
        double side = Math.Min(
            Math.Abs(currentPoint.X - _startPoint.X),
            Math.Abs(currentPoint.Y - _startPoint.Y));

        // Adjust X and Y based on drag direction and preserve equal sides.
        _square.Left = currentPoint.X >= _startPoint.X ? _startPoint.X : _startPoint.X - side;
        _square.Top = currentPoint.Y >= _startPoint.Y ? _startPoint.Y : _startPoint.Y - side;
        _square.SideLegth = side;
        // Notify the board so the renderer refreshes the preview.
        _whiteboardClient.Canvas.EditItem(_square);
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDrawing || _square is null)
        {
            return;
        }

        // Unset flag to stop drawing.
        _isDrawing = false;
        // Release the captured pointer.
        _canvas.ReleasePointerCapture(e.Pointer);
        // Add the completed model through the client for synchronization.
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

        // Unset flag to stop drawing.
        _isDrawing = false;
        if (_square is not null)
        {
            // Remove the incomplete model from the board.
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
