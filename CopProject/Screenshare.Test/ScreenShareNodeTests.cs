// Author = Sumit
// Roll Number = 112301032

using System.Drawing;
using System.Net;
using Networking;
using Screenshare.Tests.Fakes;
using ScreenShare;

namespace Screenshare.Tests;

public class ScreenShareNodeTests : IDisposable
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private readonly FakeNetwork _network = new();
    private readonly ManualTimeProvider _clock = new();
    private ScreenShareOptions _options = new();   // a test may replace it before making nodes
    private bool _busyScreens;                      // true: 640x360 screens of random pixels
    private readonly List<ScreenShareNode> _nodes = new();
    private readonly List<FakeImageSource> _sources = new();
    private readonly Dictionary<ScreenShareNode, FakeCommunicator> _connections = new();

    public void Dispose()
    {
        foreach (ScreenShareNode node in _nodes)
        {
            node.Dispose();
        }
    }

    private ScreenShareNode NewNode(FakeMeetingSession session)
    {
        ScreenShareNode node = new(session, NewSource, _options, _clock);
        _nodes.Add(node);
        _connections[node] = (FakeCommunicator)session.Communicator;
        return node;
    }

    private FakeImageSource NewSource()
    {
        FakeImageSource source = _busyScreens ? new(640, 360) { Busy = true } : new();
        _sources.Add(source);
        return source;
    }

    private ScreenShareNode Host(out FakeMeetingSession session)
    {
        session = FakeMeetingSession.Host(_network);
        return NewNode(session);
    }

    private ScreenShareNode Host()
    {
        return Host(out _);
    }

    private ScreenShareNode Client(string id)
    {
        return NewNode(FakeMeetingSession.Client(_network, id));
    }

    // work hops between nodes' own threads; settled when every node is idle at the same time
    private void Settle()
    {
        Assert.True(
            SpinWait.SpinUntil(() => _nodes.All(n => n.IsIdle) && _nodes.All(n => n.IsIdle), s_timeout),
            "ScreenShare work did not settle");
    }

    private void Share(ScreenShareNode node)
    {
        node.StartSharing(runCaptureLoop: false);
        Assert.True(node.CaptureOnce());
        Settle();
    }

    // the data packets of a sharer's full stream (not its small grid stream)
    private List<ScreenDataPacket> FullStreamPackets(ScreenShareNode sharer)
    {
        return MessagesFrom(sharer).OfType<ScreenDataPacket>()
            .Where(p => p.StreamId == sharer.LocalParticipant.Id)
            .ToList();
    }

    // a late joiner gets the sharer's next full frame: the sharer captures again
    private void Capture(ScreenShareNode sharer)
    {
        Assert.True(sharer.CaptureOnce());
        Settle();
    }

    private List<IScreenShareMessage> MessagesFrom(ScreenShareNode node)
    {
        return _network.Log
            .Where(m => m.From.Equals(node.LocalParticipant.Endpoint) && m.Module == ScreenShareModule.ModuleId)
            .Select(m => ScreenShareCodec.Decode(m.Bytes, out IScreenShareMessage? message) ? message : null)
            .OfType<IScreenShareMessage>()
            .ToList();
    }

    // delivers a message to a node as if the host had sent it
    private void Inject(ScreenShareNode node, IScreenShareMessage message)
    {
        _network.Inject(_connections[node], ScreenShareCodec.Encode(message), ScreenShareModule.ModuleId);
        Settle();
    }

    // construction

    [Fact]
    public void Constructor_NullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenShareNode(null!));
    }

    [Fact]
    public void Constructor_InvalidOptions_Throws()
    {
        ScreenShareOptions bad = new() { MaxViewerLag = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ScreenShareNode(FakeMeetingSession.Host(_network), NewSource, bad, _clock));
    }

    [Fact]
    public void Constructor_WithDefaults_Works()
    {
        // default options, system clock and real screen capture; sharing is not started,
        // so the screen is never captured
        using ScreenShareNode host = new(FakeMeetingSession.Host(_network));

        Assert.True(host.IsHost);
        Assert.False(host.IsSharing);
    }

    [Fact]
    public void Host_RunsRelayAndSendsNoHello()
    {
        ScreenShareNode host = Host();
        Settle();

        Assert.True(host.IsHost);
        Assert.NotNull(host.Host);
        Assert.Empty(MessagesFrom(host));
        Assert.Equal(1, _clock.ActiveTimerCount);
    }

    [Fact]
    public void Client_SaysHelloToHost()
    {
        Host();
        ScreenShareNode client = Client("sumit");
        Settle();

        Assert.False(client.IsHost);
        Assert.Null(client.Host);
        Hello hello = Assert.IsType<Hello>(Assert.Single(MessagesFrom(client)));
        Assert.Equal(client.LocalParticipant, hello.Viewer);
    }

    [Fact]
    public void LocalParticipant_UsesSessionIdAndOwnEndpoint()
    {
        Host();
        FakeMeetingSession session = FakeMeetingSession.Client(_network, "sumit");

        ScreenShareNode client = NewNode(session);

        Assert.Equal("sumit", client.LocalParticipant.Id);
        Assert.Equal(((FakeCommunicator)session.Communicator).Endpoint, client.LocalParticipant.Endpoint);
    }

    [Fact]
    public void LocalParticipant_Ipv4MappedAddress_IsReportedAsPlainIpv4()
    {
        // a dual-mode client socket reports ::ffff:10.0.0.2, but the host knows it as 10.0.0.2
        Host();
        FakeCommunicator mapped = new(_network, new IPEndPoint(IPAddress.Parse("::ffff:10.0.0.2"), 6000), isServer: false);
        FakeMeetingSession session = new(new MeetingParticipant("sumit", "Sumit", false), _network.Server.Endpoint, mapped);

        ScreenShareNode client = NewNode(session);

        Assert.Equal(IPAddress.Parse("10.0.0.2"), client.LocalParticipant.Endpoint.Address);
    }

    // sharing and viewing

    [Fact]
    public void ClientShares_OtherClientSeesStreamAndPicture()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        List<(string Id, string Name)> added = new();
        List<string> updated = new();
        sumit.StreamAdded += (id, name) => added.Add((id, name));
        sumit.FrameUpdated += updated.Add;
        Settle();

        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        Assert.Equal(("aryan", "aryan"), Assert.Single(added));
        Assert.Contains("aryan", updated);
        using Bitmap? frame = sumit.GetFrame("aryan");
        Assert.NotNull(frame);
        Assert.Equal((64, 48), (frame.Width, frame.Height));
    }

    [Fact]
    public void HostShares_ClientSeesPicture()
    {
        ScreenShareNode host = Host();
        ScreenShareNode sumit = Client("sumit");
        Settle();

        Share(host);
        sumit.Subscribe("host");
        Settle();
        Capture(host);

        using Bitmap? frame = sumit.GetFrame("host");
        Assert.NotNull(frame);
    }

    [Fact]
    public void ClientShares_HostSeesPicture()
    {
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        List<string> added = new();
        host.StreamAdded += (id, _) => added.Add(id);
        Settle();

        Share(aryan);
        host.Subscribe("aryan");
        Settle();
        Capture(aryan);

        Assert.Equal("aryan", Assert.Single(added));
        using Bitmap? frame = host.GetFrame("aryan");
        Assert.NotNull(frame);
    }

    [Fact]
    public void LateJoiner_LearnsAboutRunningStreams()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();
        Share(aryan);

        ScreenShareNode sanjay = Client("sanjay");
        Settle();

        // sanjay can only subscribe to a stream she was told about in reply to her Hello
        sanjay.Subscribe("aryan");
        Settle();
        Capture(aryan);

        using Bitmap? frame = sanjay.GetFrame("aryan");
        Assert.NotNull(frame);
    }

    [Fact]
    public void Sharing_KeepsGoingBecauseHostAcksReachSender()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan");   // someone watching, so the stream isn't paused
        Settle();

        // without the host's acks the sender would stop after a few frames in flight
        for (int i = 0; i < 20; i++)
        {
            Assert.True(aryan.CaptureOnce(), $"frame {i} was held back");
            Settle();
        }
    }

    [Fact]
    public void FullFrameRequest_ReachesSender()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);            // the full frame sumit's subscription asked for
        Capture(aryan);            // then an ordinary frame
        Assert.False(FullStreamPackets(aryan).Last().IsFullFrame);

        Inject(aryan, new RequestFullFrame(new Participant("host", _network.Server.Endpoint), "aryan"));
        Assert.True(aryan.CaptureOnce());
        Settle();

        Assert.True(FullStreamPackets(aryan).Last().IsFullFrame);
    }

    [Fact]
    public void AckOrRequest_ForAnotherStream_IsIgnored()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();
        Participant host = new("host", _network.Server.Endpoint);

        // not sharing yet
        Inject(aryan, new FrameAck(host, "aryan", 0));
        Inject(aryan, new RequestFullFrame(host, "aryan"));

        // sharing, but the messages name someone else's stream
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);            // the full frame sumit's subscription asked for
        Inject(aryan, new FrameAck(host, "someone-else", 0));
        Inject(aryan, new RequestFullFrame(host, "someone-else"));
        Inject(aryan, new UpdateSendToList("someone-else", Array.Empty<IPEndPoint>()));
        Assert.True(aryan.CaptureOnce());
        Settle();

        Assert.False(FullStreamPackets(aryan).Last().IsFullFrame);
    }

    [Fact]
    public void StartSharing_Twice_StartsOneStream()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();

        aryan.StartSharing(runCaptureLoop: false);
        aryan.StartSharing(runCaptureLoop: false);

        Assert.True(aryan.IsSharing);
        Assert.Single(_sources);
    }

    [Fact]
    public void StartSharing_WithCaptureLoop_SendsFrames()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();

        aryan.StartSharing();
        Assert.True(SpinWait.SpinUntil(() => MessagesFrom(aryan).OfType<ScreenDataPacket>().Any(), s_timeout));
        aryan.StopSharing();

        Assert.False(aryan.IsSharing);
    }

    [Fact]
    public void StopSharing_TellsViewersAndReleasesScreen()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        List<string> removed = new();
        sumit.StreamRemoved += removed.Add;
        Settle();
        Share(aryan);

        aryan.StopSharing();
        Settle();

        Assert.False(aryan.IsSharing);
        Assert.Equal("aryan", Assert.Single(removed));
        Assert.True(_sources[0].IsDisposed);
    }

    [Fact]
    public void StopSharing_WhenNotSharing_DoesNothing()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");

        aryan.StopSharing();

        Assert.False(aryan.IsSharing);
    }

    [Fact]
    public void CaptureOnce_WhenNotSharing_ReturnsFalse()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");

        Assert.False(aryan.CaptureOnce());
    }

    [Fact]
    public void Unsubscribe_StopsReceiving()
    {
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        sumit.Unsubscribe("aryan");
        Settle();

        Assert.Null(sumit.GetFrame("aryan"));
        Assert.Empty(host.Host!.GetViewers("aryan"));
    }

    [Theory]
    [InlineData("  ")]
    public void SubscribeOrUnsubscribe_BlankId_Throws(string? streamId)
    {
        ScreenShareNode host = Host();

        Assert.ThrowsAny<ArgumentException>(() => host.Subscribe(streamId!));
        Assert.ThrowsAny<ArgumentException>(() => host.Unsubscribe(streamId!));
    }

    [Fact]
    public void GetFrame_UnknownStream_IsNull()
    {
        ScreenShareNode host = Host();

        Assert.Null(host.GetFrame("nobody"));
    }

    // leaving and ending

    [Fact]
    public void SharerLeaves_HostDropsStreamForViewers()
    {
        ScreenShareNode host = Host(out FakeMeetingSession hostSession);
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        List<string> removed = new();
        sumit.StreamRemoved += removed.Add;
        Settle();
        Share(aryan);

        hostSession.RaiseParticipantLeft(new MeetingParticipant("aryan", "aryan", false));
        Settle();

        Assert.Empty(host.Host!.StreamIds);
        Assert.Equal("aryan", Assert.Single(removed));
    }

    [Fact]
    public void ParticipantLeft_OnClient_IsIgnored()
    {
        Host();
        FakeMeetingSession session = FakeMeetingSession.Client(_network, "sumit");
        ScreenShareNode sumit = NewNode(session);

        session.RaiseParticipantLeft(new MeetingParticipant("aryan", "aryan", false));
        Settle();

        Assert.False(sumit.IsHost);
    }

    [Fact]
    public void MeetingEnded_ShutsNodeDown()
    {
        Host();
        FakeMeetingSession session = FakeMeetingSession.Client(_network, "aryan");
        ScreenShareNode aryan = NewNode(session);
        Settle();
        Share(aryan);

        session.RaiseMeetingEnded();

        Assert.False(aryan.IsSharing);
        Assert.False(session.HasParticipantLeftListeners);
        Assert.False(session.HasMeetingEndedListeners);
        Assert.Throws<ObjectDisposedException>(aryan.StartSharing);
    }

    [Fact]
    public void Dispose_StopsTimerAndListening()
    {
        ScreenShareNode host = Host(out FakeMeetingSession session);
        ScreenShareNode sumit = Client("sumit");
        Settle();
        int before = _network.Log.Count;

        host.Dispose();
        Inject(host, new Hello(sumit.LocalParticipant));

        Assert.Equal(0, _clock.ActiveTimerCount);
        Assert.False(session.HasMeetingEndedListeners);
        // the host no longer listens, so nobody answered the Hello
        Assert.Equal(before, _network.Log.Count);
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        ScreenShareNode host = Host();

        host.Dispose();
        host.Dispose();

        Assert.True(host.IsIdle);
    }

    [Fact]
    public void Dispose_ConnectionAlreadyClosed_DoesNotThrow()
    {
        FakeMeetingSession inner = FakeMeetingSession.Host(_network);
        FakeMeetingSession session = new(inner.LocalUser, inner.HostEndpoint, new ClosedCommunicator(_network.Server));
        ScreenShareNode host = new(session, NewSource, _options, _clock);

        Assert.Null(Record.Exception(host.Dispose));
    }

    // maintenance

    [Fact]
    public void MaintenanceTimer_RemovesViewerThatNeverAcks()
    {
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();
        Share(aryan);

        // a viewer that is not on the network: frames to it are lost, so it never acks
        Participant ghost = new("ghost", new IPEndPoint(IPAddress.Loopback, 9999));
        Inject(host, new Subscribe(ghost, "aryan", 480));
        Assert.Single(host.Host!.GetViewers("aryan"));
        Capture(aryan);   // the ghost is sent aryan's next full frame, which it never acks

        _clock.Advance(_options.ViewerTimeout + _options.MaintenanceInterval);
        Settle();

        Assert.Empty(host.Host!.GetViewers("aryan"));
    }

    [Fact]
    public void RunMaintenance_OnHost_RemovesTimedOutViewers()
    {
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();
        Share(aryan);
        Inject(host, new Subscribe(new Participant("ghost", new IPEndPoint(IPAddress.Loopback, 9999)), "aryan", 480));
        Capture(aryan);

        _clock.Advance(_options.ViewerTimeout);   // less than one maintenance tick past the timeout
        host.RunMaintenance();
        Settle();

        Assert.Empty(host.Host!.GetViewers("aryan"));
    }

    // two streams per sharer: a small grid stream and the full one, each sent only while watched

    private const int Grid = 180;
    private const int Zoomed = 1080;

    [Fact]
    public void Sharer_OffersFullAndGridStreams_UiSeesOneTile()
    {
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        List<string> added = new();
        sumit.StreamAdded += (id, _) => added.Add(id);
        Settle();

        Share(aryan);

        Assert.Equal(new[] { "aryan", "aryan#thumb" }, host.Host!.StreamIds.Order());
        Assert.Equal("aryan", Assert.Single(added));
    }

    [Fact]
    public void GridTile_GetsSmallStream()
    {
        _busyScreens = true;   // 640x360, so the 180p grid stream is smaller
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);

        sumit.Subscribe("aryan", Grid);
        Settle();
        Capture(aryan);

        using Bitmap? frame = sumit.GetFrame("aryan");
        Assert.Equal((320, 180), (frame!.Width, frame.Height));
        Assert.Equal(sumit.LocalParticipant.Endpoint, Assert.Single(host.Host!.GetViewers("aryan#thumb")));
        Assert.Empty(host.Host.GetViewers("aryan"));
    }

    [Fact]
    public void ZoomedTile_GetsFullStream()
    {
        _busyScreens = true;
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);

        sumit.Subscribe("aryan", Zoomed);
        Settle();
        Capture(aryan);

        using Bitmap? frame = sumit.GetFrame("aryan");
        Assert.Equal((640, 360), (frame!.Width, frame.Height));
    }

    [Fact]
    public void ZoomingIn_SwitchesStream_AndKeepsOldPictureUntilNewOne()
    {
        _busyScreens = true;
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan", Grid);
        Settle();
        Capture(aryan);

        sumit.Subscribe("aryan", Zoomed);
        Settle();
        using (Bitmap? meanwhile = sumit.GetFrame("aryan"))
        {
            Assert.Equal(320, meanwhile!.Width);   // no blank tile while switching
        }

        Capture(aryan);
        using Bitmap? zoomed = sumit.GetFrame("aryan");
        Assert.Equal(640, zoomed!.Width);
        Assert.Empty(host.Host!.GetViewers("aryan#thumb"));
        Assert.Single(host.Host.GetViewers("aryan"));
    }

    [Fact]
    public void Sharer_PausesStreamsNobodyWatches()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        Settle();

        Share(aryan);   // the first frame of each stream announces it

        Assert.False(aryan.CaptureOnce());
    }

    [Fact]
    public void Sharer_SendsOnlyTheStreamThatIsWatched()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);

        sumit.Subscribe("aryan", Grid);
        Settle();
        for (int i = 0; i < 5; i++)
        {
            Capture(aryan);
        }

        // the full stream sent only its announcing frame; the grid stream kept going
        Assert.Single(FullStreamPackets(aryan));
        Assert.Equal(6, MessagesFrom(aryan).OfType<ScreenDataPacket>().Count(p => p.StreamId == "aryan#thumb"));
    }

    [Fact]
    public void StopSharing_WhileOnGridStream_ClearsPicture()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        List<string> removed = new();
        sumit.StreamRemoved += removed.Add;
        Settle();
        Share(aryan);
        sumit.Subscribe("aryan", Grid);
        Settle();
        Capture(aryan);

        aryan.StopSharing();
        Settle();

        Assert.Null(sumit.GetFrame("aryan"));
        Assert.Equal("aryan", Assert.Single(removed));
    }

    [Fact]
    public void ThumbnailNotSmaller_OnlyFullStreamIsSent()
    {
        _options = new ScreenShareOptions { ThumbnailHeight = 480, MaxHeight = 480 };
        ScreenShareNode host = Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();
        Share(aryan);

        sumit.Subscribe("aryan", Grid);
        Settle();
        Capture(aryan);

        Assert.Equal("aryan", Assert.Single(host.Host!.StreamIds));
        Assert.NotNull(sumit.GetFrame("aryan"));
    }

    [Fact]
    public void Subscribe_NonPositiveHeight_Throws()
    {
        ScreenShareNode host = Host();

        Assert.Throws<ArgumentOutOfRangeException>(() => host.Subscribe("aryan", 0));
    }

    // how screen data travels: TCP by default, because a UDP datagram can't hold a full frame

    [Fact]
    public void ScreenData_GoesOverTcpByDefault()
    {
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();

        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        List<SentMessage> data = _network.Log.Where(IsData).ToList();
        Assert.NotEmpty(data);
        Assert.All(data, m => Assert.False(m.Lossy));
    }

    [Fact]
    public void BusyScreen_OverTcp_ReachesViewer()
    {
        _busyScreens = true;
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();

        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        // the frame was far bigger than one UDP datagram, and still arrived
        Assert.Contains(_network.Log, m => IsData(m) && m.Bytes.Length > FakeNetwork.MaxDatagramBytes);
        using Bitmap? frame = sumit.GetFrame("aryan");
        Assert.NotNull(frame);
        Assert.Equal((640, 360), (frame.Width, frame.Height));
    }

    [Fact]
    public void BusyScreen_OverUdp_IsLost()
    {
        // why SendDataLossy is off: a full frame doesn't fit in one datagram and is dropped
        _options = new ScreenShareOptions { SendDataLossy = true };
        _busyScreens = true;
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();

        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        Assert.Contains(_network.Dropped, m => m.Lossy);
        Assert.Null(sumit.GetFrame("aryan"));
    }

    [Fact]
    public void SmallFrames_OverUdp_Arrive()
    {
        _options = new ScreenShareOptions { SendDataLossy = true };
        Host();
        ScreenShareNode aryan = Client("aryan");
        ScreenShareNode sumit = Client("sumit");
        Settle();

        Share(aryan);
        sumit.Subscribe("aryan");
        Settle();
        Capture(aryan);

        Assert.All(_network.Log.Where(IsData), m => Assert.True(m.Lossy));
        using Bitmap? frame = sumit.GetFrame("aryan");
        Assert.NotNull(frame);
    }

    private static bool IsData(SentMessage message)
    {
        return message.Module == ScreenShareModule.ModuleId
            && ScreenShareCodec.Decode(message.Bytes, out IScreenShareMessage? decoded)
            && decoded.Kind == ScreenShareMessageKind.Data;
    }

    // screen data can come over the lossy path, which Networking hands only to lossy listeners

    [Fact]
    public void Constructor_ListensForLossyScreenData()
    {
        ScreenShareNode host = Host();
        ScreenShareNode sumit = Client("sumit");

        Assert.True(_connections[host].HasLossyListener(ScreenShareModule.ModuleId));
        Assert.True(_connections[sumit].HasLossyListener(ScreenShareModule.ModuleId));
    }

    [Fact]
    public void Dispose_StopsListeningForLossyScreenData()
    {
        Host();
        ScreenShareNode sumit = Client("sumit");

        sumit.Dispose();

        Assert.False(_connections[sumit].HasLossyListener(ScreenShareModule.ModuleId));
    }

    [Fact]
    public void RunMaintenance_OnClient_DoesNothing()
    {
        Host();
        ScreenShareNode sumit = Client("sumit");

        sumit.RunMaintenance();

        Assert.True(SpinWait.SpinUntil(() => sumit.IsIdle, s_timeout));
    }

    // a connection that has already been closed: unsubscribing fails
    private sealed class ClosedCommunicator : ICommunicator
    {
        private readonly ICommunicator _inner;

        public ClosedCommunicator(ICommunicator inner)
        {
            _inner = inner;
        }

        public int ListenPort => _inner.ListenPort;
        public IPAddress ListenIpAddress => _inner.ListenIpAddress;
        public void AddSubscriber(int module, IMessageListener subscriber)
        {
            _inner.AddSubscriber(module, subscriber);
        }

        public void RemoveSubscriber(int module)
        {
            throw new ObjectDisposedException("communicator");
        }

        public void SendMessage(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        {
            _inner.SendMessage(iPEndPoint, message, module, priority);
        }

        public void BroadcastMessage(byte[] message, int module, int priority)
        {
            _inner.BroadcastMessage(message, module, priority);
        }

        public void JoinMulticastGroup(int multicastGroup, IPEndPoint clientIp)
        {
            throw new NotSupportedException();
        }

        public void LeaveMulticastGroup(int multicastGroup, IPEndPoint clientIp)
        {
            throw new NotSupportedException();
        }

        public void MulticastMessage(int multicastGroup, byte[] message, int module, int priority)
        {
            throw new NotSupportedException();
        }

        public void AddLossySubscriber(int module, IMessageListenerLossy subscriber)
        {
            _inner.AddLossySubscriber(module, subscriber);
        }

        public void RemoveLossySubscriber(int module)
        {
            throw new ObjectDisposedException("communicator");
        }

        public void SendMessageLossy(IPEndPoint iPEndPoint, byte[] message, int module, int priority)
        {
            throw new NotSupportedException();
        }

        public void BroadcastMessageLossy(byte[] message, int module, int priority)
        {
            throw new NotSupportedException();
        }

        public void MulticastMessageLossy(int multicastGroup, byte[] message, int module, int priority)
        {
            throw new NotSupportedException();
        }
    }
}
