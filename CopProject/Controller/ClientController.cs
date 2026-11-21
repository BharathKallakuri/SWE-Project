// K.Bala Krishna
// 112301015

using System.Diagnostics;
using System.Net;
using System.Text;
using Networking;
using Utils;

namespace Controller;

/// <summary>
/// Client-side implementation of IControllerService .
/// Runs on Machine B (or any participant machine). It sends JOIN/LEAVE messages to the remote host and listens for broadcast notifications.
/// </summary>
public sealed class ClientController : IControllerService
{
    private ControllerUser? _currentUser;
    private ICommunicator? _communicator;
    private bool _disposed;
    private string? _hostIp;
    private int _hostPort;

    // For storing the user count and users information
    private int _lastKnownUserCount;
    private readonly Dictionary<string, ControllerUser> _knownUsers = new();
    private readonly List<IUserNotificationHandler> _notificationHandlers = new();
    internal bool InitialSync = false;
    internal readonly ManualResetEventSlim JoinResponseEvent = new(false);
    public string? LastErrorMessage { get; internal set; }
    internal bool IsJoined = false;

    // Events
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<string>? SecurityAlertRaised;
    public event EventHandler<string>? JoinRejected;
    internal void RaiseSecurityAlert(string alert)
    {
        SecurityAlertRaised?.Invoke(this, alert);
    }

    internal void RaiseJoinRejected(string reason)
    {
        JoinRejected?.Invoke(this, reason);
    }

