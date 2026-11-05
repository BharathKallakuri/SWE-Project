using System.Drawing;
using ScreenShare;

namespace Screenshare;

internal sealed class ScreenShareReceiver : IDisposable
{
    private readonly Participant _local;
    private readonly Action<IScreenShareMessage> _sendToHost;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, string> _streamNames = new();
    private readonly Dictionary<string, Subscription> _subscriptions = new();
    private readonly StreamFrameCache _frames = new();

    public ScreenShareReceiver(
        Participant local,
        Action<IScreenShareMessage> sendToHost,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(sendToHost);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        _sendToHost = sendToHost;
        _timeProvider = timeProvider;
    }

    public void Dispose()
    {

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
