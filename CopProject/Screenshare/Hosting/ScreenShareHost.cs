// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;
using System.Net;
using Screenshare;

namespace ScreenShare;

internal sealed partial class ScreenShareHost : IDisposable
{
    private readonly Participant _self;
    private readonly IRealyOutput _output;
    private readonly ScreenShareOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, HostedStream> _streams = new();

    /// <param name="self">the host's own participant, used as the sender of acks and requests</param>
    public ScreenShareHost(Participant self, IRealyOutput output, ScreenShareOptions options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _self = self;
        _output = output;
        _options = options;
        _timeProvider = timeProvider;
        Trace.TraceInformation("[Screenshare] ScreenShareHost.constructor: Initialized host for participant {0}.", self.Id);
    }

    /// <summary>gets the ids of the running streams</summary>
    public IReadOnlyCollection<string> StreamIds => _streams.Keys;

    /// <summary>gets the endpoints subscribed to a stream, or an empty list</summary>
    public IReadOnlyCollection<IPEndPoint> GetViewers(string streamId)
    {
        return _streams.TryGetValue(streamId, out HostedStream? stream)
            ? stream.Viewers.Keys
            : Array.Empty<IPEndPoint>();
    }

    /// <summary>
    /// handles a message that a participant, or the host itself, sent to the host
    /// </summary>
    /// <param name="message">the decoded message</param>
    /// <param name="bytes">the encoded message, forwarded as is</param>
    public void Handle(IScreenShareMessage message, byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(bytes);

        Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: Handling message kind {0}.", message.Kind);

        switch (message)
        {
            case Hello hello:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: Hello received from viewer {0} ({1}). Sending {2} active streams.", hello.Viewer.Id, hello.Viewer.Endpoint, _streams.Count);
                foreach (HostedStream stream in _streams.Values)
                {
                    Send(hello.Viewer.Endpoint, stream.Started);
                }
                break;

            case StreamStarted started:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: StreamStarted received for stream {0}.", started.StreamId);
                OnStreamStarted(started, bytes);
                break;

            case StreamStopped stopped:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: StreamStopped received for stream {0}.", stopped.StreamId);
                if (RemoveStream(stopped.StreamId))
                {
                    try
                    {
                        _output.Broadcast(stopped, bytes);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("[Screenshare] ScreenShareHost.Handle: Failed to broadcast StreamStopped for stream {0}: {1}", stopped.StreamId, ex);
                    }
                }
                break;

            case Subscribe subscribe:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: Subscribe received for stream {0} from viewer {1}.", subscribe.StreamId, subscribe.Viewer.Id);
                OnSubscribe(subscribe);
                break;

            case Unsubscribe unsubscribe:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: Unsubscribe received for stream {0} from viewer {1}.", unsubscribe.StreamId, unsubscribe.Viewer.Id);
                if (_streams.TryGetValue(unsubscribe.StreamId, out HostedStream? unsubscribed) &&
                    unsubscribed.Viewers.Remove(unsubscribe.Viewer.Endpoint))
                {
                    TellSharerWhoWatches(unsubscribed);
                }
                break;

            case ScreenDataPacket packet:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: ScreenDataPacket received for stream {0}, frame {1}.", packet.StreamId, packet.FrameNumber);
                OnData(packet, bytes);
                break;

            case FrameAck ack:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: FrameAck received for stream {0}, frame {1} from viewer {2}.", ack.StreamId, ack.FrameNumber, ack.Viewer.Id);
                OnViewerAck(ack);
                break;

            case RequestFullFrame request:
                Trace.TraceInformation("[Screenshare] ScreenShareHost.Handle: RequestFullFrame received for stream {0} from viewer {1}.", request.StreamId, request.Viewer.Id);
                OnFullFrameRequest(request);
                break;

            default:
                Trace.TraceWarning("[Screenshare] ScreenShareHost.Handle: Unhandled message kind {0}.", message.Kind);
                break;
        }
    }

    /// <summary>
    /// drops everything belonging to a participant who left: their subscriptions, and their
    /// stream if they were sharing
    /// </summary>
    public void OnParticipantLeft(string participantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);

