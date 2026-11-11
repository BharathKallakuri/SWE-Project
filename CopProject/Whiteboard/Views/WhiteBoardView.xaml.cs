// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Whiteboard.Interfaces;
using Whiteboard.ViewModels;
using Windows.Foundation;

namespace Whiteboard.Views;

[ExcludeFromCodeCoverage]
public sealed partial class WhiteBoardView : UserControl
{
    private readonly WhiteBoardViewModel _viewModel;

    public WhiteBoardView(WhiteBoardViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();

        _viewModel = viewModel;

        ToolbarInfo toolbarInfo = new(Colors.Black, 3, Colors.Red);
        _viewModel.InitializeViewModel(WhiteboardCanvas, toolbarInfo);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Subscribes to history changes and displays the latest button availability.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.HistoryChanged += UpdateHistoryButtons;
        UpdateHistoryButtons();
    }

    /// <summary>
    /// Detaches the history listener when the view leaves the visual tree.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.HistoryChanged -= UpdateHistoryButtons;
    }

    /// <summary>
    /// Updates undo and redo availability on the UI thread.
    /// </summary>
    private void UpdateHistoryButtons()
    {
        // Network notifications arrive outside the UI thread.
        DispatcherQueue.TryEnqueue(() => {
            ButtonUndo.IsEnabled = _viewModel.CanUndo;
            ButtonRedo.IsEnabled = _viewModel.CanRedo;
        });
    }


    /// <summary>
    /// Requests undo of the current user's latest available action.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void ButtonUndo_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Undo();
    }

    /// <summary>
    /// Requests redo of the current user's latest available undone action.
    /// </summary>
    /// <param name="sender">Source of the event.</param>
    /// <param name="e">Event details.</param>
    private void ButtonRedo_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Redo();
    }


    private void ButtonPen_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Pen);
    }

    private void ButtonSquare_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Square);

    }

    private void ButtonCircle_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Circle);
    }

    private void ButtonEraseItems_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.SelectTool(WhiteboardTool.Eraser);
    }

    private void CanvasContainer_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        WhiteboardCanvas.Width = e.NewSize.Width;
        WhiteboardCanvas.Height = e.NewSize.Height;
    }

    private void WhiteboardCanvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Attempt to capture the pointer
        if (WhiteboardCanvas.CapturePointer(e.Pointer))
        {
            // If successful, get the current pointer position
            Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

            // Pass the pointer position to the view model for computation
            _viewModel.PointerPressed(new Point2D(p.X, p.Y));

            // Mark the event as handled
            e.Handled = true;
        }
    }

    private void WhiteboardCanvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerMoved(new Point2D(p.X, p.Y));

        // Mark the event as handled
        e.Handled = true;
    }

    private void WhiteboardCanvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerReleased(new Point2D(p.X, p.Y));

        // Release the pointer, if captured
        WhiteboardCanvas.ReleasePointerCapture(e.Pointer);

        // Mark the event as handled
        e.Handled = true;
    }

    private void WhiteboardCanvas_PointerCaptureLost(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Get the current pointer position
        Point p = e.GetCurrentPoint(WhiteboardCanvas).Position;

        // Pass the pointer position to the view model for computation
        _viewModel.PointerCaptureLost(new Point2D(p.X, p.Y));
    }
}
