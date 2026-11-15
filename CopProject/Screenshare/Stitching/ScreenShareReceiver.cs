using System.Drawing;
using ScreenShare;

namespace ScreenShare;

/// <summary>
/// receives other participants' streams. a sharer may send two streams (see StreamLayers): the
/// full one and a small grid stream. the UI only ever sees the sharer's id; this class picks
/// which of the two to subscribe to from the size the tile asks for, and keeps the picture
/// under the sharer's id either way.
/// </summary>
internal sealed class ScreenShareReceiver : IDisposable
{
    private readonly Participant _local;
    private readonly Action<IScreenShareMessage> _sendToHost;
    private readonly TimeProvider _timeProvider;
    private readonly int _thumbnailHeight;
    private readonly Dictionary<string, string> _streamNames = new();       // sharer's id -> name
    private readonly HashSet<string> _withThumbnail = new();                 // sharers offering a grid stream
    private readonly Dictionary<string, Subscription> _subscriptions = new(); // by the stream actually received
    private readonly StreamFrameCache _frames = new();                        // by the sharer's id

    /// <summary>raised with the stream id and display name when a stream starts</summary>
    public event Action<string, string>? StreamAdded;

    /// <summary>raised with the stream id when a stream stops</summary>
    public event Action<string>? StreamRemoved;

    /// <summary>raised with the stream id when a subscribed stream's picture changed</summary>
    public event Action<string>? FrameUpdated;

    /// <param name="local">this participant, sent with every request so the host can reply</param>
    /// <param name="sendToHost">sends a message to the host</param>
    /// <param name="thumbnailHeight">a tile asking for this height or less gets the grid stream;
    /// 0 to always take the full stream</param>
    public ScreenShareReceiver(
        Participant local,
        Action<IScreenShareMessage> sendToHost,
        TimeProvider timeProvider,
        int thumbnailHeight = 0)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(sendToHost);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        _sendToHost = sendToHost;
        _timeProvider = timeProvider;
        _thumbnailHeight = thumbnailHeight;
    }

    private Subscription CreateSubscription(string streamId)
    {
        Subscription subscription = new Subscription(streamId, _timeProvider);
        subscription.Applier.FullFrameRequested += id => _sendToHost(new RequestFullFrame(_local, id));
        return subscription;
    }

    /// <param name="keepPicture">true when switching between a sharer's two streams, so the
    /// tile keeps showing the last picture until the other stream's first frame arrives</param>
    private bool RemoveSubscription(string streamId, bool keepPicture = false)
    {
        if (!_subscriptions.Remove(streamId, out Subscription? subscription))
        {
            return false;
        }

        subscription.Dispose();
        if (!keepPicture)
        {
            _frames.Remove(StreamLayers.BaseOf(streamId));
        }

        return true;
    }

    /// <summary>
    /// starts receiving a sharer's stream, or switches between its full and grid streams when
    /// the tile's size changes. the host replies with the sharer's next full frame.
    /// unknown streams and unchanged subscriptions are ignored.
    /// </summary>
    /// <param name="streamId">the sharer's id, as given by StreamAdded</param>
    public void Subscribe(string streamId, int maxHeight)
    {
        if (!_streamNames.ContainsKey(streamId))
        {
            return;
        }

        string wanted = maxHeight <= _thumbnailHeight && _withThumbnail.Contains(streamId)
            ? StreamLayers.ThumbnailOf(streamId)
            : streamId;
        if (_subscriptions.ContainsKey(wanted))
        {
            return;
        }

        string other = wanted == streamId ? StreamLayers.ThumbnailOf(streamId) : streamId;
        if (RemoveSubscription(other, keepPicture: true))
        {
            _sendToHost(new Unsubscribe(_local, other));
        }

        _subscriptions[wanted] = CreateSubscription(wanted);
        _sendToHost(new Subscribe(_local, wanted, maxHeight));
    }

    /// <summary>
    /// stops receiving a sharer's stream and forgets its picture
    /// </summary>
    public void Unsubscribe(string streamId)
    {
        foreach (string layer in new[] { streamId, StreamLayers.ThumbnailOf(streamId) })
        {
            if (RemoveSubscription(layer))
            {
                _sendToHost(new Unsubscribe(_local, layer));
            }
        }
    }

    /// <summary>
    /// gets a copy of a stream's latest image, or null.
    /// </summary>
    public Bitmap? GetFrame(string streamId)
    {
        return _frames.Get(streamId);
    }

    private void OnStreamStarted(StreamStarted started)
    {
        // a restarted stream numbers its frames from 0 again, so start a fresh picture;
        // the host sends this subscriber the sharer's next full frame
        if (_subscriptions.Remove(started.StreamId, out Subscription? old))
        {
            old.Dispose();
            _subscriptions[started.StreamId] = CreateSubscription(started.StreamId);
        }

        // a grid stream is not a new participant: remember it is on offer, and stop there
        if (StreamLayers.IsThumbnail(started.StreamId))
        {
            _withThumbnail.Add(StreamLayers.BaseOf(started.StreamId));
            return;
        }

        bool isNew = !_streamNames.ContainsKey(started.StreamId);
        _streamNames[started.StreamId] = started.DisplayName;

        if (isNew)
        {
            StreamAdded?.Invoke(started.StreamId, started.DisplayName);
        }
    }

    private void OnStreamStopped(StreamStopped stopped)
    {
        if (StreamLayers.IsThumbnail(stopped.StreamId))
        {
            _withThumbnail.Remove(StreamLayers.BaseOf(stopped.StreamId));
            RemoveSubscription(stopped.StreamId, keepPicture: true);
            return;
        }

        // the sharer stopped: drop whichever of its streams we had, and its picture
        RemoveSubscription(StreamLayers.ThumbnailOf(stopped.StreamId), keepPicture: true);
        _withThumbnail.Remove(stopped.StreamId);
        RemoveSubscription(stopped.StreamId);
        _frames.Remove(stopped.StreamId);
        if (_streamNames.Remove(stopped.StreamId))
        {
            StreamRemoved?.Invoke(stopped.StreamId);
        }
    }

    private void OnData(ScreenDataPacket packet)
    {
        if (!_subscriptions.TryGetValue(packet.StreamId, out Subscription? subscription))
        {
            return;
        }

        // acknowledge receipt even if the packet turns out unusable, so the host keeps pacing
        _sendToHost(new FrameAck(_local, packet.StreamId, packet.FrameNumber));

        PacketApplyOutcome outcome = subscription.Applier.Apply(packet);
        if (outcome != PacketApplyOutcome.Applied || packet.IsPulse)
        {
            return;
        }

        // either of a sharer's streams draws the same tile
        string sharer = StreamLayers.BaseOf(packet.StreamId);
        using (Bitmap frame = subscription.Stitcher.GetCurrentFrame()!)
        {
            _frames.Update(sharer, frame);
        }

        FrameUpdated?.Invoke(sharer);
    }

    /// <summary>
    /// handles a message the host sent to this participant
    /// </summary>
    public void Handle(IScreenShareMessage message)
    {
        switch (message)
        {
            case StreamStarted started:
                OnStreamStarted(started);
                break;

            case StreamStopped stopped:
                OnStreamStopped(stopped);
                break;

            case ScreenDataPacket packet:
                OnData(packet);
                break;
        }
    }

    public void Dispose()
    {
        foreach (Subscription subscription in _subscriptions.Values)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _frames.Dispose();
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
