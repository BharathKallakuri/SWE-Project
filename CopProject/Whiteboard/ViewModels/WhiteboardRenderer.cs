// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using XamlRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;

namespace Whiteboard.ViewModels;

internal sealed class WhiteboardRenderer : IDisposable
{
    private readonly IWhiteboard _board;
    private readonly Canvas _canvas;
    private readonly Dictionary<Guid, Microsoft.UI.Xaml.UIElement> _visuals = [];

    public WhiteboardRenderer(IWhiteboard board, Canvas canvas)
    {
        _board = board;
        _canvas = canvas;
        _board.ItemAdded += RenderAdded;
        _board.ItemUpdated += RenderUpdated;
        _board.ItemDeleted += RemoveDeleted;

        foreach (IItem item in _board.Items.Values)
        {
            RenderAdded(item);
        }
    }

    public void Dispose()
    {
        _board.ItemAdded -= RenderAdded;
        _board.ItemUpdated -= RenderUpdated;
        _board.ItemDeleted -= RemoveDeleted;
    }

    private void RenderAdded(IItem item)
    {
        RemoveDeleted(item.Id);
        Microsoft.UI.Xaml.UIElement visual = CreateVisual(item);
        _visuals[item.Id] = visual;
        _canvas.Children.Add(visual);
    }

    private void RenderUpdated(IItem item)
    {
        RenderAdded(item);
    }

    private void RemoveDeleted(Guid id)
    {
        if (_visuals.Remove(id, out Microsoft.UI.Xaml.UIElement? visual))
        {
            _canvas.Children.Remove(visual);
        }
    }

    private static Microsoft.UI.Xaml.UIElement CreateVisual(IItem item)
    {
        return item switch {
            Whiteboard.Items.Shapes.Square square => CreateSquare(square),
            _ => new Border()
        };
    }

    private static XamlRectangle CreateSquare(Whiteboard.Items.Shapes.Square item)
    {
        XamlRectangle visual = new() {
            Width = item.SideLegth,
            Height = item.SideLegth,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Left);
        Canvas.SetTop(visual, item.Top);
        return visual;
    }

    private static Windows.UI.Color ToColor(Windows.UI.Color color)
    {
        return color;
    }
}

