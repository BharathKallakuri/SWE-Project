// Authot = Devapriya Pradeep
// Roll Number = 112301006

using System.Diagnostics;
using System.Net;
using ScreenShare;

namespace Screenshare.Resilience;

/// <summary>
/// the host's view of its peers' traffic: feeds PeerLivenessTracker and stops the host
/// from sending to peers that are already known to be gone.
/// </summary>
public sealed class HostPeerMonitor : ITrafficObserver
{
    private readonly PeerLivenessTracker _tracker;

    public HostPeerMonitor(PeerLivenessTracker tracker)
    {
        ArgumentNullException.ThrowIfNull(tracker);

        _tracker = tracker;
    }

    /// <summary>
    /// host updates peer liveness when it gets a message
    /// </summary>
    /// <param name="message"></param>
    public void OnInbound(byte[] message)
    {
        if (!WireFormat.TryPeekKind(message, out ScreenShareMessageKind kind))
        {
            return;
        }

        if (kind == ScreenShareMessageKind.Data)
        {
            if (WireFormat.TryPeekStreamId(message, out string? streamId))
            {
                // sharer is alive, so update their liveness
                _tracker.OnInboundData(streamId);
            }

            return;
        }

        // decoding cheap for control messages, we need the endpoint here
        if (ScreenShareCodec.Decode(message, out IScreenShareMessage? decoded))
        {
            _tracker.OnInbound(decoded);
        }
        else
        {
            Trace.TraceWarning($"[ScreenShare] host could not decode a {kind} message, so it does not count as a sign of life");
        }
    }

    /// <summary>
    /// host sends to peer if its still there, expecting an ack
    /// </summary>
    /// <param name="to"></param>
    /// <param name="message"></param>
    public void OnOutbound(IPEndPoint to, byte[] message)
    {
        if (WireFormat.TryPeekKind(message, out ScreenShareMessageKind kind) &&
            kind == ScreenShareMessageKind.Data)
        {
            _tracker.OnOutboundData(to);
        }
    }

    public bool ShouldSend(IPEndPoint to)
    {
        return !_tracker.IsLost(to);
    }

    public IReadOnlyList<LostPeer> Sweep()
    {
        return _tracker.Sweep();
    }
}
