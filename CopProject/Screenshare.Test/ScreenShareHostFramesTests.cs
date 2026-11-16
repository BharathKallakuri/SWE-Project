// Author = Sumit
// Roll Number = 112301032

using System.Drawing;
using System.Net;
using ScreenShare;
using Screenshare.Tests.Fakes;

namespace Screenshare.Tests;

public class ScreenShareHostFramesTests : IDisposable
{
    private const string StreamId = "aryan-screen";
    private const int Width = 32;
    private const int Height = 24;

    private static readonly Participant s_self = new("host", new IPEndPoint(IPAddress.Loopback, 5000));
    private static readonly Participant s_sharer = new("aryan", new IPEndPoint(IPAddress.Loopback, 5001));
    private static readonly Participant s_sumit = new("sumit", new IPEndPoint(IPAddress.Loopback, 5002));
    private static readonly Participant s_sanjay = new("sanjay", new IPEndPoint(IPAddress.Loopback, 5003));

    // default options: a viewer may owe 5 acks, and times out after 10 seconds
    private readonly ScreenShareOptions _options = new();
    private readonly ManualTimeProvider _clock = new();
    private readonly FakeRelayOutput _output = new();
    private readonly ScreenShareHost _host;

    public ScreenShareHostFramesTests()
    {
        _host = new ScreenShareHost(s_self, _output, _options, _clock);
        Handle(new StreamStarted(StreamId, s_sharer, "Aryan", Width, Height));
        _output.Clear();
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    private void Handle(IScreenShareMessage message)
    {
        _host.Handle(message, ScreenShareCodec.Encode(message));
    }

    private ScreenDataPacket FullFrame(long frameNumber, string streamId = StreamId)
    {
        using Bitmap bitmap = new(Width, Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Teal);
        }

        return new ScreenDataPacket(
            streamId, frameNumber, _clock.GetUtcNow().UtcDateTime, Width, Height,
            DataPacketType.FullFrame, fullFrameData: PngEncoder.Encode(bitmap));
    }

    // "frame n, nothing changed"
    private ScreenDataPacket Pulse(long frameNumber)
    {
        return new ScreenDataPacket(
            StreamId, frameNumber, _clock.GetUtcNow().UtcDateTime, Width, Height, DataPacketType.Pulse);
    }

    private void Subscribe(Participant viewer)
    {
        Handle(new Subscribe(viewer, StreamId, Height));
    }

    private void Ack(Participant viewer, long frameNumber)
    {
        Handle(new FrameAck(viewer, StreamId, frameNumber));
    }

    // frame numbers of the data packets a viewer was sent, in order
    private List<long> FramesSentTo(Participant viewer)
    {
        return _output.SentTo<ScreenDataPacket>(viewer.Endpoint).Select(p => p.FrameNumber).ToList();
    }

    // sends frames from..to as a full frame then pulses, with no acks from viewers
    private void SendFrames(long from, long to)
    {
        for (long n = from; n <= to; n++)
        {
            Handle(n == 0 ? FullFrame(0) : Pulse(n));
        }
    }

    // OnData

    [Fact]
    public void Data_ForUnknownStream_IsIgnored()
    {
        Handle(FullFrame(0, streamId: "nobody"));

        Assert.Empty(_output.Sent);
        Assert.Empty(_output.Broadcasts);
    }

    [Fact]
    public void Data_IsAcknowledgedToSharer()
    {
        Handle(FullFrame(0));

        FrameAck ack = Assert.Single(_output.SentTo<FrameAck>(s_sharer.Endpoint));
        Assert.Equal(s_self, ack.Viewer);
        Assert.Equal(StreamId, ack.StreamId);
        Assert.Equal(0, ack.FrameNumber);
    }

    [Fact]
    public void Data_WithNoViewers_IsOnlyAcknowledged()
    {
        Handle(FullFrame(0));

        Assert.Single(_output.Sent);
    }

    [Fact]
    public void NewViewer_GetsIncomingFullFrameAsIs()
    {
        Subscribe(s_sumit);
        ScreenDataPacket full = FullFrame(0);

        Handle(full);

        Assert.Same(full, Assert.Single(_output.SentTo<ScreenDataPacket>(s_sumit.Endpoint)));
    }

