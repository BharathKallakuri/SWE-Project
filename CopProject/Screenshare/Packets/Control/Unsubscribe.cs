namespace ScreenShare; 

public sealed record Unsubscribe : IScreenShareMessage
{

    public Participant Viewer { get; }
    public string StreamId { get;
    }
    public ScreenShareMessageKind Kind => ScreenShareMessageKind.Unsubscribe;


    public Unsubscribe(Participant viewer, string streamId)
    {
        ArgumentNullException.ThrowIfNull(viewer, nameof(viewer));
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId, nameof(streamId));
        Viewer = viewer;
        StreamId = streamId;
    }


}
