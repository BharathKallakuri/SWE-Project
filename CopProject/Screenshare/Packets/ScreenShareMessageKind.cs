namespace ScreenShare;

public enum ScreenShareMessageKind : byte
{
    Date = 1,
    StreamStarted = 2,
    StreamStopped = 3,
    Subscribe = 4,
    Unsubscribe = 5,
    RequestFullFrame = 6,
    FrameAck = 7,
    UpdateSendToList = 8,



}
