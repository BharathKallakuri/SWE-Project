using System.Reflection.Metadata.Ecma335;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Whiteboard.Interfaces;
using Windows.UI;

namespace Whiteboard.Items;

/// <inheritdoc/>
public class Textbox : Item, ITextbox
{
    public Point2D Center
    {
        get => field = new(Left + (Width / 2), Top - (Height / 2));
        init => field = new(0, 0);
    }

    // Model size for selection/hit-testing (not UI control size)
    public double Width { get; set; } = 150;
    public double Height { get; set; } = 40;

    public override Rect2D BoundingBox
    {
        get => new(
            Left,
            Top,
            Width,
            Height
        );

        set {
            Left = value.Left;
            Top = value.Top;
            Width = value.Width;
            Height = value.Height;
        }
    }

    public override bool HitTest(Point2D point)
    {
        Rect2D box = BoundingBox;

        bool withinX = point.X >= box.Left && point.X <= box.Left + box.Width;
        bool withinY = point.Y >= box.Top && point.Y <= box.Top + box.Height;

        return withinX && withinY;
    }

    public string Content { get; set; } = string.Empty;
    public FontFamily FontFamily { get; set; } = new FontFamily("Segoe UI");
    public int FontSize { get; set; } = 15;
    public Color FontColor { get; set; } = Color.FromArgb(255, 0, 0, 0);    // Black

}
