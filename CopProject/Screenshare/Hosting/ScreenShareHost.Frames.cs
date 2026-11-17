// Author = Sumit
// Roll Number = 112301032

using System.Diagnostics;
using System.Net;

namespace ScreenShare;

// frames, acks, flow control and full-frame requests.
// the other half, streams and viewers, is in ScreenShareHost.cs.
//
// the host only forwards: it never decodes a frame, so relaying stays cheap with many streams.
// a viewer that needs a full frame gets the sharer's next one;
// the host asks the sharer for it, at most once per interval per stream.
internal sealed partial class ScreenShareHost
{
    /// <summary>the least time between two full-frame requests to the same sharer</summary>
    internal static readonly TimeSpan FullFrameRequestInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// drops viewers that have left frames unacknowledged for longer than the viewer timeout,
    /// </summary>
    public void RemoveTimedOutViewers()
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        foreach (HostedStream stream in _streams.Values)
        {
            List<ViewerState> timedOut = stream.Viewers.Values
                .Where(v => v.HasUnacknowledgedFrames && now - v.WaitingSince >= _options.ViewerTimeout)
                .ToList();

            foreach (ViewerState viewer in timedOut)
            {
                stream.Viewers.Remove(viewer.Viewer.Endpoint);
                Trace.TraceInformation("[ScreenShare] Removed the Timedout userendpoint {0} for" +
                    " user {1}", viewer.Viewer.Endpoint, viewer.Viewer.Id);
            }

            if (timedOut.Count > 0)
            {
                TellSharerWhoWatches(stream);
            }
        }
    }

    /// <summary>
    /// sends the sharer the stream's current viewers, so it pauses a stream nobody watches
    /// (e.g. the full stream while every tile is small) and resumes it when someone does
    /// </summary>
    private void TellSharerWhoWatches(HostedStream stream)
    {
        Trace.TraceInformation(
            "[Screenshare] ScreenShareHost.TellSharerWhoWatches: {0} has {1} viewer(s).",
            stream.Started.StreamId,
            stream.Viewers.Count);
        Send(stream.Sharer.Endpoint, new UpdateSendToList(stream.Started.StreamId, stream.Viewers.Keys.ToList()));
    }

    private void OnData(ScreenDataPacket packet, byte[] bytes)
    {
        if (!_streams.TryGetValue(packet.StreamId, out HostedStream? stream))
        {
            return;
        }

        // acknowledge receipt, so the sharer can send its next frame
        Send(stream.Sharer.Endpoint, new FrameAck(_self, packet.StreamId, packet.FrameNumber));

        if (packet.IsFullFrame)
        {
            // the request was answered; whoever still needs a full frame may ask again at once
            stream.FullFrameRequestedAt = null;
        }

        bool someoneWaiting = false;
        foreach (ViewerState viewer in stream.Viewers.Values)
        {
            if (viewer.NeedsFullFrame)
            {
                if (IsLagging(viewer))
                {
                    continue;
                }

                if (packet.IsFullFrame)
                {
                    SendToViewer(viewer, packet, bytes);
                    viewer.NeedsFullFrame = false;
                    Trace.TraceInformation(
                        "[Screenshare] ScreenShareHost.OnData: {0} got full frame {1} of {2}.",
                        viewer.Viewer.Id,
                        packet.FrameNumber,
                        packet.StreamId);
                }
                else
                {
                    someoneWaiting = true;
                }

                continue;
            }

            if (IsLagging(viewer))
            {
                // stop sending deltas; once it catches up it gets one full frame instead
                viewer.NeedsFullFrame = true;
                Trace.TraceWarning(
                    "[Screenshare] ScreenShareHost.OnData: {0} is {1} frames behind on {2}; pausing it until it catches up.",
                    viewer.Viewer.Id,
                    viewer.LastSent - viewer.LastAcked,
                    packet.StreamId);
                continue;
            }

            SendToViewer(viewer, packet, bytes);
        }

        if (someoneWaiting)
        {
            RequestFullFrame(stream);
        }
    }

    private void OnViewerAck(FrameAck ack)
    {
        if (!_streams.TryGetValue(ack.StreamId, out HostedStream? stream) ||
            !stream.Viewers.TryGetValue(ack.Viewer.Endpoint, out ViewerState? viewer))
        {
            return;
        }

        if (ack.FrameNumber > viewer.LastAcked && ack.FrameNumber <= viewer.LastSent)
        {
            viewer.LastAcked = ack.FrameNumber;
            viewer.WaitingSince = _timeProvider.GetUtcNow();

            // a viewer that fell behind has caught up: get it a full frame without waiting
            if (viewer.NeedsFullFrame && !IsLagging(viewer))
            {
                RequestFullFrame(stream);
            }
        }
    }

    private void OnFullFrameRequest(RequestFullFrame request)
    {
        if (!_streams.TryGetValue(request.StreamId, out HostedStream? stream) ||
            !stream.Viewers.TryGetValue(request.Viewer.Endpoint, out ViewerState? viewer))
        {
            return;
        }

        // the viewer lost its picture; it gets the sharer's next full frame
        Trace.TraceInformation(
            "[Screenshare] ScreenShareHost.OnFullFrameRequest: {0} asked for a full frame of {1}.",
            viewer.Viewer.Id,
            request.StreamId);
        viewer.NeedsFullFrame = true;
        RequestFullFrame(stream);
    }

    /// <summary>
    /// asks the sharer to send a full frame next, unless the host already asked within
    /// FullFrameRequestInterval and is still waiting; several viewers joining at once share one
    /// </summary>
    private void RequestFullFrame(HostedStream stream)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (stream.FullFrameRequestedAt is DateTimeOffset last && now - last < FullFrameRequestInterval)
        {
            return;
        }

        stream.FullFrameRequestedAt = now;
        Trace.TraceInformation(
            "[Screenshare] ScreenShareHost.RequestFullFrame: asking the sharer of {0} for a full frame.",
            stream.Started.StreamId);
        Send(stream.Sharer.Endpoint, new RequestFullFrame(_self, stream.Started.StreamId));
    }

    private void SendToViewer(ViewerState viewer, ScreenDataPacket packet, byte[] bytes)
    {
        // the timeout clock starts when the viewer goes from fully caught up to owing an ack
        if (!viewer.HasUnacknowledgedFrames)
        {
            viewer.WaitingSince = _timeProvider.GetUtcNow();
        }

        _output.Send(viewer.Viewer.Endpoint, packet, bytes);
        viewer.LastSent = packet.FrameNumber;
    }

    private bool IsLagging(ViewerState viewer)
    {
        return viewer.LastSent - viewer.LastAcked > _options.MaxViewerLag;
    }

    /// <summary>
    /// a running stream: who shares it, who watches it, and whether a full frame was asked for
    /// </summary>
    private sealed class HostedStream
    {
        public HostedStream(StreamStarted started)
        {
            Started = started;
        }

        public StreamStarted Started { get; }

        public Participant Sharer => Started.Sharer;

        public Dictionary<IPEndPoint, ViewerState> Viewers { get; } = new();

        /// <summary>when the host last asked the sharer for a full frame; null when none is pending</summary>
        public DateTimeOffset? FullFrameRequestedAt { get; set; }
    }

    /// <summary>
    /// how far a viewer has got with a stream
    /// </summary>
    private sealed class ViewerState
    {
        public ViewerState(Participant viewer)
        {
            Viewer = viewer;
        }

        public Participant Viewer { get; }

        /// <summary>true until the viewer has been sent a full frame to start from</summary>
        public bool NeedsFullFrame { get; set; } = true;

        public long LastSent { get; set; } = -1;

        public long LastAcked { get; set; } = -1;

        /// <summary>when the viewer last caught up, or started owing an ack</summary>
        public DateTimeOffset WaitingSince { get; set; }

        public bool HasUnacknowledgedFrames => LastSent > LastAcked;
    }
}
