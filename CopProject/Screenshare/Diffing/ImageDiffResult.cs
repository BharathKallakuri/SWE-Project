// Author = Aryan
// Roll Number = 112301041

// to capture whether we need to transfer full frame or partial or nothing

using System.Drawing;
using System.Diagnostics;

namespace ScreenShare;

/// <summary>
/// the outcome of comparing two frames. owns FullFrame, so dispose the result when done with it
/// </summary>
public sealed class ImageDiffResult : IDisposable
{
    public bool IsUnchanged { get; }

    public bool IsFullFrame { get; }

    public List<Tile> ChangedTiles { get; }

    public Bitmap? FullFrame { get; }

    /// <summary>
    /// for a delta with many changed tiles, the whole new picture too, so the sender can send
    /// it as one image instead when that is smaller (e.g. as JPEG); null otherwise
    /// </summary>
    public Bitmap? WholeFrame { get; }


    private ImageDiffResult(
        bool isUnchanged,
        bool isFullFrame,
        List<Tile> changedTiles,
        Bitmap? fullFrame,
        Bitmap? wholeFrame = null)
    {
        IsUnchanged = isUnchanged;
        IsFullFrame = isFullFrame;
        ChangedTiles = changedTiles;
        FullFrame = fullFrame;
        WholeFrame = wholeFrame;

    }

    public static ImageDiffResult Unchanged()
    {
        return new ImageDiffResult(
            isUnchanged: true,
            isFullFrame: false,
            changedTiles: new List<Tile>(),
            fullFrame: null);
    }

    /// <param name="wholeFrame">the whole new picture, for a big delta; owned by the result</param>
    public static ImageDiffResult Delta(List<Tile> changedTiles, Bitmap? wholeFrame = null)
    {
        Trace.TraceInformation("[Screenshare] Delta result created. Changed tiles: {0}.", changedTiles.Count);
        return new ImageDiffResult(
            isUnchanged: false,
            isFullFrame: false,
            changedTiles: changedTiles,
            fullFrame: null,
            wholeFrame: wholeFrame);
    }

    public static ImageDiffResult CreateFullFrame(Bitmap frame)
    {
        Trace.TraceInformation("[Screenshare] Full-frame result created. Dimensions: {0}x{1}.", frame.Width, frame.Height);
        return new ImageDiffResult(
            isUnchanged: false,
            isFullFrame: true,
            changedTiles: new List<Tile>(),
            fullFrame: frame);
    }

    public void Dispose()
    {
        FullFrame?.Dispose();
        WholeFrame?.Dispose();
    }
}
