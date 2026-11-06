using System.Diagnostics;

namespace ScreenShare;

internal sealed class ScreenShareSender : IDisposable
{
    private readonly Participant _local;
    private readonly string _displayName;
    private readonly IImageSource _source;
    private readonly ScreenSharingService _service;
    private readonly ScreenPacketizer _packetizer;
    private readonly Action<IScreenShareMessage> _send;
    private readonly ScreenShareOptions _options;
    private readonly TimeProvider _timeProvider;

    private readonly object _flowLock = new();
    private long _lastSent = -1;
    private long _lastAcked = -1;
    private DateTimeOffset _lastProgress;

    private volatile bool _fullFrameRequested;
    private bool _announced;
    private volatile bool _stopped;
    private CancellationTokenSource? _loopCancellation;
    private Thread? _loopThread;
    public string StreamId => _local.Id;

    public ScreenShareSender(
        Participant local,
        string displayName,
        IImageSource source,
        Action<IScreenShareMessage> send,
        ScreenShareOptions options,
        TimeProvider timeProvider)
    {
        _local = local;
        _displayName = displayName;
        _source = source;
        _service = new ScreenSharingService(source, new ImageDiffer());
        _packetizer = new ScreenPacketizer(local.Id);
        _send = send;
        _options = options;
        _timeProvider = timeProvider;
        _lastProgress = timeProvider.GetUtcNow();
    }
    public void Dispose()
    {

    }
}
