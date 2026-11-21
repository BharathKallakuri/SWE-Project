// Author = Sumit
// Roll Number = 112301032

namespace ScreenShare;

/// <summary>
/// tuning for capture, flow control and cleanup
/// </summary>
public sealed record ScreenShareOptions
{
    /// <summary>
    /// gets how many frames per second the sender tries to capture (spec: 24 to 30). a typical
    /// PC reaches about 20 with GDI capture; the cap keeps fast PCs from spending CPU beyond the spec.
    /// </summary>
    public int FramesPerSecond { get; init; } = 30;

    /// <summary>gets the height captures are scaled down to; wider aspect ratios keep their shape</summary>
    public int MaxHeight { get; init; } = 480;

    /// <summary>
    /// gets the height of the small stream sent for grid tiles. at or above MaxHeight, only
    /// the full stream is sent.
    /// </summary>
    public int ThumbnailHeight { get; init; } = 180;

    /// <summary>gets how many frames per second the small grid stream sends</summary>
    public int ThumbnailFramesPerSecond { get; init; } = 5;

    /// <summary>gets the size in pixels of the square tiles changes are sent in, for the full stream</summary>
    public int TileSize { get; init; } = ImageDiffer.TILE_SIZE;

    /// <summary>
    /// gets the tile size for the small grid stream. a 64-pixel tile is a fifteenth of a 180p
    /// picture, so one typed character would resend a big piece of it; smaller tiles keep such
    /// changes small enough to be held back and sent together
    /// </summary>
    public int ThumbnailTileSize { get; init; } = 16;

    /// <summary>
    /// gets the JPEG quality (1 to 100) for full frames, or 0 to send them as PNG. a full frame
    /// as JPEG at 75 is about a quarter of the PNG size; changed tiles always stay PNG.
    /// </summary>
    public int FullFrameJpegQuality { get; init; } = 75;

    /// <summary>gets how many frames the sender may send before the host acknowledges them</summary>
    public int MaxFramesInFlight { get; init; } = 8;

    /// <summary>
    /// gets how long the sender waits for an acknowledgement before assuming the frames
    /// were lost and starting over with a full frame
    /// </summary>
    public TimeSpan AckTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// gets how many unacknowledged frames a viewer may have before the host stops sending it
    /// deltas; once it catches up it gets one full frame instead
    /// </summary>
    public int MaxViewerLag { get; init; } = 5;

    /// <summary>
    /// gets how long a viewer may leave frames unacknowledged before the host drops it,
    /// which catches clients that crashed without leaving the meeting
    /// </summary>
    public TimeSpan ViewerTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>gets how often the host checks for timed-out viewers</summary>
    public TimeSpan MaintenanceInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// gets whether screen data goes over Networking's lossy (UDP) path instead of TCP.
    /// off: Networking sends each lossy message as one UDP datagram (at most 65,507 bytes,
    /// JSON and base64 included), so full frames and big changes are dropped without an error.
    /// turn on once Networking splits large lossy messages.
    /// </summary>
    public bool SendDataLossy { get; init; }

    /// <exception cref="ArgumentOutOfRangeException">a value is zero or negative</exception>
    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(FramesPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ThumbnailHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ThumbnailFramesPerSecond);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(TileSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ThumbnailTileSize);
        ArgumentOutOfRangeException.ThrowIfNegative(FullFrameJpegQuality);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(FullFrameJpegQuality, 100);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxFramesInFlight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxViewerLag);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(AckTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ViewerTimeout, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(MaintenanceInterval, TimeSpan.Zero);
    }
}
