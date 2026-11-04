namespace ScreenShare;

public sealed record StreamStopped : IScreenShareMessage
{
    public string StreamId { get; }
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.StreamStopped;

    public StreamStopped(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        StreamId = streamId; 
    }
}
