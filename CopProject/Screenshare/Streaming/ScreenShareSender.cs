// Author = Sanjaysiva A
// Roll Number = 112301030

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
    private volatile bool _paused;
    private CancellationTokenSource? _loopCancellation;
    private Thread? _loopThread;

    /// <summary>gets the id of the stream this sender sends; the sharer's id unless given</summary>
    public string StreamId { get; }

    /// <summary>
    /// gets or sets whether frames are held back because nobody watches the stream. the first
    /// frame is always sent, since it announces the stream.
    /// </summary>
    public bool Paused
    {
        get => _paused;
        set => _paused = value;
    }

    /// <param name="streamId">the stream's id; defaults to the sharer's id</param>
    public ScreenShareSender(
        Participant local,
        string displayName,
        IImageSource source,
        Action<IScreenShareMessage> send,
        ScreenShareOptions options,
        TimeProvider timeProvider,
        string? streamId = null)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(send);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _local = local;
        StreamId = streamId ?? local.Id;
        _displayName = displayName;
        _source = source;
        _service = new ScreenSharingService(source, new ImageDiffer(options.TileSize));
        _packetizer = new ScreenPacketizer(StreamId, options.FullFrameJpegQuality, options.TileSize);
        _send = send;
        _options = options;
        _timeProvider = timeProvider;
        _lastProgress = timeProvider.GetUtcNow();
        Trace.TraceInformation("[Screenshare] ScreenShareSender.constructor: Initialized sender for stream {0} ({1}).", StreamId, displayName);
    }

    public bool CaptureOnce()
    {
        if (_stopped || (_announced && _paused) || !MaySendFrame())
        {
            return false;
        }

        if (_fullFrameRequested)
        {
            _fullFrameRequested = false;
            Trace.TraceInformation("[Screenshare] ScreenShareSender.CaptureOnce: reset requested for full frame on stream {0}.", StreamId);
            _service.Reset();
        }

        using ImageDiffResult result = _service.ProcessFrame();
        ScreenDataPacket packet = _packetizer.CreatePacket(result, _timeProvider.GetUtcNow().UtcDateTime);

        if (!_announced)
        {
            Trace.TraceInformation("[Screenshare] ScreenShareSender.CaptureOnce: Announcing stream {0} ({1}x{2}) for participant {3}.", StreamId, packet.Width, packet.Height, _local.Id);
            try
            {
                _send(new StreamStarted(StreamId, _local, _displayName, packet.Width, packet.Height));
            }
            catch (Exception ex)
            {
                Trace.TraceError("[Screenshare] ScreenShareSender.CaptureOnce: Failed to send StreamStarted for stream {0}: {1}", StreamId, ex);
                throw;
            }
            _announced = true;
        }

        lock (_flowLock)
        {
            _lastSent = packet.FrameNumber;
        }

        try
        {
            _send(packet);
            Trace.TraceInformation("[Screenshare] ScreenShareSender.CaptureOnce: Sent packet {0} (type: {1}) for stream {2}.", packet.FrameNumber, packet.Type, StreamId);
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenShareSender.CaptureOnce: Failed to send data packet {0} for stream {1}: {2}", packet.FrameNumber, StreamId, ex);
            throw;
        }
        return true;
    }

    private bool MaySendFrame()
    {
        lock (_flowLock)
        {
            if (_lastSent - _lastAcked < _options.MaxFramesInFlight)
            {
                return true;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (now - _lastProgress < _options.AckTimeout)
            {
                Trace.TraceWarning("[Screenshare] ScreenShareSender.MaySendFrame: Flow control limit reached for stream {0} ({1} frames in flight). Holding frame.", StreamId, _lastSent - _lastAcked);
                return false;
            }

            // ack timeout, start over
            Trace.TraceWarning("[Screenshare] ScreenShareSender.MaySendFrame: {0}: No ack for {1}, resending a full frame.", StreamId, _options.AckTimeout);
            _lastAcked = _lastSent;
            _lastProgress = now;
            _fullFrameRequested = true;
            return true;
        }
    }

    private void RunLoop(CancellationToken token)
    {
        TimeSpan interval = TimeSpan.FromSeconds(1.0 / _options.FramesPerSecond);
        var stopwatch = new Stopwatch();

        while (!token.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                CaptureOnce();
            }
            catch (Exception e)
            {
                Trace.TraceError("[Screenshare] ScreenShareSender.RunLoop: {0}: Capture failed: {1}", StreamId, e);
            }

            TimeSpan remaining = interval - stopwatch.Elapsed;
            if (remaining > TimeSpan.Zero)
            {
                token.WaitHandle.WaitOne(remaining);
            }
        }
    }

    public void StartLoop()
    {
        if (_loopThread != null || _stopped)
        {
            Trace.TraceWarning("[Screenshare] ScreenShareSender.StartLoop: Capture loop already running or sender stopped for stream {0}.", StreamId);
            return;
        }

        Trace.TraceInformation("[Screenshare] ScreenShareSender.StartLoop: Starting capture loop thread for stream {0}.", StreamId);
        _loopCancellation = new CancellationTokenSource();
        CancellationToken token = _loopCancellation.Token;

        _loopThread = new Thread(() => RunLoop(token)) {
            IsBackground = true,
            Name = $"ScreenShare capture {StreamId}"
        };
        _loopThread.Start();
    }

    public void OnFrameAck(long frameNumber)
    {
        lock (_flowLock)
        {
            if (frameNumber > _lastAcked && frameNumber <= _lastSent)
            {
                _lastAcked = frameNumber;
                _lastProgress = _timeProvider.GetUtcNow();
                Trace.TraceInformation("[Screenshare] ScreenShareSender.OnFrameAck: Stream {0} updated ack to frame {1}.", StreamId, frameNumber);
            }
            else
            {
                Trace.TraceWarning("[Screenshare] ScreenShareSender.OnFrameAck: Stream {0} received out-of-order or stale ack for frame {1} (lastAcked: {2}, lastSent: {3}).", StreamId, frameNumber, _lastAcked, _lastSent);
            }
        }
    }

    public void OnFullFrameRequested()
    {
        Trace.TraceInformation("[Screenshare] ScreenShareSender.OnFullFrameRequested: Full frame requested for stream {0}.", StreamId);
        _fullFrameRequested = true;
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        Trace.TraceInformation("[Screenshare] ScreenShareSender.Stop: Stopping sender for stream {0}.", StreamId);
        _stopped = true;
        _loopCancellation?.Cancel();

        if (_loopThread != null && Thread.CurrentThread != _loopThread)
        {
            _loopThread.Join();
        }

        if (_announced)
        {
            try
            {
                _send(new StreamStopped(StreamId));
                Trace.TraceInformation("[Screenshare] ScreenShareSender.Stop: Sent StreamStopped for stream {0}.", StreamId);
            }
            catch (Exception ex)
            {
                Trace.TraceError("[Screenshare] ScreenShareSender.Stop: Failed to send StreamStopped for stream {0}: {1}", StreamId, ex);
            }
        }
    }

    public void Dispose()
    {
        Trace.TraceInformation("[Screenshare] ScreenShareSender.Dispose: Disposing sender for stream {0}.", StreamId);
        Stop();
        _loopCancellation?.Dispose();
        _service.Dispose();
        (_source as IDisposable)?.Dispose();
    }
}