        Trace.TraceInformation("[Screenshare] ScreenShareHost.OnParticipantLeft: Cleaning up streams and subscriptions for leaving participant {0}.", participantId);

        foreach (HostedStream stream in _streams.Values.ToList())
        {
            if (stream.Sharer.Id == participantId)
            {
                RemoveStream(stream.Started.StreamId);
                Broadcast(new StreamStopped(stream.Started.StreamId));
                continue;
            }

            List<ViewerState> leaving = stream.Viewers.Values.Where(v => v.Viewer.Id == participantId).ToList();
            foreach (ViewerState viewer in leaving)
            {
                stream.Viewers.Remove(viewer.Viewer.Endpoint);
            }

            if (leaving.Count > 0)
            {
                TellSharerWhoWatches(stream);
            }
        }
    }

    public void Dispose()
    {
        Trace.TraceInformation("[Screenshare] ScreenShareHost.Dispose: Disposing ScreenShareHost.");
        _streams.Clear();
    }

    private void OnStreamStarted(StreamStarted started, byte[] bytes)
    {
        // a restarted stream numbers its frames from 0 again, so existing viewers start over
        // with a full frame
        List<Participant> carriedViewers = _streams.TryGetValue(started.StreamId, out HostedStream? previous)
            ? previous.Viewers.Values.Select(v => v.Viewer).ToList()
            : new List<Participant>();
        RemoveStream(started.StreamId);

        // the sharer's first frame is always a full frame, so carried viewers need no request
        var stream = new HostedStream(started);

        foreach (Participant viewer in carriedViewers)
        {
            stream.Viewers[viewer.Endpoint] = new ViewerState(viewer);
        }

        _streams[started.StreamId] = stream;
        try
        {
            _output.Broadcast(started, bytes);
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareHost.OnStreamStarted: Failed to broadcast StreamStarted for stream {0}: {1}", started.StreamId, ex);
        }

        Trace.TraceInformation("[Screenshare] ScreenShareHost.OnStreamStarted: Registered stream {0} for sharer {1} with {2} carried viewers.", started.StreamId, started.Sharer.Id, carriedViewers.Count);

        // a new stream usually has no viewers yet, so the sharer pauses it after this first frame
        TellSharerWhoWatches(stream);
    }

    private void OnSubscribe(Subscribe subscribe)
    {
        if (!_streams.TryGetValue(subscribe.StreamId, out HostedStream? stream))
        {
            Trace.TraceWarning("[Screenshare] ScreenShareHost.OnSubscribe: Subscribe received for unknown stream {0} from viewer {1}.", subscribe.StreamId, subscribe.Viewer.Id);
            return;
        }

        // a (re)subscribing viewer starts with a fresh stitcher, so treat it as new;
        // it gets the sharer's next full frame
        stream.Viewers[subscribe.Viewer.Endpoint] = new ViewerState(subscribe.Viewer);
        Trace.TraceInformation("[Screenshare] ScreenShareHost.OnSubscribe: Added viewer {0} ({1}) to stream {2}.", subscribe.Viewer.Id, subscribe.Viewer.Endpoint, subscribe.StreamId);
        TellSharerWhoWatches(stream);
        RequestFullFrame(stream);
    }

    private bool RemoveStream(string streamId)
    {
        return _streams.Remove(streamId);
    }

    private void Send(IPEndPoint to, IScreenShareMessage message)
    {
        try
        {
            _output.Send(to, message, ScreenShareCodec.Encode(message));
            Trace.TraceInformation("[Screenshare] ScreenShareHost.Send: Sent {0} to {1}.", message.Kind, to);
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareHost.Send: Failed to send {0} to {1}: {2}", message.Kind, to, ex);
            throw;
        }
    }

    private void Broadcast(IScreenShareMessage message)
    {
        try
        {
            _output.Broadcast(message, ScreenShareCodec.Encode(message));
            Trace.TraceInformation("[Screenshare] ScreenShareHost.Broadcast: Broadcasted {0}.", message.Kind);
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareHost.Broadcast: Failed to broadcast {0}: {1}", message.Kind, ex);
            throw;
        }
    }
}
