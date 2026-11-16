// Author : Saideep.
// Roll no : 112301018.
using Utils;

namespace Controller;

///<summary>
/// Tracks the single meeting session and its participants.
/// Since each session is uniquely identified by its host IP:port pair,
/// only one session is tracked at a time.
/// </summary>

public class SessionManager
{
    private readonly object _lock = new();
    private SessionInfo? _session;

    ///<summary>
    ///Raised when a session ends.
    /// </summary>
    public event EventHandler<MeetingEndedEventArgs>? MeetingEnded;

    ///<summary>
    ///Raised when a user joins the session.
    /// </summary>
    public event EventHandler<UserJoinedEventArgs>? UserJoined;

    ///<summary>
    ///Raised when a user leaves the session.
    /// </summary>
    public event EventHandler<UserLeftEventArgs>? UserLeft;

    ///<summary>
    /// Occurs when a user attempts to join a session when they are already in session.
    ///</summary>
    public event EventHandler<ControllerUser>? DuplicateUserDetected;

    ///<summary>
    /// If a session already exists with host it returns null.
    /// Otherwise,Creates a new session with the given host.
    /// </summary>
    public SessionInfo? CreateSession(ControllerUser hostUser)
    {
        lock (_lock)
        {
            if (_session != null)
            {
                return null;
            }

            var session = new SessionInfo(hostUser);
            _session = session;
            return session;
        }
    }

    ///<summary>
    /// Starts the meeting (transitions from Created -> Active).
    /// </summary>
    public bool StartSession(ControllerUser hostUser)
    {
        SessionInfo? session;
        lock (_lock)
        {
            session = _session;
            if (session == null)
            {
                return false;
            }

            if (!session.IsHost(hostUser.UserKey))
            {
                return false;
            }

            if (!session.Start())
            {
                return false;
            }
        }

        UserJoined?.Invoke(this, new UserJoinedEventArgs(session.HostUser, 1));
        return true;
    }

    ///<summary>
    /// Ends the meeting. Only the host can end it.
    /// </summary>
    public bool EndSession(string requestingUserId)
    {
        SessionInfo? session;
        lock (_lock)
        {
            session = _session;
            if (session == null)
            {
                return false;
            }

            if (!session.IsHost(requestingUserId))
            {
                return false;
            }

            if (!session.End())
            {
                return false;
            }

            _session = null;
        }

        MeetingEnded?.Invoke(this, new MeetingEndedEventArgs(session.HostUser));
        return true;
    }

    ///<summary>
    /// Adds a user to the active session.
    /// </summary>
    public bool JoinSession(ControllerUser user)
    {
        int newCount;
        bool isDuplicate = false ;

        lock (_lock)
        {
            if (_session == null)
            {
                return false;
            }

            if (_session.ContainsUser(user.UserKey))
            {
                isDuplicate = true ;
            }

            else if (!_session.AddUser(user))
            {
                return false;
            }
            newCount = _session.UserCount;
        }
        
        if(isDuplicate)
        {
            DuplicateUserDetected?.Invoke(this, user);
            return false;
        }

        UserJoined?.Invoke(this, new UserJoinedEventArgs(user, newCount));
        return true;
    }

    ///<summary>
    /// Removes a user from the session.
    /// </summary>
    public bool LeaveSession(string userKey)
    {
        int newCount;
        ControllerUser? removedUser;
        lock (_lock)
        {
            if (_session == null)
            {
                return false;
            }

            if (!_session.RemoveUser(userKey, out removedUser) || removedUser == null)
            {
                return false;
            }

            newCount = _session.UserCount;
        }

        UserLeft?.Invoke(this, new UserLeftEventArgs(removedUser, newCount));
        return true;
    }

    ///<summary>
    ///Gets the active user count for the session.
    /// </summary>
    public int GetActiveUserCount()
    {
        lock (_lock)
        {
            if (!HasActiveSession())
            {
                return 0;
            } 
            return _session?.UserCount ?? 0;
        }
    }

    ///<summary>
    ///Gets the user IDs of all participants in the session.
    /// </summary>
    public List<ControllerUser> GetActiveUsers()
    {
        lock (_lock)
        {
            if (!HasActiveSession())
            {
                return new List<ControllerUser>();
            } 
            return _session?.GetUsers() ?? new List<ControllerUser>();
        }
    }

    ///<summary>
    ///Gets the session info, or null if no session exists.
    /// </summary>
    public SessionInfo? GetSession()
    {
        lock (_lock)
        {
            return _session;
        }
    }

    ///<summary>
    ///true if there is an active (non-ended) session.
    /// </summary>
    public bool HasActiveSession()
    {
        lock (_lock)
        {
            return _session != null && _session.State != SessionState.Ended;
        }
    }

    ///<summary>
    /// Clears the current session reference, allowing a new session to be created.
    /// </summary>
    public void ClearSession()
    {
        lock (_lock)
        {
            _session = null;
        }
    }
}
