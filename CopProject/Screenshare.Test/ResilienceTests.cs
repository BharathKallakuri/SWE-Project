// Author = Devapriya Pradeep
// Roll No = 112301006

using System.Net;
using ScreenShare;
using Xunit;

namespace Screenshare.Tests;

/// <summary>
/// disconnect detection and recovery, driven by a manual clock
/// </summary>
public class ResilienceTests
{
    private static readonly ResilienceOptions s_options = new() {
        PeerSilenceTimeout = TimeSpan.FromSeconds(8),
        HostSilenceTimeout = TimeSpan.FromSeconds(12),
        StreamStallTimeout = TimeSpan.FromSeconds(5),
        RecoveryInterval = TimeSpan.FromSeconds(3),
        ReconcileWindow = TimeSpan.FromSeconds(3),
        MaxRecoveryAttempts = 3
    };

    private static Participant Peer(string id, int lastOctet)
    {
        return new(id, new IPEndPoint(IPAddress.Parse($"10.0.0.{lastOctet}"), 5000));
    }

    private static byte[] DataBytes(string streamId, long frame)
    {
        return ScreenShareCodec.Encode(new ScreenDataPacket(
            streamId, frame, DateTime.UtcNow, 64, 64, DataPacketType.Pulse));
    }
    // ----- host: PeerLivenessTracker -----

    [Fact]
    public void Tracker_SharerThatGoesSilent_IsLostOnce()
    {
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);
        Participant sharer = Peer("s1", 2);

        tracker.OnInbound(new StreamStarted("s1", sharer, "Sam", 64, 64));
        clock.Advance(TimeSpan.FromSeconds(7));
        Assert.Empty(tracker.Sweep());