    [Fact]
    public void ViewerWithPicture_GetsLaterPacketsForwarded()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));
        Ack(s_sumit, 0);

        Handle(Pulse(1));
        Handle(Pulse(2));

        Assert.Equal(new long[] { 0, 1, 2 }, FramesSentTo(s_sumit));
    }

    [Fact]
    public void NewViewer_BeforeHostHasPicture_GetsNothing()
    {
        Subscribe(s_sumit);

        // a pulse cannot start a picture, so there is nothing to send Sumit yet
        Handle(Pulse(0));

        Assert.Empty(FramesSentTo(s_sumit));
    }

    // joining a running stream: the host keeps no picture, it asks the sharer for a full frame

    private List<RequestFullFrame> RequestsToSharer()
    {
        return _output.SentTo<RequestFullFrame>(s_sharer.Endpoint);
    }

    [Fact]
    public void Subscribe_ToRunningStream_AsksSharerForFullFrame()
    {
        SendFrames(0, 3);

        Subscribe(s_sumit);

        RequestFullFrame request = Assert.Single(RequestsToSharer());
        Assert.Equal(s_self, request.Viewer);
        Assert.Equal(StreamId, request.StreamId);
        Assert.Empty(FramesSentTo(s_sumit));
    }

    [Fact]
    public void LateJoiner_GetsSharersNextFullFrameThenDeltas()
    {
        SendFrames(0, 3);
        Subscribe(s_sumit);

        Handle(Pulse(4));          // not a full frame yet: still waiting
        ScreenDataPacket full = FullFrame(5);
        Handle(full);
        Ack(s_sumit, 5);
        Handle(Pulse(6));

        Assert.Equal(new long[] { 5, 6 }, FramesSentTo(s_sumit));
        Assert.Same(full, _output.SentTo<ScreenDataPacket>(s_sumit.Endpoint)[0]);
    }

    [Fact]
    public void ViewersJoiningTogether_ShareOneRequest()
    {
        SendFrames(0, 1);

        Subscribe(s_sumit);
        Subscribe(s_sanjay);
        Handle(Pulse(2));

        Assert.Single(RequestsToSharer());
    }

    [Fact]
    public void UnansweredRequest_IsRepeatedAfterInterval()
    {
        SendFrames(0, 1);
        Subscribe(s_sumit);

        _clock.Advance(ScreenShareHost.FullFrameRequestInterval);
        Handle(Pulse(2));

        Assert.Equal(2, RequestsToSharer().Count);
    }

    [Fact]
    public void AnsweredRequest_LetsTheNextViewerAskAtOnce()
    {
        SendFrames(0, 1);
        Subscribe(s_sumit);
        Handle(FullFrame(2));

        Subscribe(s_sanjay);

        Assert.Equal(2, RequestsToSharer().Count);
    }

    [Fact]
    public void Host_NeverDecodesFrames()
    {
        Subscribe(s_sumit);

        // corrupt image data is passed on untouched: the viewer's own decoder deals with it
        var broken = new ScreenDataPacket(StreamId, 0, _clock.GetUtcNow().UtcDateTime, Width, Height,
            DataPacketType.FullFrame, fullFrameData: new byte[] { 1, 2, 3 });
        Handle(broken);

        Assert.Same(broken, Assert.Single(_output.SentTo<ScreenDataPacket>(s_sumit.Endpoint)));
    }

    // flow control: a viewer may owe at most MaxViewerLag acks

    [Fact]
    public void ViewerThatStopsAcking_StopsGettingFrames()
    {
        Subscribe(s_sumit);

        SendFrames(0, 8);

        // frames 0..5 leave Sumit owing 6 acks, which is over the limit of 5
        Assert.Equal(new long[] { 0, 1, 2, 3, 4, 5 }, FramesSentTo(s_sumit));
    }

    [Fact]
    public void LaggingViewer_ThatCatchesUp_GetsOneFullFrameThenDeltas()
    {
        Subscribe(s_sumit);
        SendFrames(0, 6);
        _output.Clear();

        Ack(s_sumit, 5);           // caught up: the host asks the sharer straight away
        Handle(Pulse(7));          // a delta can't restart the picture, so it is skipped
        Handle(FullFrame(8));
        Ack(s_sumit, 8);
        Handle(Pulse(9));

        Assert.Single(RequestsToSharer());
        Assert.Equal(new long[] { 8, 9 }, FramesSentTo(s_sumit));
        Assert.True(_output.SentTo<ScreenDataPacket>(s_sumit.Endpoint)[0].IsFullFrame);
    }

    [Fact]
    public void LaggingViewer_StillBehind_GetsNoFullFrame()
    {
        Subscribe(s_sumit);
        SendFrames(0, 6);

        Handle(FullFrame(7));

        Assert.Equal(new long[] { 0, 1, 2, 3, 4, 5 }, FramesSentTo(s_sumit));
    }

    [Fact]
    public void LaggingViewer_WithFullFrameArriving_GetsItWhenCaughtUp()
    {
        Subscribe(s_sumit);
        SendFrames(0, 6);
        Ack(s_sumit, 5);
        ScreenDataPacket full = FullFrame(7);

        Handle(full);

        Assert.Same(full, _output.SentTo<ScreenDataPacket>(s_sumit.Endpoint).Last());
    }

    // acks

    [Fact]
    public void Ack_ForFrameNotSentYet_IsIgnored()
    {
        Subscribe(s_sumit);
        SendFrames(0, 6);

        Ack(s_sumit, 99);
        Handle(Pulse(7));

        // still behind, so nothing new was sent
        Assert.Equal(6, FramesSentTo(s_sumit).Count);
    }

    [Fact]
    public void Ack_OlderThanLastAck_IsIgnored()
    {
        Subscribe(s_sumit);
        SendFrames(0, 6);
        Ack(s_sumit, 5);
        Ack(s_sumit, 1);

        Handle(FullFrame(7));

        // the older ack did not undo the newer one, so Sumit was caught up and got the frame
        Assert.Equal(7, FramesSentTo(s_sumit).Last());
    }

    [Fact]
    public void Ack_ForUnknownStreamOrViewer_IsIgnored()
    {
        Subscribe(s_sumit);
        SendFrames(0, 1);

        Handle(new FrameAck(s_sumit, "nobody", 1));
        Ack(s_sanjay, 1);

        Assert.Equal(new long[] { 0, 1 }, FramesSentTo(s_sumit));
    }

    // full-frame requests

    [Fact]
    public void FullFrameRequest_IsPassedToSharer_AndAnsweredByItsNextFullFrame()
    {
        Subscribe(s_sumit);
        SendFrames(0, 2);
        Ack(s_sumit, 2);
        _output.Clear();

        Handle(new RequestFullFrame(s_sumit, StreamId));
        Handle(Pulse(3));          // Sumit lost its picture, so a delta is no use to it
        Handle(FullFrame(4));

        Assert.Single(RequestsToSharer());
        Assert.Equal(new long[] { 4 }, FramesSentTo(s_sumit));
    }

    [Fact]
    public void FullFrameRequest_BeforeHostHasPicture_IsAnsweredByNextFullFrame()
    {
        Subscribe(s_sumit);

        Handle(new RequestFullFrame(s_sumit, StreamId));
        Assert.Empty(FramesSentTo(s_sumit));

        ScreenDataPacket full = FullFrame(0);
        Handle(full);
        Assert.Same(full, Assert.Single(_output.SentTo<ScreenDataPacket>(s_sumit.Endpoint)));
    }

    [Fact]
    public void FullFrameRequest_ForUnknownStreamOrViewer_IsIgnored()
    {
        SendFrames(0, 1);
        _output.Clear();

        Handle(new RequestFullFrame(s_sumit, StreamId));
        Handle(new RequestFullFrame(s_sumit, "nobody"));

        Assert.Empty(_output.Sent);
    }

    // telling the sharer who watches, so it can pause a stream nobody watches

    private List<IPEndPoint> LatestSendToList()
    {
        return _output.SentTo<UpdateSendToList>(s_sharer.Endpoint).Last().SendTo.ToList();
    }

    [Fact]
    public void NewStream_SharerIsToldNobodyWatches()
    {
        _output.Clear();

        Handle(new StreamStarted("other", s_sharer, "Aryan", Width, Height));

        UpdateSendToList update = Assert.Single(_output.SentTo<UpdateSendToList>(s_sharer.Endpoint));
        Assert.Equal("other", update.StreamId);
        Assert.Empty(update.SendTo);
    }

    [Fact]
    public void Subscribe_SharerIsToldBeforeFullFrameRequest()
    {
        Subscribe(s_sumit);

        Assert.Equal(new[] { s_sumit.Endpoint }, LatestSendToList());
        List<(IPEndPoint To, IScreenShareMessage Message)> toSharer = _output.Sent.Where(s => s.To.Equals(s_sharer.Endpoint)).ToList();
        Assert.IsType<UpdateSendToList>(toSharer[0].Message);
        Assert.IsType<RequestFullFrame>(toSharer[1].Message);
    }

    [Fact]
    public void Unsubscribe_SharerIsToldNobodyWatches()
    {
        Subscribe(s_sumit);

        Handle(new Unsubscribe(s_sumit, StreamId));

        Assert.Empty(LatestSendToList());
    }

    [Fact]
    public void Unsubscribe_NotWatching_TellsSharerNothing()
    {
        _output.Clear();

        Handle(new Unsubscribe(s_sumit, StreamId));

        Assert.Empty(_output.SentTo<UpdateSendToList>(s_sharer.Endpoint));
    }

    [Fact]
    public void ViewerLeavesMeeting_SharerIsTold()
    {
        Subscribe(s_sumit);
        Subscribe(s_sanjay);

        _host.OnParticipantLeft(s_sumit.Id);

        Assert.Equal(new[] { s_sanjay.Endpoint }, LatestSendToList());
    }

    [Fact]
    public void ViewerTimesOut_SharerIsTold()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));

        _clock.Advance(_options.ViewerTimeout);
        _host.RemoveTimedOutViewers();

        Assert.Empty(LatestSendToList());
    }

    // timeouts

    [Fact]
    public void ViewerOwingAck_PastTimeout_IsRemoved()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));

        _clock.Advance(_options.ViewerTimeout);
        _host.RemoveTimedOutViewers();

        Assert.Empty(_host.GetViewers(StreamId));
    }

    [Fact]
    public void ViewerOwingAck_BeforeTimeout_IsKept()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));

        _clock.Advance(_options.ViewerTimeout - TimeSpan.FromMilliseconds(1));
        _host.RemoveTimedOutViewers();

        Assert.Single(_host.GetViewers(StreamId));
    }

    [Fact]
    public void CaughtUpViewer_IsNeverTimedOut()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));
        Ack(s_sumit, 0);

        _clock.Advance(TimeSpan.FromMinutes(5));
        _host.RemoveTimedOutViewers();

        Assert.Single(_host.GetViewers(StreamId));
    }

    [Fact]
    public void Timeout_RestartsWhenViewerCatchesUp()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));
        _clock.Advance(TimeSpan.FromSeconds(8));
        Ack(s_sumit, 0);

        Handle(Pulse(1));
        _clock.Advance(TimeSpan.FromSeconds(8));
        _host.RemoveTimedOutViewers();

        // 16 seconds in all, but never more than 8 without catching up
        Assert.Single(_host.GetViewers(StreamId));
    }

    [Fact]
    public void Timeout_DoesNotRestartWhileStillOwingAcks()
    {
        Subscribe(s_sumit);
        Handle(FullFrame(0));
        _clock.Advance(TimeSpan.FromSeconds(6));
        Handle(Pulse(1));

        _clock.Advance(TimeSpan.FromSeconds(4));
        _host.RemoveTimedOutViewers();

        Assert.Empty(_host.GetViewers(StreamId));
    }

    [Fact]
    public void TimedOutViewer_OnlyThatViewerIsRemoved()
    {
        Subscribe(s_sumit);
        Subscribe(s_sanjay);
        Handle(FullFrame(0));
        Ack(s_sanjay, 0);

        _clock.Advance(_options.ViewerTimeout);
        _host.RemoveTimedOutViewers();

        Assert.Equal(s_sanjay.Endpoint, Assert.Single(_host.GetViewers(StreamId)));
    }
}
