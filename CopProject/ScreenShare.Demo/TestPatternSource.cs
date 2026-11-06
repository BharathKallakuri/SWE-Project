using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScreenShare.Demo;

/// <summary>
/// a 720p test picture: fixed colour bars with a moving ball and a running clock.
/// only a small part changes per frame, so it exercises deltas, and two demo windows on
/// one machine do not end up filming each other.
/// </summary>
internal sealed class TestPatternSource : IImageSource, IDisposable
{
    private const int Width = 1280;
    private const int Height = 720;
    private const int BallSize = 120;

    private readonly string _label;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly Bitmap _background;
    private readonly Font _font = new("Segoe UI", 28, FontStyle.Bold);

    public TestPatternSource(string label)
    {
        _label = label;
        _background = DrawBackground();
    }

    public Bitmap Capture()
    {
        var frame = new Bitmap(_background);
        double seconds = _clock.Elapsed.TotalSeconds;

        int x = (int)((Math.Sin(seconds) + 1) / 2 * (Width - BallSize));
        int y = (int)((Math.Cos(seconds * 0.7) + 1) / 2 * (Height - BallSize));

        using Graphics g = Graphics.FromImage(frame);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillEllipse(Brushes.OrangeRed, x, y, BallSize, BallSize);
        g.FillRectangle(Brushes.Black, 20, 20, 560, 60);
        g.DrawString($"{_label}   {DateTime.Now:HH:mm:ss.f}", _font, Brushes.White, 30, 25);

        return frame;
    }

    public void Dispose()
    {
        _background.Dispose();
        _font.Dispose();
    }

    private static Bitmap DrawBackground()
    {
        var bitmap = new Bitmap(Width, Height);
        Color[] bars =
        {
            Color.White, Color.Yellow, Color.Cyan, Color.Lime,
            Color.Magenta, Color.Red, Color.Blue, Color.Black,
        };

        using Graphics g = Graphics.FromImage(bitmap);
        int barWidth = Width / bars.Length;
        for (int i = 0; i < bars.Length; i++)
        {
            using var brush = new SolidBrush(bars[i]);
            g.FillRectangle(brush, i * barWidth, 0, barWidth, Height);
        }

        // a 64 px grid, the ScreenShare tile size, to make tile edges visible
        using var pen = new Pen(Color.FromArgb(80, Color.Gray));
        for (int x = 0; x < Width; x += 64)
        {
            g.DrawLine(pen, x, 0, x, Height);
        }
        for (int y = 0; y < Height; y += 64)
        {
            g.DrawLine(pen, 0, y, Width, y);
        }

        return bitmap;
    }
}
