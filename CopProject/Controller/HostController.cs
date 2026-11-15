// Author : Saideep.
// Roll No : 112301018.
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Networking;
using Utils;

namespace Controller;

///<summary>
/// Host-side implementation of IControllerService.
/// It runs on the host machine that creates and owns meetings.
///</summary>

public sealed class HostController : IControllerService
{
    private readonly ControllerUser _hostUser;
    private readonly ICommunicator _communicator;
    private readonly SessionManager _sessionManager;
    private bool _disposed;
    private readonly List<IUserNotificationHandler> _notificationHandlers = new();

    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    public event EventHandler<UserJoinedEventArgs>? UserJoined;
    public event EventHandler<UserLeftEventArgs>? UserLeft;
    public event EventHandler<string>? SecurityAlertRaised;

    public int ListenPort => _communicator.ListenPort;
    public ICommunicator? Communicator => _communicator;
    public ControllerUser? CurrentUser => _hostUser;
    public bool IsMeetingActive => _sessionManager.HasActiveSession();

    public SessionManager SessionManager => _sessionManager;

    ///<summary>
    /// It registers a handler to receive user join and leave notifications.
    ///</summary>
    public void RegisterNotificationHandler(IUserNotificationHandler handler)
    {
        if(handler != null && !_notificationHandlers.Contains(handler))
        {
            _notificationHandlers.Add(handler);
        }
        
    }

    ///<summary>
    /// Unregisters a previously registered user notification handler.
    ///</summary>
    public void UnregisterNotificationHandler(IUserNotificationHandler handler)
    {
        if(handler != null && _notificationHandlers.Contains(handler))
        {
            _notificationHandlers.Remove(handler);
        }
    }

    public HostController(ControllerUser hostUser, ICommunicator communicator)
    {
        _hostUser = hostUser ?? throw new ArgumentNullException(nameof(hostUser));
        _communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
        _sessionManager = new SessionManager();
        _hostUser.IsHost = true;
        _hostUser.Ip = NetworkUtils.GetLocalIpAddress();
        _hostUser.Port = _communicator.ListenPort;
        _communicator.AddSubscriber(Protocol.ControllerSubscriberId, new SessionMessageListener(_sessionManager));
        _sessionManager.UserJoined += OnUserJoined;
        _sessionManager.UserLeft += OnUserLeft;
        _sessionManager.MeetingEnded += OnMeetingEnded;
        _sessionManager.DuplicateUserDetected += OnDuplicateUserDetected;

    }

    ///<summary>
    /// Creates and starts a new meeting. The host user is added as the first participant.
    ///</summary>
    public bool StartMeeting()
    {
        SessionInfo? session = _sessionManager.CreateSession(_hostUser);
        if (session == null)
        {
            Trace.TraceWarning("StartMeeting failed: a session already exists.");
            return false;
        }

        bool started = _sessionManager.StartSession(_hostUser);
        if (!started)
        {
            Trace.TraceWarning("StartMeeting failed: could not transition session to Active.");
            return false;
        }
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserJoined(_hostUser);
        }

        Trace.TraceInformation($"Meeting started by host '{_hostUser.Username}'.");
        return true;
    }

    ///<summary>
    /// Ends the meeting and broadcasts a session-ended notification to all participants.
    ///</summary>
    public bool EndMeeting()
    {
        bool ended = _sessionManager.EndSession(_hostUser.UserKey);
        if (!ended)
        {
            Trace.TraceWarning("EndMeeting failed.");
            return false;
        }
        
        // Broadcast end notification to all participants
        string payload = JsonSerializer.Serialize(_hostUser);
        Broadcast(Protocol.FormatNotification(Protocol.NotifySessionEnded, payload));
        Trace.TraceInformation($"Meeting ended by host '{_hostUser.Username}'.");
        return true;
    }

    ///<summary>
    /// It is not applicable on host side . So, we return false with a trace warning.
    ///</summary>
    public bool JoinMeeting(string hostIp, int hostPort, ControllerUser user)
    {
        Trace.TraceWarning("JoinMeeting called on HostController. Use a ClientController to join remote meetings.");
        return false;
    }   

    ///<summary>
    /// Not applicable on host side. Host can only end meeting.
    ///</summary>
    public void LeaveMeeting(ControllerUser user)
    {
        Trace.TraceWarning("LeaveMeeting called on HostController. Use a ClientController to leave remote meetings.");
    }

    ///<summary>
    /// Gets the active user count of a session.
    ///</summary>
    public int GetActiveUserCount() => _sessionManager.GetActiveUserCount();

    ///<summary>
    /// Gets the list of active users in the session.
    ///</summary>
    public List<ControllerUser> GetActiveUsers() => _sessionManager.GetActiveUsers();

    private void Broadcast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        _communicator.BroadcastMessage(data, Protocol.ControllerSubscriberId, 1);
    }

    ///<summary>
    /// Broadcasts the updated participant list and notifying registered handlers.
    ///</summary>
    private void OnUserJoined(object? sender, UserJoinedEventArgs e)
    {
        UserJoined?.Invoke(this, e);
        List<ControllerUser> allUsers = _sessionManager.GetActiveUsers();
        string payload = $"{JsonSerializer.Serialize(e.User)}{Protocol.Separator}{JsonSerializer.Serialize(allUsers)}";
        Broadcast(Protocol.FormatNotification(Protocol.NotifyUserJoined, payload));
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserJoined(e.User);
        }
    }

    ///<summary>
    ///It also the does the same work like broadcasts the updated participant list.
    ///</summary>
    private void OnUserLeft(object? sender, UserLeftEventArgs e)
    {
        UserLeft?.Invoke(this, e);

        List<ControllerUser> allUsers = _sessionManager.GetActiveUsers();
        string payload = $"{JsonSerializer.Serialize(e.User)}{Protocol.Separator}{JsonSerializer.Serialize(allUsers)}";
        Broadcast(Protocol.FormatNotification(Protocol.NotifyUserLeft, payload));
        foreach(var handler in _notificationHandlers)
        {
            handler.OnUserLeft(e.User);
        }
    }

    ///<summary>
    /// Handles the event by forwarding it to subscribers.
    ///</summary>
    private void OnMeetingEnded(object? sender, MeetingEndedEventArgs e)
    {
        MeetingEnded?.Invoke(this, e);
    }

    private void OnDuplicateUserDetected(object? sender, ControllerUser user)
    {
        string alert = $"Duplicate join rejected: '{user.Gmail}' attempted to join from {user.Ip} while already in meeting.";
        Trace.TraceWarning(alert);
        SecurityAlertRaised?.Invoke(this, alert);
        string rejectMsg = Protocol.FormatNotification(Protocol.NotifyJoinRejected, $"User with email '{user.Gmail}' is already in this meeting on another device.");
        Broadcast(rejectMsg);
    }

    ///<summary>
    /// Releases all the resources used by this file and unsubscribe from events.
    ///</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sessionManager.UserJoined -= OnUserJoined;
        _sessionManager.UserLeft -= OnUserLeft;
        _sessionManager.MeetingEnded -= OnMeetingEnded;
        _sessionManager.DuplicateUserDetected -= OnDuplicateUserDetected;

        _communicator.RemoveSubscriber(Protocol.ControllerSubscriberId);
        Thread.Sleep(150);
        (_communicator as IDisposable)?.Dispose();
    }
}
