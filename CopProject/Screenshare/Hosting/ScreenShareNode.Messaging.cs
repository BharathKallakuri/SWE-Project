using System.Diagnostics;
using System.Net;
using Screenshare;

namespace ScreenShare;

public sealed partial class ScreenShareNode
{
    /// <summary>
    /// starts sharing this participant's screen
    /// </summary>
    /// <exception cref="ObjectDisposedException">the node was disposed</exception>
    public void StartSharing()
    {
        StartSharing(runCaptureLoop: true);
    }

    /// <summary>
    /// stops sharing; viewers are told the stream ended
    /// </summary>
    public void StopSharing()
    {
        Sharing? sharing;
        lock (_sharingLock)
        {
            sharing = _sharing;
            Volatile.Write(ref _sharing, null);
        }

        sharing?.Dispose();
    }

    /// <param name="runCaptureLoop">false lets tests drive captures with CaptureOnce</param>
    internal void StartSharing(bool runCaptureLoop)
    {
        lock (_sharingLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_sharing != null)
            {
                return;
            }

            // one screen capture feeds both streams; the full stream may reuse it for one frame
            var capture = new SharedScreenCapture(
                _imageSourceFactory(), TimeSpan.FromSeconds(1.0 / _options.FramesPerSecond), _timeProvider);
            string name = _session.LocalUser.DisplayName;

            var full = new ScreenShareSender(
                LocalParticipant, name, capture.View(), SendToHost, _options, _timeProvider);

            ScreenShareSender? thumbnail = HasThumbnails
                ? new ScreenShareSender(
                    LocalParticipant,
                    name,
                    new ScalingImageSource(capture.View(), _options.ThumbnailHeight),
                    SendToHost,
                    _options with { FramesPerSecond = _options.ThumbnailFramesPerSecond, TileSize = _options.ThumbnailTileSize },
                    _timeProvider,
                    StreamLayers.ThumbnailOf(LocalParticipant.Id))
                : null;

            var sharing = new Sharing(full, thumbnail, capture);
            Volatile.Write(ref _sharing, sharing);

            if (runCaptureLoop)
            {
                full.StartLoop();
                thumbnail?.StartLoop();
            }
        }
    }

    /// <summary>
    /// captures and sends one frame of each stream that has viewers
    /// </summary>
    internal bool CaptureOnce()
    {
        Sharing? sharing = Volatile.Read(ref _sharing);
        if (sharing == null)
        {
            return false;
        }

        bool sentFull = sharing.Full.CaptureOnce();
        bool sentThumbnail = sharing.Thumbnail?.CaptureOnce() ?? false;
        return sentFull || sentThumbnail;
    }

    /// <summary>runs the host's timeout check now; for tests</summary>
    internal void RunMaintenance()
    {
        if (_host != null)
        {
            _dispatcher.Post(_host.RemoveTimedOutViewers);
        }
    }

    /// <summary>
    /// sends a message from this participant's sender or receiver to the host's relay.
    /// safe from any thread.
    /// </summary>
    private void SendToHost(IScreenShareMessage message)
    {
        byte[] bytes = ScreenShareCodec.Encode(message);

        if (_host != null)
        {
            _dispatcher.Post(() => _host.Handle(message, bytes));
        }
        else if (message.Kind == ScreenShareMessageKind.Data && _options.SendDataLossy)
        {
            _communicator.SendMessageLossy(_session.HostEndpoint, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
        }
        else
        {
            _communicator.SendMessage(_session.HostEndpoint, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
        }
    }

    /// <summary>
    /// runs on Networking's receive thread, so only queues the work
    /// </summary>
    private void OnNetworkMessage(IScreenShareMessage message, byte[] bytes)
    {
        if (message is ScreenDataPacket packet && packet.StreamId == LocalParticipant.Id)
        {
            return;
        }

        if (_host != null)
        {
            _dispatcher.Post(() => _host.Handle(message, bytes));
        }
        else
        {
            _dispatcher.Post(() => HandleFromHost(message));
        }
    }

    /// <summary>
    /// handles a message the host sent to this participant (or, on the host, to itself)
    /// </summary>
    private void HandleFromHost(IScreenShareMessage message)
    {
        switch (message)
        {
            case FrameAck ack:
                Volatile.Read(ref _sharing)?.SenderOf(ack.StreamId)?.OnFrameAck(ack.FrameNumber);
                break;

            case RequestFullFrame request:
                Volatile.Read(ref _sharing)?.SenderOf(request.StreamId)?.OnFullFrameRequested();
                break;

            case UpdateSendToList update:
                // the host says who watches one of our streams; nobody: stop sending its frames
                ScreenShareSender? sender = Volatile.Read(ref _sharing)?.SenderOf(update.StreamId);
                sender?.Paused = update.SendTo.Count == 0;
                break;

            default:
                _receiver.Handle(message);
                break;
        }
    }

    private sealed class Sharing(ScreenShareSender full, ScreenShareSender? thumbnail, SharedScreenCapture capture) : IDisposable
    {
        public ScreenShareSender Full { get; } = full;

        public ScreenShareSender? Thumbnail { get; } = thumbnail;

        public ScreenShareSender? SenderOf(string streamId)
        {
            return streamId == Full.StreamId ? Full
                : streamId == Thumbnail?.StreamId ? Thumbnail
                : null;
        }

        public void Dispose()
        {
            Full.Dispose();
            Thumbnail?.Dispose();
            capture.Dispose();
        }
    }

    /// <summary>
    /// routes the relay's output: the host's own endpoint is delivered in-process,
    /// everyone else goes through Networking
    /// </summary>
    private sealed class HostOutput : IRealyOutput
    {
        private readonly ScreenShareNode _node;

        public HostOutput(ScreenShareNode node)
        {
            _node = node;
        }

        public void Send(IPEndPoint to, IScreenShareMessage message, byte[] bytes)
        {
            if (to.Equals(_node.LocalParticipant.Endpoint))
            {
                _node._dispatcher.Post(() => _node.HandleFromHost(message));
            }
            else if (message.Kind == ScreenShareMessageKind.Data && _node._options.SendDataLossy)
            {
                _node._communicator.SendMessageLossy(to, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
            }
            else
            {
                _node._communicator.SendMessage(to, bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));
            }
        }

        public void Broadcast(IScreenShareMessage message, byte[] bytes)
        {
            _node._communicator.BroadcastMessage(bytes, ScreenShareModule.ModuleId, ScreenShareModule.PriorityFor(message));

            _node._dispatcher.Post(() => _node.HandleFromHost(message));
        }
    }
}
