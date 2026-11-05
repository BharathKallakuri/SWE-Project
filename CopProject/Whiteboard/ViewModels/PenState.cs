// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Whiteboard.Client;
using Windows.Foundation;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Pen tool is selected
/// </summary>
/// <inheritdoc/>
[ExcludeFromCodeCoverage]
// Temporary exclusion from code coverage
public class PenState : IViewModelState
{
    /// <summary>
    /// The whiteboard client
    /// </summary>
    private readonly IWhiteboardClient _whiteboardClient;

    /// <summary>
    /// The canvas UI
    /// </summary>
    private readonly Canvas _canvas;

    /// <summary>
    /// Current toolbar information
    /// </summary>
    private readonly ToolbarInfo _toolbarInfo;

    /// <summary>
    /// Flag to check whether the user is currently drawing on screen
    /// </summary>
    private bool _isDrawing = false;

    /// <summary>
    /// Start point of the polyline
    /// </summary>
    private Point _startPoint;

    /// <summary>
    /// Active polyline
    /// </summary>
    private Polyline _polyline;

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
        // Try capturing the pointer
        if (!_canvas.CapturePointer(e.Pointer))
        {
            return;
        }

        // Set flag to begin drawing
        _isDrawing = true;

        // Record the location where the pointer was pressed
        _startPoint = e.GetCurrentPoint(_canvas).Position;

        // Create a new polyline with brush and fill properties taken from the toolbar
        _polyline = new Polyline {
            Stroke = _toolbarInfo.BorderBrush,
            StrokeThickness = _toolbarInfo.StrokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false // disable hit testing as long as the drawing is in progress
        };

        // Begin the polyline with the first point
        _polyline.Points.Add(_startPoint);

        // Add the polyline to the canvas for rendering
        _canvas.Children.Add(_polyline);

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        if (_polyline is not null)
        {
            // Get the current pointer position
            Point currentPoint = e.GetCurrentPoint(_canvas).Position;

            // Add the current cursor point to the polyline
            _polyline.Points.Add(currentPoint);
        }

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        // Release the captured pointer
        _canvas.ReleasePointerCapture(e.Pointer);

        if (_polyline is not null)
        {
            // Add tooltip
            ToolTip toolTip = new(_whiteboardClient.UserId, DateTime.Now);
            ToolTipService.SetToolTip(_polyline, toolTip);

            // Enable hit testing on the polyline
            _polyline.IsHitTestVisible = true;
        }

        // Reset polyline state back to null
        _polyline = null;

        // Mark the event as handled
        e.Handled = true;
    }

    public void PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        // Return early if the user is not drawing
        if (!_isDrawing)
        {
            return;
        }

        // Unset flag to stop drawing
        _isDrawing = false;

        if (_polyline is not null)
        {
            // Remove partially formed polyline from the canvas
            _canvas.Children.Remove(_polyline);
        }

        // Reset polyline state back to null
        _polyline = null;
    }
}
