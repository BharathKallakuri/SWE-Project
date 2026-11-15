//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.Net;
//using Xunit;
//using ScreenShare;


//namespace Screenshare.Tests;

//public class ScreenShareReceiverTests
//{
//    private static Participant CreateParticipant(string id, int port)
//    {
//        return new Participant(id, IPEndPoint.Parse($"127.0.0.1:{port}"));
//    }

//    [Fact]
//    public void Constructor_NullArguments_ThrowsArgumentNullException()
//    {
//        Participant local = CreateParticipant("sanjay", 5001);
//        Action<IScreenShareMessage> sendToHost = _ => { };
//        TimeProvider timeProvider = new ManualTimeProvider();

//        Assert.Throws<ArgumentNullException>(() =>
//            new ScreenShareReceiver(null!, sendToHost, timeProvider));
//        Assert.Throws<ArgumentNullException>(() =>
//            new ScreenShareReceiver(local, null!, timeProvider));
//        Assert.Throws<ArgumentNullException>(() =>
//            new ScreenShareReceiver(local, sendToHost, null!));
//    }

//    [Fact]
//    public void StreamLifecycle_SubscribeUnsubscribeRestartAndStop_CoversSubscriptionBranches()
//    {
//        Participant local = CreateParticipant("viewer-1", 5001);
//        Participant host = CreateParticipant("host-1", 5000);
//        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
//        ManualTimeProvider timeProvider = new ManualTimeProvider();

//        using ScreenShareReceiver receiver = new ScreenShareReceiver(local, sentMessages.Add, timeProvider);

//        // Unknown stream calls are ignored
//        receiver.Subscribe("unknown", 720);
//        receiver.Unsubscribe("unknown");
//        Assert.Empty(sentMessages);

//        // Start stream before attaching event handlers (covers null-conditional event branches)
//        receiver.Handle(new StreamStarted("stream-1", host, "Host Screen 1", 64, 64));

//        List<string> addedStreams = new List<string>();
//        List<string> removedStreams = new List<string>();
//        receiver.StreamAdded += (id, name) => addedStreams.Add($"{id}:{name}");
//        receiver.StreamRemoved += removedStreams.Add;

//        // Start second stream with event handler attached
//        receiver.Handle(new StreamStarted("stream-2", host, "Host Screen 2", 64, 64));
//        Assert.Single(addedStreams);

//        // Subscribe and verify duplicate Subscribe is ignored
//        receiver.Subscribe("stream-2", 720);
//        receiver.Subscribe("stream-2", 720);
//        Assert.IsType<Subscribe>(Assert.Single(sentMessages));

//        // Restart existing subscribed stream (replaces subscription without re-firing StreamAdded)
//        receiver.Handle(new StreamStarted("stream-2", host, "Host Screen 2 Renamed", 64, 64));
//        Assert.Single(addedStreams);

//        // Unsubscribe and stop streams
//        receiver.Unsubscribe("stream-2");
//        Assert.IsType<Unsubscribe>(sentMessages[^1]);

//        receiver.Handle(new StreamStopped("unknown"));
//        receiver.Handle(new StreamStopped("stream-2"));
//        Assert.Equal("stream-2", Assert.Single(removedStreams));

//        // Unhandled message type falls through cleanly
//        receiver.Handle(new Hello(local));
//    }

//    [Fact]
//    public void HandleDataPackets_AppliesFramesHandlesPulseRequestsFullFrameAndDisposes()
//    {
//        Participant local = CreateParticipant("viewer-1", 5001);
//        Participant host = CreateParticipant("host-1", 5000);
//        List<IScreenShareMessage> sentMessages = new List<IScreenShareMessage>();
//        ManualTimeProvider timeProvider = new ManualTimeProvider();

//        // Generate valid FullFrame (0), Pulse (1), and ChangedTiles (2) packets
//        ControllableImageSource source = new ControllableImageSource();
//        using ScreenSharingService captureService = new ScreenSharingService(source, new ImageDiffer());
//        ScreenPacketizer packetizer = new ScreenPacketizer("stream-1");

//        using ImageDiffResult diff0 = captureService.ProcessFrame();
//        ScreenDataPacket fullFramePacket = packetizer.CreatePacket(diff0, timeProvider.GetUtcNow().UtcDateTime);

//        using ImageDiffResult diff1 = captureService.ProcessFrame();
//        ScreenDataPacket pulsePacket = packetizer.CreatePacket(diff1, timeProvider.GetUtcNow().UtcDateTime);

//        source.ModifyPixel = true;
//        using ImageDiffResult diff2 = captureService.ProcessFrame();
//        ScreenDataPacket changedTilesPacket = packetizer.CreatePacket(diff2, timeProvider.GetUtcNow().UtcDateTime);

//        ScreenShareReceiver receiver = new ScreenShareReceiver(local, sentMessages.Add, timeProvider);

//        // Data before subscribing is ignored and GetFrame returns null
//        receiver.Handle(fullFramePacket);
//        Assert.Null(receiver.GetFrame("stream-1"));

//        receiver.Handle(new StreamStarted("stream-1", host, "Host Screen", 64, 64));
//        receiver.Subscribe("stream-1", 720);
//        sentMessages.Clear();

//        // Advance past ScreenPacketApplier's full-frame request cooldown
//        timeProvider.Advance(TimeSpan.FromSeconds(10));

//        // Receiving ChangedTiles before FullFrame is not applied and triggers RequestFullFrame
//        receiver.Handle(changedTilesPacket);
//        Assert.Contains(sentMessages, m => m is FrameAck);
//        Assert.Contains(sentMessages, m => m is RequestFullFrame);

//        // Apply FullFrame before attaching FrameUpdated handler
//        receiver.Handle(fullFramePacket);

//        List<string> updatedStreams = new List<string>();
//        receiver.FrameUpdated += updatedStreams.Add;

//        // Pulse packet is acknowledged but does not update the frame or raise FrameUpdated
//        receiver.Handle(pulsePacket);
//        Assert.Empty(updatedStreams);

//        // Valid ChangedTiles packet updates the cached frame and raises FrameUpdated
//        receiver.Handle(changedTilesPacket);
//        Assert.Equal("stream-1", Assert.Single(updatedStreams));

//        using Bitmap? currentFrame = receiver.GetFrame("stream-1");
//        Assert.NotNull(currentFrame);

//        // Dispose with an active subscription cleans up subscriptions and frame cache
//        receiver.Dispose();
//    }

//    private sealed class ControllableImageSource : IImageSource
//    {
//        public bool ModifyPixel { get; set; }

//        public Bitmap Capture()
//        {
//            Bitmap bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
//            if (ModifyPixel)
//            {
//                bitmap.SetPixel(0, 0, Color.Red);
//            }

//            return bitmap;
//        }
//    }

//    private sealed class ManualTimeProvider : TimeProvider
//    {
//        private DateTimeOffset _utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

//        public override DateTimeOffset GetUtcNow()
//        {
//            return _utcNow;
//        }

//        public void Advance(TimeSpan delta)
//        {
//            _utcNow = _utcNow.Add(delta);
//        }
//    }
//}