        clock.Advance(TimeSpan.FromSeconds(2));
        LostPeer lost = Assert.Single(tracker.Sweep());
        Assert.Equal("s1", lost.ParticipantId);
        Assert.Equal(sharer.Endpoint, lost.Endpoint);

        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.Empty(tracker.Sweep());
    }

    [Fact]
    public void Tracker_SharerThatKeepsSendingData_IsNotLost()
    {
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);

        tracker.OnInbound(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64));

        for (int i = 0; i < 20; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            tracker.OnInboundData("s1");
            Assert.Empty(tracker.Sweep());
        }
    }

    [Fact]
    public void Tracker_ViewerThatStopsAcknowledging_IsLost()
    {
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);
        Participant viewer = Peer("v1", 3);

        tracker.OnInbound(new Subscribe(viewer, "s1", 480));
        tracker.OnOutboundData(viewer.Endpoint);
        clock.Advance(TimeSpan.FromSeconds(9));

        LostPeer lost = Assert.Single(tracker.Sweep());
        Assert.Equal("v1", lost.ParticipantId);
        Assert.True(tracker.IsLost(viewer.Endpoint));
    }

    [Fact]
    public void Tracker_ViewerThatAcknowledges_IsNotLost()
    {
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);
        Participant viewer = Peer("v1", 3);

        for (int i = 0; i < 20; i++)
        {
            tracker.OnOutboundData(viewer.Endpoint);
            clock.Advance(TimeSpan.FromSeconds(1));
            tracker.OnInbound(new FrameAck(viewer, "s1", i));
            Assert.Empty(tracker.Sweep());
        }
    }

    [Fact]
    public void Tracker_ViewerOfSilentSharer_IsNotBlamed()
    {
        // the host sends such a viewer nothing, so it has nothing to acknowledge
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);

        tracker.OnInbound(new Subscribe(Peer("v1", 3), "s1", 480));
        clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Empty(tracker.Sweep());
    }

    [Fact]
    public void Tracker_LostPeerThatReturns_IsNoLongerLost()
    {
        var clock = new ManualTimeProvider();
        var tracker = new PeerLivenessTracker(s_options, clock);
        Participant viewer = Peer("v1", 3);

        tracker.OnOutboundData(viewer.Endpoint);
        tracker.OnInbound(new Hello(viewer));
        tracker.OnOutboundData(viewer.Endpoint);
        clock.Advance(TimeSpan.FromSeconds(9));
        Assert.Single(tracker.Sweep());

        tracker.OnInbound(new Hello(viewer));

        Assert.False(tracker.IsLost(viewer.Endpoint));
    }

    // ----- client: ClientLinkMonitor -----

    [Fact]
    public void Client_StalledSubscription_IsResentWithBackoffAndThenGivenUp()
    {
        var clock = new ManualTimeProvider();
        var monitor = new ClientLinkMonitor(s_options, clock);
        Participant me = Peer("me", 9);
        var host = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 5000);

        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64)));
        monitor.OnOutbound(host, ScreenShareCodec.Encode(new Subscribe(me, "s1", 480)));

        // data arrives for a while: nothing to do
        for (int i = 0; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            monitor.OnInbound(DataBytes("s1", i));
            Assert.Empty(monitor.Sweep().Outgoing);
        }

        // then it stops
        int resent = 0;
        for (int i = 0; i < 11; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            resent += monitor.Sweep().Outgoing.OfType<Subscribe>().Count();
        }

        Assert.True(resent >= 1);
        Assert.True(resent <= s_options.MaxRecoveryAttempts);

        // data resuming resets the attempt counter
        monitor.OnInbound(DataBytes("s1", 99));
        Assert.Empty(monitor.Sweep().Outgoing);
    }

    [Fact]
    public void Client_HostSilence_ReportsDownProbesWithHelloAndRecoversOnAnswer()
    {
        var clock = new ManualTimeProvider();
        var monitor = new ClientLinkMonitor(s_options, clock);
        Participant me = Peer("me", 9);
        var host = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 5000);

        monitor.OnOutbound(host, ScreenShareCodec.Encode(new Hello(me)));
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64)));
        var subscribe = new Subscribe(me, "s1", 480);
        monitor.OnOutbound(host, ScreenShareCodec.Encode(subscribe));

        clock.Advance(TimeSpan.FromSeconds(13));
        Assert.False(monitor.Sweep().Connected);
        Assert.False(monitor.IsConnected);

        // while down it keeps knocking
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Single(monitor.Sweep().Outgoing.OfType<Hello>());

        // the host answers
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64)));
        ClientActions recovered = monitor.Sweep();

        Assert.True(recovered.Connected);
        Assert.Single(recovered.Outgoing.OfType<Hello>());
        Assert.Single(recovered.Outgoing.OfType<Subscribe>());
    }

    [Fact]
    public void Client_StreamThatEndedWhileOffline_IsReportedGone()
    {
        var clock = new ManualTimeProvider();
        var monitor = new ClientLinkMonitor(s_options, clock);
        Participant me = Peer("me", 9);
        var host = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 5000);

        monitor.OnOutbound(host, ScreenShareCodec.Encode(new Hello(me)));
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64)));
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s2", Peer("s2", 4), "Kim", 64, 64)));
        monitor.OnOutbound(host, ScreenShareCodec.Encode(new Subscribe(me, "s1", 480)));

        clock.Advance(TimeSpan.FromSeconds(13));
        monitor.Sweep();

        // back online: the host only announces s1 again
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStarted("s1", Peer("s1", 2), "Sam", 64, 64)));
        monitor.Sweep();

        clock.Advance(s_options.ReconcileWindow + TimeSpan.FromSeconds(1));
        string gone = Assert.Single(monitor.Sweep().GoneStreams);

        Assert.Equal("s2", gone);
    }

    [Fact]
    public void Client_SharerWithoutAcks_ReannouncesItsStream()
    {
        var clock = new ManualTimeProvider();
        var monitor = new ClientLinkMonitor(s_options, clock);
        Participant me = Peer("me", 9);
        var host = new IPEndPoint(IPAddress.Parse("10.0.0.1"), 5000);
        var announce = new StreamStarted("me", me, "Me", 64, 64);

        monitor.OnOutbound(host, ScreenShareCodec.Encode(announce));
        monitor.OnOutbound(host, DataBytes("me", 0));

        // keep the link looking alive so only the missing acks stand out
        clock.Advance(TimeSpan.FromSeconds(6));
        monitor.OnInbound(ScreenShareCodec.Encode(new StreamStopped("other")));

        Assert.Single(monitor.Sweep().Outgoing.OfType<StreamStarted>());
    }

    [Fact]
    public void WireFormat_PeeksStreamIdWithoutDecoding()
    {
        byte[] data = DataBytes("stream-42", 7);

        Assert.True(WireFormat.TryPeekKind(data, out ScreenShareMessageKind kind));
        Assert.Equal(ScreenShareMessageKind.Data, kind);
        Assert.True(WireFormat.TryPeekStreamId(data, out string? id));
        Assert.Equal("stream-42", id);

        Assert.False(WireFormat.TryPeekStreamId(
            ScreenShareCodec.Encode(new Hello(Peer("v", 3))), out _));
        Assert.False(WireFormat.TryPeekKind(new byte[] { 99, 1 }, out _));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 8, 10, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
        }
    }
}
