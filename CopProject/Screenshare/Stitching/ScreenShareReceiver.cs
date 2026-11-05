using System.Drawing;
using ScreenShare;

namespace Screenshare;

internal sealed class ScreenShareReceiver : IDisposable
{
    private readonly Participant _local;
    private readonly Action<IScreenShareMessage> _sendToHost;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, string> _streamNames = new();
    private readonly Dictionary<string, Subscription> _subscriptions = new();
    private readonly StreamFrameCache _frames = new();

    /// <summary>raised with the stream id and display name when a stream starts</summary>
    public event Action<string, string>? StreamAdded;

    /// <summary>raised with the stream id when a stream stops</summary>
    public event Action<string>? StreamRemoved;

    /// <summary>raised with the stream id when a subscribed stream's picture changed</summary>
    public event Action<string>? FrameUpdated;

    /// <param name="local">this participant, sent with every request so the host can reply</param>
    /// <param name="sendToHost">sends a message to the host</param>
    public ScreenShareReceiver(
        Participant local,
        Action<IScreenShareMessage> sendToHost,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(sendToHost);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        _sendToHost = sendToHost;
        _timeProvider = timeProvider;
    }

    private Subscription CreateSubscription(string streamId)
    {
        Subscription subscription = new Subscription(streamId, _timeProvider);
        subscription.Applier.FullFrameRequested += id => _sendToHost(new RequestFullFrame(_local, id));
        return subscription;
    }

    private bool RemoveSubscription(string streamId)
    {
        if (!_subscriptions.Remove(streamId, out Subscription? subscription))
        {
            return false;
        }

        subscription.Dispose();
        _frames.Remove(streamId);
        return true;
    }

    /// <summary>
    /// starts receiving a stream. the host replies with a full frame from its copy.
    /// unknown streams and existing subscriptions are ignored.
    /// </summary>
    public void Subscribe(string streamId, int maxHeight)
    {
        if (!_streamNames.ContainsKey(streamId) || _subscriptions.ContainsKey(streamId))
        {
            return;
        }

        _subscriptions[streamId] = CreateSubscription(streamId);
        _sendToHost(new Subscribe(_local, streamId, maxHeight));
    }

    /// <summary>
    /// stops receiving a stream and forgets its picture
    /// </summary>
    public void Unsubscribe(string streamId)
    {
        if (!RemoveSubscription(streamId))
        {
            return;
        }

        _sendToHost(new Unsubscribe(_local, streamId));
    }

    public void Dispose()
    {

    }

    /// <summary>
    /// implemented for internal use
    /// </summary>
    private sealed class Subscription : IDisposable
    {
        public ImageStitcher Stitcher { get; } = new();
        public ScreenPacketApplier Applier { get; }

        public Subscription(string streamId, TimeProvider timeProvider)
        {
            Applier = new ScreenPacketApplier(streamId, Stitcher, timeProvider);
        }

        public void Dispose()
        {
            Stitcher.Dispose();
        }
    }
}
