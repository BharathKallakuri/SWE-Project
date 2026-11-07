// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using XamlPolyline = Microsoft.UI.Xaml.Shapes.Polyline;
using XamlRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using Windows.Foundation;

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
        RunOnUIThread(() => {
            RemoveDeletedInternal(item.Id);
            Microsoft.UI.Xaml.UIElement visual = CreateVisual(item);
            _visuals[item.Id] = visual;
            _canvas.Children.Add(visual);
        });
    }

    private void RenderUpdated(IItem item)
    {
        RenderAdded(item);
    }

    private void RemoveDeleted(Guid id)
    {
        RunOnUIThread(() => RemoveDeletedInternal(id));
    }

    private void RemoveDeletedInternal(Guid id)
    {
        if (_visuals.Remove(id, out Microsoft.UI.Xaml.UIElement? visual))
        {
            _canvas.Children.Remove(visual);
        }

        for (int index = _canvas.Children.Count - 1; index >= 0; index--)
        {
            if (_canvas.Children[index] is FrameworkElement element && element.Tag is Guid itemId && itemId == id)
            {
                _canvas.Children.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Dispatch the given action on the UI thread
    /// </summary>
    /// <param name="action">Action to dispatch</param>
    private void RunOnUIThread(Action action)
    {
        _canvas.DispatcherQueue.TryEnqueue(() => action());
    }

    private static Microsoft.UI.Xaml.UIElement CreateVisual(IItem item)
    {
        return item switch {
            IFreehand freehand => CreateFreehand(freehand),
            Whiteboard.Items.Shapes.Square square => CreateSquare(square),
            _ => new Border()
        };
    }

    private static XamlPolyline CreateFreehand(IFreehand item)
    {
        XamlPolyline visual = new() {
            Stroke = new SolidColorBrush(item.StrokeColor),
            StrokeThickness = item.StrokeWidth,
            StrokeLineJoin = Microsoft.UI.Xaml.Media.PenLineJoin.Round,
            StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            IsHitTestVisible = false,
            Tag = item.Id
        };

        foreach (Point2D point in item.Points)
        {
            visual.Points.Add(new Point(point.X, point.Y));
        }

        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static XamlRectangle CreateSquare(Whiteboard.Items.Shapes.Square item)
    {
        XamlRectangle visual = new() {
            Width = item.SideLegth,
            Height = item.SideLegth,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false,
            Tag = item.Id
        };

        double left = item.Left;
        double top = item.Top;
        if (left == 0 && top == 0 && (item.Center.X != 0 || item.Center.Y != 0))
        {
            left = item.Center.X - (item.SideLegth / 2);
            top = item.Center.Y - (item.SideLegth / 2);
        }

        Canvas.SetLeft(visual, left);
        Canvas.SetTop(visual, top);
        ToolTipService.SetToolTip(visual, new ToolTip(item.CreatedBy, item.CreatedDateTime));
        return visual;
    }

    private static Windows.UI.Color ToColor(Windows.UI.Color color)
    {
        return color;
    }
}

