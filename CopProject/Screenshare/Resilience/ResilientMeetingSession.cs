using System.Diagnostics;
using System.Net;
using Networking;

namespace ScreenShare;

/// <summary>
/// it wraps around IMeetingSession to add resilience to the host's peer connections and the client's link to the host
/// </summary>
public sealed class ResilientMeetingSession : IMeetingSession, IDisposable
{
    private readonly IMeetingSession _inner;
    private readonly ResilienceOptions _options;
    private readonly HostPeerMonitor? _hostMonitor;
    private readonly ClientLinkMonitor? _clientMonitor;
    private readonly ResilientCommunicator _communicator;
    private readonly ITimer _timer;
    private int _sweeping;
    private bool _disposed;

    /// <param name="inner">the controller's session</param>
    /// <param name="options">detection and recovery tuning. defaults are in ResilienceOptions</param>
    /// <param name="timeProvider">clock for timeouts</param>
    public ResilientMeetingSession(
        IMeetingSession inner,
        ResilienceOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
        _options = options ?? new ResilienceOptions();
        _options.Validate();
        TimeProvider time = timeProvider ?? TimeProvider.System;

        ITrafficObserver observer;
        if (inner.LocalUser.IsHost)
        {
            _hostMonitor = new HostPeerMonitor(new PeerLivenessTracker(_options, time));
            observer = _hostMonitor;
        }
        else
        {
            _clientMonitor = new ClientLinkMonitor(_options, time);
            observer = _clientMonitor;
        }

        _communicator = new ResilientCommunicator(inner.Communicator, observer);

        _inner.ParticipantLeft += OnInnerParticipantLeft;
        _inner.MeetingEnded += OnInnerMeetingEnded;

        _timer = time.CreateTimer(_ => Sweep(), null, _options.SweepInterval, _options.SweepInterval);
    }

    public MeetingParticipant LocalUser => _inner.LocalUser;

    public IPEndPoint HostEndpoint => _inner.HostEndpoint;

    public ICommunicator Communicator => _communicator;

    /// <summary>raised on the host when the controller says a participant left, and when the host decides one vanished</summary>
    public event Action<MeetingParticipant>? ParticipantLeft;

    public event Action? MeetingEnded;

    /// <summary>raised on the host when a peer went silent or its connection failed, before ParticipantLeft</summary>
    public event Action<LostPeer>? PeerLost;

    /// <summary>
    /// client declares false when the host has been silent too long and true once it answers again. 
    /// </summary>
    public event Action<bool>? HostConnectionChanged;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Dispose();
        _inner.ParticipantLeft -= OnInnerParticipantLeft;
        _inner.MeetingEnded -= OnInnerMeetingEnded;
    }

    private void OnInnerParticipantLeft(MeetingParticipant participant)
    {
        ParticipantLeft?.Invoke(participant);
    }

    private void OnInnerMeetingEnded()
    {
        MeetingEnded?.Invoke();
    }

    private void Sweep()
    {
        // in case of slow sweeps
        if (_disposed || Interlocked.Exchange(ref _sweeping, 1) == 1)
        {
            return;
        }

        try
        {
            if (_hostMonitor != null)
            {
                SweepHost(_hostMonitor);
            }
            else if (_clientMonitor != null)
            {
                SweepClient(_clientMonitor);
            }
        }
        catch (Exception e)
        {
            Trace.WriteLine($"[ScreenShare] resilience sweep failed: {e}");
        }
        finally
        {
            Volatile.Write(ref _sweeping, 0);
        }
    }

    private void SweepHost(HostPeerMonitor monitor)
    {
        foreach (LostPeer lost in monitor.Sweep())
        {
            Trace.WriteLine($"[ScreenShare] peer {lost.ParticipantId ?? "?"} at {lost.Endpoint} went silent");
            PeerLost?.Invoke(lost);

            if (lost.ParticipantId != null)
            {
                ParticipantLeft?.Invoke(new MeetingParticipant(lost.ParticipantId, lost.ParticipantId, isHost: false));
            }
        }
    }

    private void SweepClient(ClientLinkMonitor monitor)
    {
        ClientActions actions = monitor.Sweep();

        foreach (IScreenShareMessage message in actions.Outgoing)
        {
            // through the wrapper, so the monitor also sees 
            _communicator.SendMessage(
                HostEndpoint,
                ScreenShareCodec.Encode(message),
                ScreenShareModule.ModuleId,
                ScreenShareModule.PriorityFor(message));
        }

        foreach (string streamId in actions.GoneStreams)
        {
            _communicator.InjectLocal(new StreamStopped(streamId));
        }

        if (actions.Connected is bool connected)
        {
            HostConnectionChanged?.Invoke(connected);
        }
    }
}
