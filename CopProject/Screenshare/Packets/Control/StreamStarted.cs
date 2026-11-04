namespace ScreenShare; 

public sealed record StreamStarted : IScreenShareMessage
{
    public string StreamId { get; }
    public Participant Sharer { get; }
    public string DisplayName { get; }
    public int Width { get; }
    public int Height { get; }

    public ScreenShareMessageKind Kind => ScreenShareMessageKind.StreamStarted; 

    public StreamStarted(string streamId, Participant sharer, string displayName, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        ArgumentNullException.ThrowIfNull(sharer, nameof(sharer));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName, nameof(displayName));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width, nameof(width));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height, nameof(height));

        StreamId = streamId;
        Sharer = sharer;
        DisplayName = displayName;
        Width = width;
        Height = height;

    }

}
