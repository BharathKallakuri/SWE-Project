// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;
using System.Drawing;

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
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.constructor: Initialized receiver for participant {0} (thumbnailHeight: {1}).", local.Id, thumbnailHeight);
    }

    private Subscription CreateSubscription(string streamId)
    {
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.CreateSubscription: Creating subscription for stream {0}.", streamId);
        Subscription subscription = new Subscription(streamId, _timeProvider);
        subscription.Applier.FullFrameRequested += id => {
            Trace.TraceInformation("[Screenshare] ScreenShareReceiver.CreateSubscription: Full frame requested for stream {0}.", id);
            _sendToHost(new RequestFullFrame(_local, id));
        };
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

        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.RemoveSubscription: Removed subscription for stream {0} (keepPicture: {1}).", streamId, keepPicture);
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
            Trace.TraceWarning("[Screenshare] ScreenShareReceiver.Subscribe: Unknown stream {0} requested.", streamId);
            return;
        }

        string wanted = maxHeight <= _thumbnailHeight && _withThumbnail.Contains(streamId)
            ? StreamLayers.ThumbnailOf(streamId)
            : streamId;
        if (_subscriptions.ContainsKey(wanted))
        {
            Trace.TraceInformation("[Screenshare] ScreenShareReceiver.Subscribe: Already subscribed to stream {0}.", wanted);
            return;
        }

        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.Subscribe: Subscribing participant {0} to stream {1} (layer: {2}, maxHeight: {3}).", _local.Id, streamId, wanted, maxHeight);
        string other = wanted == streamId ? StreamLayers.ThumbnailOf(streamId) : streamId;
        if (RemoveSubscription(other, keepPicture: true))
        {
            try
            {
                _sendToHost(new Unsubscribe(_local, other));
            }
            catch (Exception ex)
            {
                Trace.TraceError("[Screenshare] ScreenShareReceiver.Subscribe: Failed to send Unsubscribe for stream {0}: {1}", other, ex);
            }
        }

        _subscriptions[wanted] = CreateSubscription(wanted);
        try
        {
            _sendToHost(new Subscribe(_local, wanted, maxHeight));
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareReceiver.Subscribe: Failed to send Subscribe for stream {0}: {1}", wanted, ex);
            throw;
        }
    }

    /// <summary>
    /// stops receiving a sharer's stream and forgets its picture
    /// </summary>
    public void Unsubscribe(string streamId)
    {
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.Unsubscribe: Unsubscribing participant {0} from stream {1}.", _local.Id, streamId);
        foreach (string layer in new[] { streamId, StreamLayers.ThumbnailOf(streamId) })
        {
            if (RemoveSubscription(layer))
            {
                try
                {
                    _sendToHost(new Unsubscribe(_local, layer));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[Screenshare] ScreenShareReceiver.Unsubscribe: Failed to send Unsubscribe for stream {0}: {1}", layer, ex);
                }
            }
        }
    }

    /// <summary>
    /// gets a copy of a stream's latest image, or null.
    /// </summary>
    public Bitmap? GetFrame(string streamId)
    {
        Bitmap? frame = _frames.Get(streamId);
        if (frame == null)
        {
            Trace.TraceInformation("[Screenshare] ScreenShareReceiver.GetFrame: No cached frame available for stream {0}.", streamId);
        }
        return frame;
    }

    private void OnStreamStarted(StreamStarted started)
    {
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.OnStreamStarted: Stream started: {0} ({1}).", started.StreamId, started.DisplayName);

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
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.OnStreamStopped: Stream stopped: {0}.", stopped.StreamId);

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
            Trace.TraceWarning("[Screenshare] ScreenShareReceiver.OnData: Received packet for unsubscribed stream {0}, frame {1}.", packet.StreamId, packet.FrameNumber);
            return;
        }

        // acknowledge receipt even if the packet turns out unusable, so the host keeps pacing
        try
        {
            _sendToHost(new FrameAck(_local, packet.StreamId, packet.FrameNumber));
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareReceiver.OnData: Failed to send FrameAck for stream {0}, frame {1}: {2}", packet.StreamId, packet.FrameNumber, ex);
        }

        PacketApplyOutcome outcome = subscription.Applier.Apply(packet);
        if (outcome != PacketApplyOutcome.Applied)
        {
            Trace.TraceWarning("[Screenshare] ScreenShareReceiver.OnData: Packet {0} for stream {1} was not applied. Outcome: {2}.", packet.FrameNumber, packet.StreamId, outcome);
            return;
        }

        if (packet.IsPulse)
        {
            Trace.TraceInformation("[Screenshare] ScreenShareReceiver.OnData: Pulse received and applied for stream {0}, frame {1}.", packet.StreamId, packet.FrameNumber);
            return;
        }

        // either of a sharer's streams draws the same tile
        string sharer = StreamLayers.BaseOf(packet.StreamId);
        try
        {
            using (Bitmap frame = subscription.Stitcher.GetCurrentFrame()!)
            {
                _frames.Update(sharer, frame);
            }

            FrameUpdated?.Invoke(sharer);
            Trace.TraceInformation("[Screenshare] ScreenShareReceiver.OnData: Frame updated for sharer {0} (frame {1}).", sharer, packet.FrameNumber);
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareReceiver.OnData: Failed to update frame for sharer {0}: {1}", sharer, ex);
        }
    }

    /// <summary>
    /// handles a message the host sent to this participant
    /// </summary>
    public void Handle(IScreenShareMessage message)
    {
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.Handle: Handling message of kind {0}.", message?.Kind);

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

            default:
                Trace.TraceWarning("[Screenshare] ScreenShareReceiver.Handle: Unhandled message kind {0}.", message?.Kind);
                break;
        }
    }

    public void Dispose()
    {
        Trace.TraceInformation("[Screenshare] ScreenShareReceiver.Dispose: Disposing ScreenShareReceiver.");
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
