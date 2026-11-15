// K.Bala Krishna
// 112301015

using Utils;
using Networking;

namespace Controller;

///<summary>
/// Defines the core contract for meeting control, participant tracking, and event handling.
/// The UX layer depends on this abstraction, not on concrete Host/Client classes.
///</summary>

public interface IControllerService : IDisposable
{
    // Event triggered when a user joins a session.
    event EventHandler<UserJoinedEventArgs>? UserJoined;
    // Event triggered when a user leaves a session.
    event EventHandler<UserLeftEventArgs>? UserLeft;
    // Event triggered when the Meeting is Ended. 
    event EventHandler<MeetingEndedEventArgs>? MeetingEnded;
    // Returns True if the meeting was created and started successfully.
    bool StartMeeting();

    // Returns True if the meeting was ended successfully.
    bool EndMeeting();

    // Joins an existing meeting on a remote host.
    bool JoinMeeting(string hostIp, int hostPort, ControllerUser user);

    // Leaves a meeting that was previously joined.
    void LeaveMeeting(ControllerUser user);

    // Gets the number of active participants in the session.
    int GetActiveUserCount();

    // Gets the user information of all active participants in the session.
    List<ControllerUser> GetActiveUsers();

    // Gets the port this controller is listening on.
    int ListenPort { get; }
    
    // Meeting connection and current user identity (for ScreenShare and other modules).
    ICommunicator? Communicator { get; }
    ControllerUser? CurrentUser { get; }
    
    // Registration for external module notification handlers.
    void RegisterNotificationHandler(IUserNotificationHandler handler);
    void UnregisterNotificationHandler(IUserNotificationHandler handler);
}
