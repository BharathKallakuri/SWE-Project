// Author = Sumit
// Roll Number = 112301032

using System;
using Xunit;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareOptionsTests
{
    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new ScreenShareOptions();

        Assert.Equal(30, options.FramesPerSecond);
        Assert.Equal(480, options.MaxHeight);
        Assert.Equal(8, options.MaxFramesInFlight);
        Assert.Equal(TimeSpan.FromSeconds(2), options.AckTimeout);
        Assert.Equal(5, options.MaxViewerLag);
        Assert.Equal(TimeSpan.FromSeconds(10), options.ViewerTimeout);
        Assert.Equal(TimeSpan.FromSeconds(1), options.MaintenanceInterval);

        // performance: grid stream 180p at 5 FPS in 16-pixel tiles, full stream in 64-pixel
        // tiles, full frames as the smaller of JPEG 75 and PNG, data over TCP
        Assert.Equal(180, options.ThumbnailHeight);
        Assert.Equal(5, options.ThumbnailFramesPerSecond);
        Assert.Equal(16, options.ThumbnailTileSize);
        Assert.Equal(64, options.TileSize);
        Assert.Equal(75, options.FullFrameJpegQuality);
        Assert.False(options.SendDataLossy);
    }

    [Theory]
    [InlineData(nameof(ScreenShareOptions.ThumbnailHeight), 0)]
    [InlineData(nameof(ScreenShareOptions.ThumbnailFramesPerSecond), 0)]
    [InlineData(nameof(ScreenShareOptions.ThumbnailTileSize), 0)]
    [InlineData(nameof(ScreenShareOptions.TileSize), 0)]
    [InlineData(nameof(ScreenShareOptions.FullFrameJpegQuality), -1)]
    [InlineData(nameof(ScreenShareOptions.FullFrameJpegQuality), 101)]
    public void Validate_ThrowsError_WhenPerformanceOptionIsInvalid(string option, int value)
    {
        var options = option switch
        {
            nameof(ScreenShareOptions.ThumbnailHeight) => new ScreenShareOptions { ThumbnailHeight = value },
            nameof(ScreenShareOptions.ThumbnailFramesPerSecond) => new ScreenShareOptions { ThumbnailFramesPerSecond = value },
            nameof(ScreenShareOptions.ThumbnailTileSize) => new ScreenShareOptions { ThumbnailTileSize = value },
            nameof(ScreenShareOptions.TileSize) => new ScreenShareOptions { TileSize = value },
            _ => new ScreenShareOptions { FullFrameJpegQuality = value },
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Fact]
    public void Validate_WithDefaultOptions_DoesNotThrowAnError()
    {
        var options = new ScreenShareOptions();

        // Record.Exception captures any errors thrown by the Validate method
        Exception exception = Record.Exception(options.Validate);

        // If the exception is null, it means no errors were thrown (which is what we want!)
        Assert.Null(exception);
    }


    // ---------------------------------------------------------

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenFramesPerSecondIsInvalid(int invalidValue)
    {
        //  Create default options, but change just this one property
        ScreenShareOptions options = new ScreenShareOptions() with { FramesPerSecond = invalidValue };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenMaxHeightIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxHeight = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenMaxFramesInFlightIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxFramesInFlight = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenMaxViewerLagIsInvalid(int invalidValue)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaxViewerLag = invalidValue };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }



    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenAckTimeoutIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { AckTimeout = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenViewerTimeoutIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { ViewerTimeout = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    public void Validate_ThrowsError_WhenMaintenanceIntervalIsInvalid(int seconds)
    {
        ScreenShareOptions options = new ScreenShareOptions() with { MaintenanceInterval = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }
}
