// Author = Sumit
// Roll Number = 112301032

using Microsoft.UI.Xaml.Media;
using ScreenShare.Models;

namespace Screenshare.Tests;

// skipped: BitmapTileSurface wraps WinUI's WriteableBitmap, which needs the Windows App SDK
// runtime that the test process can't load ("Class not registered"). they run in a WinUI
// test host; the tile logic around the surface is tested with FakeTileSurface in ParticipantTileTest
public class BitMapTileSurfaceTests
{
    private const string NeedsWinUi = "needs the WinUI runtime (WriteableBitmap)";

    [Fact(Skip = NeedsWinUi)]
    public void PixelWidth_ReturnsPixelWidth()
    {
        BitmapTileSurface surface = new(10, 30);

        Assert.Equal(10, surface.PixelWidth);
    }

    [Fact(Skip = NeedsWinUi)]
    public void PixelHeight_ReturnsPixelHeight()
    {
        BitmapTileSurface surface = new(10, 30);

        Assert.Equal(30, surface.PixelHeight);
    }

    [Fact(Skip = NeedsWinUi)]
    public void Source_IsTheBitmap()
    {
        BitmapTileSurface surface = new(10, 30);

        Assert.NotNull(surface.Source);
    }

    [Fact(Skip = NeedsWinUi)]
    public void WritePixels_FullFrame_KeepsSizeAndSource()
    {
        BitmapTileSurface surface = new(10, 30);
        ImageSource? sourceBefore = surface.Source;

        surface.WritePixels(new byte[10 * 30 * 4]);

        Assert.Equal((10, 30), (surface.PixelWidth, surface.PixelHeight));
        Assert.Same(sourceBefore, surface.Source);
    }
}
