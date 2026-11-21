// Author : Saideep.
// Roll No : 112301018.
using Utils;

namespace Controller;

///<summary>
/// Base class for all session-related event arguments.
/// </summary>
public class SessionEventArgs : EventArgs
{
}

///<summary>
/// Provides data for the event that is raised when a user joins a session.
/// </summary>
public class UserJoinedEventArgs : SessionEventArgs
{
    public ControllerUser User { get; }
    public int NewCount { get; }

    public UserJoinedEventArgs(ControllerUser user, int newCount)
    {
        User = user;
        NewCount = newCount;
    }
}

///<summary>
/// Provides data for the event that is raised when a user leaves the session.
/// </summary>
public class UserLeftEventArgs : SessionEventArgs
{
    public ControllerUser User { get; }
    public int NewCount { get; }

    public UserLeftEventArgs(ControllerUser user, int newCount)
    {
        User = user;
        NewCount = newCount;
    }
}

///<summary>
/// provides data for the event that is raised when a session ends.
/// </summary>
public class MeetingEndedEventArgs : SessionEventArgs
{
    public ControllerUser Host { get; }

    public MeetingEndedEventArgs(ControllerUser host)
    {
        Host = host;
    }
}
