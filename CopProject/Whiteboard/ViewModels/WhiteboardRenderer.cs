using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Whiteboard.Interfaces.Shapes;
using Whiteboard.Items;
using Windows.Foundation;
using XamlEllipse = Microsoft.UI.Xaml.Shapes.Ellipse;
using XamlPolyline = Microsoft.UI.Xaml.Shapes.Polyline;
using XamlRectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using XamlPolygon = Microsoft.UI.Xaml.Shapes.Polygon;
using XamlLine = Microsoft.UI.Xaml.Shapes.Line;

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
            FreehandItem freehand => CreateFreehand(freehand),
            Whiteboard.Items.Shapes.Square square => CreateSquare(square),
            Whiteboard.Items.Shapes.Rectangle rectangle => CreateRectangle(rectangle),
            Whiteboard.Items.Shapes.Circle circle => CreateCircle(circle),
            Whiteboard.Items.Shapes.Ellipse ellipse => CreateEllipse(ellipse),
            Whiteboard.Items.Shapes.EqTriangle triangle => CreateTriangle(triangle),
            Whiteboard.Items.Shapes.Polygon polygon => CreatePolygon(polygon),
            Line line => CreateLine(line),
            Textbox textbox => CreateTextbox(textbox),
            _ => new Border()
        };
    }

    private static XamlPolyline CreateFreehand(FreehandItem item)
    {
        XamlPolyline visual = new() {
            Stroke = new SolidColorBrush(ToColor(item.StrokeColor)),
            StrokeThickness = item.StrokeWidth,
            StrokeLineJoin = Microsoft.UI.Xaml.Media.PenLineJoin.Round,
            StrokeStartLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            StrokeEndLineCap = Microsoft.UI.Xaml.Media.PenLineCap.Round,
            IsHitTestVisible = false
        };
        foreach (Point2D point in item.Points)
        {
            visual.Points.Add(new Point(point.X, point.Y));
        }
        return visual;
    }

    private static XamlPolygon CreateTriangle(Whiteboard.Items.Shapes.EqTriangle item)
    {
        double height = Math.Sqrt(3) / 2 * item.SideLength;
        XamlPolygon visual = new() {
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false,
            Points = {
                new Point(item.Left + item.SideLength / 2, item.Top),
                new Point(item.Left, item.Top + height),
                new Point(item.Left + item.SideLength, item.Top + height)
            }
        };
        return visual;
    }

    private static XamlPolygon CreatePolygon(Whiteboard.Items.Shapes.Polygon item)
    {
        XamlPolygon visual = new() {
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false
        };
        foreach (Point2D point in item.Vertices)
        {
            visual.Points.Add(new Point(point.X, point.Y));
        }
        return visual;
    }

    private static XamlLine CreateLine(Line item)
    {
        return new XamlLine {
            X1 = item.Begin.X,
            Y1 = item.Begin.Y,
            X2 = item.End.X,
            Y2 = item.End.Y,
            Stroke = new SolidColorBrush(Colors.Black),
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
    }

    private static TextBlock CreateTextbox(Textbox item)
    {
        TextBlock visual = new() {
            Text = item.Content,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(item.FontFamily),
            FontSize = item.FontSize,
            Foreground = new SolidColorBrush(ToColor(item.FontColor)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Left);
        Canvas.SetTop(visual, item.Top);
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
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Left);
        Canvas.SetTop(visual, item.Top);
        return visual;
    }

    private static XamlRectangle CreateRectangle(IRectangle item)
    {
        XamlRectangle visual = new() {
            Width = item.Width,
            Height = item.Height,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Left);
        Canvas.SetTop(visual, item.Top);
        return visual;
    }

    private static XamlEllipse CreateCircle(Whiteboard.Items.Shapes.Circle item)
    {
        XamlEllipse visual = new() {
            Width = item.Radius * 2,
            Height = item.Radius * 2,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Center.X - item.Radius);
        Canvas.SetTop(visual, item.Center.Y - item.Radius);
        return visual;
    }

    private static XamlEllipse CreateEllipse(Whiteboard.Items.Shapes.Ellipse item)
    {
        XamlEllipse visual = new() {
            Width = item.Width,
            Height = item.Height,
            Fill = new SolidColorBrush(ToColor(item.FillColor)),
            Stroke = new SolidColorBrush(ToColor(item.BorderColor)),
            StrokeThickness = item.BorderWidth,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(visual, item.Center.X - item.Width / 2);
        Canvas.SetTop(visual, item.Center.Y - item.Height / 2);
        return visual;
    }

    private static Windows.UI.Color ToColor(Windows.UI.Color color)
    {
        return color;
    }
}
