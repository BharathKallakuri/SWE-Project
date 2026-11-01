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
}
