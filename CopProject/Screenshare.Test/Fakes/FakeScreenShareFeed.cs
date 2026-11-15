using ScreenShare.Interfaces;
using ScreenShare.Models;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the meeting's screen sharing when testing the view model: a test raises
/// joins, leaves and frames itself, and checks which tiles were asked for.
/// </summary>
internal sealed class FakeScreenShareFeed : IScreenShareFeed
{
    public event Action<string, string>? ParticipantJoined;

    public event Action<string>? ParticipantLeft;

    public event Action<string, ScreenFrame>? FrameReady;

    /// <summary>every RequestFeeds call, in order</summary>
    public List<IReadOnlyDictionary<string, int>> Requests { get; } = new();

    public bool IsSharing { get; private set; }

    public int StartCalls { get; private set; }

    public int StopCalls { get; private set; }

    /// <summary>when set, StartSharing throws it, like a meeting that already ended</summary>
    public Exception? StartException { get; set; }

    /// <summary>true while someone listens to any of the feed's events</summary>
    public bool HasListeners => ParticipantJoined != null || ParticipantLeft != null || FrameReady != null;

    public void RequestFeeds(IReadOnlyDictionary<string, int> idToResolution)
    {
        Requests.Add(new Dictionary<string, int>(idToResolution));
    }

    public void StartSharing()
    {
        StartCalls++;
        if (StartException != null)
        {
            throw StartException;
        }

        IsSharing = true;
    }

    /// <summary>when set, StopSharing throws it and sharing carries on</summary>
    public Exception? StopException { get; set; }

    public void StopSharing()
    {
        StopCalls++;
        if (StopException != null)
        {
            throw StopException;
        }

        IsSharing = false;
    }

    public void RaiseJoined(string id, string name) => ParticipantJoined?.Invoke(id, name);

    public void RaiseLeft(string id) => ParticipantLeft?.Invoke(id);

    public void RaiseFrame(string id, ScreenFrame frame) => FrameReady?.Invoke(id, frame);
}
