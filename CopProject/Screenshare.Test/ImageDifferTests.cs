using System.Drawing;
using Xunit;

namespace Screenshare.Tests;

using ScreenShare;

public class ImageDifferTests
{
    [Fact]
    public void Compare_ReturnsUnchanged_WhenFramesAreIdentical()
    {
        IImageDiffer differ = new ImageDiffer();
        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.True(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.Empty(result.ChangedTiles);

    }

    [Fact]
    public void Compare_ReturnsDelta_WhenSmallAreaChanges()
    {
        IImageDiffer differ = new ImageDiffer();
        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(128, 128);

        // change one pixel in current frame
        currentFrame.SetPixel(10, 10, Color.Red);

        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        // the changed pixel belongs to the tile at (0,0)
        Assert.Contains(result.ChangedTiles, tile => tile.X == 0 && tile.Y == 0);
    }

    [Fact]
    public void Compare_ReturnsFullFrame_WhenResolutionChanges()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(128, 128);
        using var currentFrame = new Bitmap(256, 128);

        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.True(result.IsFullFrame);
        Assert.NotNull(result.FullFrame);
        Assert.Equal(256, result.FullFrame!.Width);
        Assert.Equal(128, result.FullFrame.Height);

        // imagediffresult does not dispose its full frame automatically
        result.FullFrame.Dispose();
    }

    [Fact]
    public void Compare_HandlesPartialTileAtBottomRightEdge()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(100, 100);
        using var currentFrame = new Bitmap(100, 100);

        // change a pixel in the bottom-right parital tile
        currentFrame.SetPixel(99, 99, Color.Red);
        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        Tile tile = Assert.Single(result.ChangedTiles);

        Assert.Equal(64, tile.X);
        Assert.Equal(64, tile.Y);
        Assert.Equal(36, tile.Width);
        Assert.Equal(36, tile.Height);
    }

    [Fact]
    public void Compare_HandlesPartialTileAtRightEdge()
    {
        IImageDiffer differ = new ImageDiffer();

        using var previousFrame = new Bitmap(100, 128);
        using var currentFrame = new Bitmap(100, 128);

        // change a pizel in the rightmost partial tile
        currentFrame.SetPixel(99, 10, Color.Red);

        ImageDiffResult result = differ.Compare(previousFrame, currentFrame);

        Assert.False(result.IsUnchanged);
        Assert.False(result.IsFullFrame);
        Assert.NotEmpty(result.ChangedTiles);

        Assert.Contains(result.ChangedTiles, tile => tile.X == 64 && tile.Y == 0 && tile.Width == 36 && tile.Height == 64);
    }
}
