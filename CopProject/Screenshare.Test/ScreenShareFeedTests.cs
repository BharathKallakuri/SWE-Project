// Author = Sumit
// Roll Number = 112301032

using System.Collections.Concurrent;
using System.Drawing;
using Screenshare.Tests.Fakes;
using ScreenShare;
using ScreenShare.Models;

namespace Screenshare.Tests;

public class ScreenShareFeedTests : IDisposable
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    private readonly FakeNetwork _network = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly List<ScreenShareNode> _nodes = new();

    private readonly ScreenShareNode _host;
    private readonly ScreenShareNode _aryan;
    private readonly ScreenShareNode _sumit;
    private readonly ScreenShareFeed _feed;   // Sumit's screen-share panel

    // the feed raises events on ScreenShare's thread
    private readonly ConcurrentQueue<(string Id, string Name)> _joined = new();
    private readonly ConcurrentQueue<string> _left = new();
    private readonly ConcurrentQueue<(string Id, ScreenFrame Frame)> _frames = new();

    public ScreenShareFeedTests()
    {
        _host = NewNode(FakeMeetingSession.Host(_network));
        _aryan = NewNode(FakeMeetingSession.Client(_network, "aryan", "Aryan"));
        _sumit = NewNode(FakeMeetingSession.Client(_network, "sumit", "Sumit"));

        _feed = new ScreenShareFeed(_sumit);
        _feed.ParticipantJoined += (id, name) => _joined.Enqueue((id, name));
        _feed.ParticipantLeft += _left.Enqueue;
        _feed.FrameReady += (id, frame) => _frames.Enqueue((id, frame));
        Settle();
    }

    public void Dispose()
    {
        _feed.Dispose();
        foreach (ScreenShareNode node in _nodes)
        {
            node.Dispose();
        }
    }

    private ScreenShareNode NewNode(FakeMeetingSession session)
    {
        ScreenShareNode node = new(session, () => new FakeImageSource(), new ScreenShareOptions(), _clock);
        _nodes.Add(node);
        return node;
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

    private void RequestFeeds(params string[] visible)
    {
        _feed.RequestFeeds(visible.ToDictionary(id => id, _ => 480));
        Settle();
    }

    // a late joiner gets the sharer's next full frame: the sharer captures again
    private void Capture(ScreenShareNode sharer)
    {
        Assert.True(sharer.CaptureOnce());
        Settle();
    }

    // Subscribe messages Sumit sent to the host
    private int SubscribesFromSumit()
    {
        return _network.Log
            .Where(m => m.From.Equals(_sumit.LocalParticipant.Endpoint))
            .Count(m => ScreenShareCodec.Decode(m.Bytes, out IScreenShareMessage? message) && message is Subscribe);
    }

    [Fact]
    public void Constructor_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenShareFeed(null!));
    }

    // who is sharing

    [Fact]
    public void SomeoneStartsSharing_RaisesParticipantJoined()
    {
        Share(_aryan);

        Assert.Equal(("aryan", "Aryan"), Assert.Single(_joined));
    }

    [Fact]
    public void OwnStream_IsNotShownInOwnGrid()
    {
        Share(_sumit);

        Assert.Empty(_joined);
    }

    [Fact]
    public void SomeoneStopsSharing_RaisesParticipantLeft()
    {
        Share(_aryan);

        _aryan.StopSharing();
        Settle();

        Assert.Equal("aryan", Assert.Single(_left));
    }

    [Fact]
    public void OwnStreamStopping_IsNotReported()
    {
        Share(_sumit);

        _sumit.StopSharing();
        Settle();

        Assert.Empty(_left);
    }

    // pictures

    [Fact]
    public void VisibleStream_GetsFramesAsRawPixels()
    {
        Share(_aryan);

        RequestFeeds("aryan");
        Capture(_aryan);

        (string id, ScreenFrame frame) = Assert.Single(_frames);
        Assert.Equal("aryan", id);
        Assert.Equal((64, 48), (frame.Width, frame.Height));
        Assert.Equal(64 * 48 * 4, frame.Pixels.Length);
    }

    [Fact]
    public void Events_WithNobodyListening_AreSkipped()
    {
        using ScreenShareFeed quiet = new(_sumit);
        Share(_aryan);

        quiet.RequestFeeds(new Dictionary<string, int> { ["aryan"] = 480 });
        Settle();
        Capture(_aryan);
        using (Bitmap? picture = _sumit.GetFrame("aryan"))
        {
            // Sumit's node still got the picture; the quiet feed just had nobody to tell
            Assert.NotNull(picture);
        }

        _aryan.StopSharing();
        Settle();

        Assert.Null(_sumit.GetFrame("aryan"));
    }

    [Fact]
    public void StreamThatLeavesView_IsUnsubscribed()
    {
        Share(_aryan);
        RequestFeeds("aryan");

        RequestFeeds();

        Assert.Null(_sumit.GetFrame("aryan"));
        Assert.Empty(_host.Host!.GetViewers("aryan"));
    }

    [Fact]
    public void StreamThatStaysVisible_IsSubscribedOnce()
    {
        Share(_aryan);

        RequestFeeds("aryan");
        RequestFeeds("aryan");

        Assert.Equal(1, SubscribesFromSumit());
    }

    [Fact]
    public void TileChangingSize_IsSubscribedAgainAtNewSize()
    {
        Share(_aryan);
        _feed.RequestFeeds(new Dictionary<string, int> { ["aryan"] = 180 });
        Settle();

        _feed.RequestFeeds(new Dictionary<string, int> { ["aryan"] = 1080 });
        Settle();

        // grid stream first, then the full stream once the tile was zoomed
        List<string> subscribed = _network.Log
            .Where(m => m.From.Equals(_sumit.LocalParticipant.Endpoint))
            .Select(m => ScreenShareCodec.Decode(m.Bytes, out IScreenShareMessage? message) ? message : null)
            .OfType<Subscribe>()
            .Select(s => s.StreamId)
            .ToList();
        Assert.Equal(new[] { "aryan#thumb", "aryan" }, subscribed);
    }

    [Fact]
    public void OwnStreamInRequest_IsNotSubscribed()
    {
        Share(_sumit);

        RequestFeeds("sumit");

        Assert.Equal(0, SubscribesFromSumit());
    }

    [Fact]
    public void RestartedStream_IsSubscribedAgain()
    {
        Share(_aryan);
        RequestFeeds("aryan");
        _aryan.StopSharing();
        Settle();

        Share(_aryan);
        RequestFeeds("aryan");
        Capture(_aryan);

        Assert.Equal(2, SubscribesFromSumit());
        using Bitmap? picture = _sumit.GetFrame("aryan");
        Assert.NotNull(picture);
    }

    [Fact]
    public void RequestFeeds_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _feed.RequestFeeds(null!));
    }

    // sharing own screen

    [Fact]
    public void StartAndStopSharing_ControlTheNode()
    {
        _feed.StartSharing();
        Assert.True(_feed.IsSharing);
        Assert.True(_sumit.IsSharing);

        _feed.StopSharing();
        Assert.False(_feed.IsSharing);
        Assert.False(_sumit.IsSharing);
    }

    // shutting down

    [Fact]
    public void Dispose_StopsPassingOnEvents()
    {
        _feed.Dispose();

        Share(_aryan);

        Assert.Empty(_joined);
    }

    [Fact]
    public void Dispose_IgnoresLaterRequests()
    {
        Share(_aryan);
        _feed.Dispose();

        RequestFeeds("aryan");

        Assert.Equal(0, SubscribesFromSumit());
    }

    [Fact]
    public void Dispose_LeavesNodeRunning()
    {
        _feed.Dispose();
        _feed.Dispose();

        Share(_sumit);
        Assert.True(_sumit.IsSharing);
    }
}
