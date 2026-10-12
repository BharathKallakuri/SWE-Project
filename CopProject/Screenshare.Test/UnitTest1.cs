using System.Drawing;
using ScreenShare;

namespace Screenshare.Tests;

public class ImageStitcherTests
{
    [Fact]
    public void ApplyFullFrame_StoresCopyOfFrame()
    {
        using var stitcher = new ImageStitcher();
        using var original = new Bitmap(128, 128);

        using (Graphics g = Graphics.FromImage(original))
        {
            g.Clear(Color.Blue);
        }

        stitcher.ApplyFullFrame(original);

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);
        Assert.Equal(128, result.Width);
        Assert.Equal(128, result.Height);
        Assert.Equal(Color.Blue.ToArgb(),
            result.GetPixel(10, 10).ToArgb());
    }

    [Fact]
    public void ApplyChangedTile_UpdatesOnlySpecifiedRegion()
    {
        using var stitcher = new ImageStitcher();
        using var original = new Bitmap(128, 128);

        using (Graphics g = Graphics.FromImage(original))
        {
            g.Clear(Color.Blue);
        }

        stitcher.ApplyFullFrame(original);

        using var tileBitmap = new Bitmap(64, 64);

        using (Graphics g = Graphics.FromImage(tileBitmap))
        {
            g.Clear(Color.Red);
        }

        using var stream = new MemoryStream();
        tileBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

        var tile = new Tile(
            x: 0,
            y: 0,
            width: 64,
            height: 64,
            hash: 0,
            data: stream.ToArray());

        stitcher.ApplyChangedTile(tile);

        using Bitmap? result = stitcher.GetCurrentFrame();

        Assert.NotNull(result);

        // changed region should be red.
        Assert.Equal(Color.Red.ToArgb(),
            result.GetPixel(10, 10).ToArgb());

        // unchanged region should remain blue.
        Assert.Equal(Color.Blue.ToArgb(),
            result.GetPixel(100, 100).ToArgb());
    }
}
