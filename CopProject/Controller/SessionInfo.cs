// Author : Saideep.
// Roll No : 112301018.
using Utils;

namespace Controller;

/// <summary>
/// It Holds all metadata about a single meeting session, including its lifecycle.
/// state, the host who created it, and all current participants(ControllerUser's).
/// </summary>
public sealed class SessionInfo
{
    private readonly Dictionary<string, ControllerUser> _users = new();
    private readonly object _lock = new();

    public ControllerUser HostUser { get; }
    // public DateTime CreatedAt { get; }
    public SessionState State { get; private set; }

    public SessionInfo(ControllerUser hostUser)
    {
        HostUser = hostUser ?? throw new ArgumentNullException(nameof(hostUser));
        // CreatedAt = DateTime.UtcNow;
        State = SessionState.Created;
        _users[hostUser.UserKey] = hostUser;
    }

    ///<summary>
    ///Transitions the session to the Active state.
    ///True if the transition was valid (from Created), false otherwise.
    /// </summary>
    public bool Start()
    {
        lock (_lock)
        {
            if (State != SessionState.Created)
            {
                return false;
            }
            State = SessionState.Active;
            return true;
        }
    }

    ///<summary>
    ///Transitions the session to the Ended state.
    ///True if the transition was valid , false otherwise.
    /// </summary>
    public bool End()
    {
        lock (_lock)
        {
            if (State == SessionState.Ended)
            {
                return false;
            } 
            State = SessionState.Ended;
            return true;
        }
    }

    ///<summary>
    ///Adds a participant with their network address.
    ///True if the user was added
    /// </summary>
    public bool AddUser(ControllerUser user)
    {
        lock (_lock)
        {
            if (State == SessionState.Ended)
            {
                return false;
            }
            if (_users.ContainsKey(user.UserKey))
            {
                return false;
            }
            _users[user.UserKey] = user;
            return true;
        }
    }

    ///<summary>
    /// It determines whether the specified user is currently active in the session.
    /// </summary>
    public bool ContainsUser(string userKey)
    {
        lock (_lock)
        {
            return _users.ContainsKey(userKey);
        }
    }

    ///<summary>
    ///Removes a participant.
    ///True if the user was found and removed.
    /// </summary>
    public bool RemoveUser(string userKey, out ControllerUser? removedUser)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(userKey, out removedUser))
            {
                return _users.Remove(userKey);
            }
            removedUser = null;
            return false;
        }
    }

    ///<summary>
    ///Gets the current number of participants.
    /// </summary>
    public int UserCount
    {
        get
        {
            lock (_lock)
            {
                return _users.Count;
            }
        }
    }

    ///<summary>
    ///Gets a snapshot of all current participant user IDs.
    /// </summary>
    public List<ControllerUser> GetUsers()
    {
        lock (_lock)
        {
            return _users.Values.ToList();
        }
    }

    ///<summary>
    ///Checks whether the given user is the host of this session.
    ///</summary>
    public bool IsHost(string userKey)
    {
        return HostUser.UserKey== userKey;
    }
}
