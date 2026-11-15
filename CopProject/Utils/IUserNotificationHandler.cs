// K.Bala Krishna
  // 112301015

namespace Utils;

/// <summary>
/// Interface for other modules to get information about the users inside the meeting.
///  Without depending on the controller module.
/// </summary>
public interface IUserNotificationHandler
{
    // Called when a user joins the meeting, or when loading people already in the meeting.
    void OnUserJoined(ControllerUser user);

    // Called when a user leaves the meeting.
    void OnUserLeft(ControllerUser user);
}
