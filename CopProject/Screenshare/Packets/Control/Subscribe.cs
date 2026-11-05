namespace ScreenShare;

/// <summary>
/// sent by a viewer to the host to start receiving a stream, or to change its quality
/// </summary>
public sealed record Subscribe : IScreenShareMessage
{
    /// <summary>gets the viewer asking for the stream</summary>
    public Participant Viewer { get; }

    /// <summary>gets the id of the requested stream</summary>
    public string StreamId { get; }

    /// <summary>gets the largest frame height the viewer wants, e.g. 720 or 480</summary>
    public int MaxHeight { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Subscribe;

    /// <exception cref="ArgumentNullException">the viewer is null</exception>
    /// <exception cref="ArgumentException">the stream id is empty or the height is not positive</exception>
    public Subscribe(Participant viewer, string streamId, int maxHeight)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHeight);

        Viewer = viewer;
        StreamId = streamId;
        MaxHeight = maxHeight;
    }
}
