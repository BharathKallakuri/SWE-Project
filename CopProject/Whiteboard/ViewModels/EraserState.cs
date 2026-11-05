// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Whiteboard.ViewModels;

/// <summary>
/// Represents the state when the Eraser tool is selected.
/// </summary>
public class EraserState : IViewModelState
{
    private readonly Canvas _canvas;

    public EraserState(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Point point = e.GetCurrentPoint(_canvas).Position;
        IReadOnlyList<UIElement> hitElements = VisualTreeHelper.FindElementsInHostCoordinates(point, _canvas);

        foreach (UIElement element in hitElements)
        {
            UIElement? canvasChild = GetCanvasChild(element);
            if (canvasChild is null)
            {
                continue;
            }

            _canvas.Children.Remove(canvasChild);
            e.Handled = true;
            return;
        }
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

    private UIElement? GetCanvasChild(UIElement element)
    {
        DependencyObject current = element;

        while (VisualTreeHelper.GetParent(current) is DependencyObject parent && parent != _canvas)
        {
            current = parent;
        }

        return VisualTreeHelper.GetParent(current) == _canvas ? current as UIElement : null;
    }
}