    public int ListenPort => _communicator?.ListenPort ?? 0;
    public ICommunicator? Communicator => _communicator;
    public ControllerUser? CurrentUser => _currentUser;

    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
        if (handler != null && !_notificationHandlers.Contains(handler))
        {
            _notificationHandlers.Add(handler);
        }
    }

    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
        if (handler != null && _notificationHandlers.Contains(handler))
        {
            _notificationHandlers.Remove(handler);
        }
    }

    internal void NotifyHandlersUserJoined(ControllerUser user)
    {
        foreach (IUserNotificationHandler handler in _notificationHandlers)
        {
            handler.OnUserJoined(user);
        }
    }

    internal void NotifyHandlersUserLeft(ControllerUser user)
    {
        foreach (IUserNotificationHandler handler in _notificationHandlers)
        {
            handler.OnUserLeft(user);
        }
    }

    public ClientController(ControllerUser user, ICommunicator? communicator = null)
    {
        _currentUser = user ?? throw new ArgumentNullException(nameof(user));
        _communicator = communicator;
        // Subscribe to incoming notifications from the host if communicator was provided upfront.
        _communicator?.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
    }

    // IControllerService functions implementation. 
    // Not applicable on the client side. Use a HostController to start/end meetings.
    public bool StartMeeting()
    {
        Trace.TraceWarning("[Controller]: StartMeeting called on Client. Only the host can start meetings.");
        return false;
    }

    public bool EndMeeting()
    {
        Trace.TraceWarning("[Controller]: EndMeeting called on Client. Only the host can end meetings.");
        return false;
    }

    // Sends a JOIN message to the host machine after formatting.
    [Obsolete]
    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user)
    {
        if (!IPAddress.TryParse(hostIp, out IPAddress? ipAddress))
        {
            Trace.TraceWarning($"[Controller]: JoinMeeting failed: '{hostIp}' is not a valid IP address.");
            return false;
        }
        var serverEndPoint = new IPEndPoint(ipAddress, hostPort);
        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }

        ICommunicator? comm = CommunicatorFactory.CreateCommunicator(serverEndPoint, isServer: false, isClient: true);
        if (comm == null || comm.ListenPort == 0)
        {
            (comm as IDisposable)?.Dispose();
            Trace.TraceWarning($"[Controller]: failed to connect to host at {hostIp}:{hostPort}");
            return false;
        }
        _communicator = comm;
        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new ClientNotificationListener(this));
        _hostIp = hostIp;
        _hostPort = hostPort;
        _currentUser = user;
        user.IsHost = false;
        user.Ip = _communicator.ListenIpAddress.ToString();
        user.Port = _communicator.ListenPort;
        user.JoinedAt = DateTime.UtcNow;
        lock (_knownUsers)
        {
            _knownUsers[user.UserKey] = user;
        }
        JoinResponseEvent.Reset();
        IsJoined = false;
        LastErrorMessage = null;
        string message = Protocol.FormatJoinRequest(user);
        SendToHost(hostIp, hostPort, message);
        Trace.TraceInformation($"[Controller]: Sent JOIN for user '{user.Username}' to {hostIp}:{hostPort}.");
        // Wait up to 5 second for host response.
        JoinResponseEvent.Wait(TimeSpan.FromMilliseconds(5000));
        if (!IsJoined)
        {
            if (string.IsNullOrEmpty(LastErrorMessage))
            {
                LastErrorMessage = $"Could not connect to meeting at {hostIp}:{hostPort}.";
            }
            return false;
        }
        return true;
    }
    // Sends a LEAVE message to the host the client previously joined.
    public void LeaveMeeting(ControllerUser user)
    {
        if (_hostIp == null)
        {
            Trace.TraceWarning("[Controller]: LeaveMeeting called but no host has been joined yet.");
            return;
        }
        string userKey = _currentUser?.UserKey ?? user.UserKey;
        string message = Protocol.FormatLeaveRequest(userKey);
        SendToHost(_hostIp, _hostPort, message);
        Trace.TraceInformation($"[Controller]: Sent LEAVE for user '{user.Gmail}' to {_hostIp}:{_hostPort}.");

        // Allow background SendingQueueListenerClient thread to flush the LEAVE packet before closing socket.
        Thread.Sleep(450);

        // Clear remembered host and local tracking.
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }

        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }
    }

    private void SendToHost(string hostIp, int hostPort, string message)
    {
        if (_communicator == null)
        {
            Trace.TraceWarning("[Controller]: SendToHost called but communicator is not initialized.");
            return;
        }

        var endPoint = new IPEndPoint(IPAddress.Parse(hostIp), hostPort);
        byte[] data = Encoding.UTF8.GetBytes(message);
        _communicator.SendMessage(endPoint, data, Protocol.ControllerSubscriberId, 1);
    }

    // Indicates whether the client is currently joined to a host.
    public bool IsConnected => _hostIp != null;

    // Returns the user count tracked from host broadcast notifications.
    public int GetActiveUserCount()
    {
        lock (_knownUsers)
        {
            return _knownUsers.Count > 0 ? _knownUsers.Count : _lastKnownUserCount;
        }
    }

    // Returns the  user list, updated from host broadcast notifications.
    public List<ControllerUser> GetActiveUsers()
    {
        lock (_knownUsers)
        {
            return _knownUsers.Values.ToList();
        }
    }
    // Fire events from notification listener .

    internal void RaiseMeetingEnded(MeetingEndedEventArgs e)
    {
        _hostIp = null;
        _hostPort = 0;
        _lastKnownUserCount = 0;
        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }
        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }
        MeetingEnded?.Invoke(this, e);
    }

    internal void RaiseUserJoined(UserJoinedEventArgs e)
    {
        UserJoined?.Invoke(this, e);
    }

    internal void RaiseUserLeft(UserLeftEventArgs e)
    {
        UserLeft?.Invoke(this, e);
    }

    internal void UpdateUserCount(int count)
    {
        _lastKnownUserCount = count;
    }

    internal void SyncUserList(IEnumerable<ControllerUser> users)
    {
        lock (_knownUsers)
        {
            _knownUsers.Clear();
            foreach (ControllerUser user in users)
            {
                _knownUsers[user.UserKey] = user;
            }
        }
    }

    public int GetLastKnownUserCount()
    {
        return _lastKnownUserCount;
    }

    // Cleanup 
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Auto-send LEAVE to host if still connected, so host cleans up this user.
        if (IsJoined && _hostIp != null && _currentUser != null)
        {
            try
            {
                string message = Protocol.FormatLeaveRequest(_currentUser.UserKey);
                SendToHost(_hostIp, _hostPort, message);
                Thread.Sleep(150);
                Trace.TraceInformation($"[Controller]: Dispose: auto-sent LEAVE for '{_currentUser.Username}' to {_hostIp}:{_hostPort}.");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"[Controller]: Dispose: failed to send LEAVE: {ex.Message}");
            }
        }

        if (_communicator != null)
        {
            _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
            if (_communicator is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _communicator = null;
        }

        lock (_knownUsers)
        {
            _knownUsers.Clear();
        }
    }
}

