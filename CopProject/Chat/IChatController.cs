using System.Net;

namespace Chat;

/// <summary>
/// Supplies chat participant-related events and their network endpoints.
/// </summary>
public interface IChatController
{
    /// <summary>
    /// Raised when a participant joins the meeting.
    /// </summary>
    event Action<string>? ParticipantAdded;

    /// <summary>
    /// Raised when a participant leaves the meeting.
    /// </summary>
    event Action<string>? ParticipantRemoved;

    /// <summary>
    /// Gets the network endpoint associated with a participant username.
    /// </summary>
    /// <param name="username">The participant username.</param>
    /// <returns>The participant's IP address and port.</returns>
    IPEndPoint GetParticipantEndPoint(string username);
}
