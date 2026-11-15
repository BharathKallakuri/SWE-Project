// Author : Saideep.
// Roll no : 112301018.
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Networking;
using Utils;

namespace Controller;

/// <summary> 
/// Receives messages from the Networking module and updates the SessionManager accordingly.
/// </summary>

public class SessionMessageListener : IMessageListener
{
    private readonly SessionManager _sessionManager;

    public SessionMessageListener(SessionManager sessionManager)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <summary>
    /// It processes the raw message bytes received from the network transport layer.
    /// </summary>
    /// <param name = "message">The raw encoded message payload.</param>
    public void OnMessageReceived(byte[] message)
    {
        if (message == null || message.Length == 0)
        {
            Trace.TraceWarning($"Controller received empty message.");
            return;
        }

        string rawMessage = Encoding.UTF8.GetString(message);
        if (string.IsNullOrEmpty(rawMessage))
        {
            Trace.TraceWarning($"Controller received empty message.");
            return;
        }

        string[] parts = rawMessage.Split(Protocol.Separator, 2);
        if (parts.Length < 2)
        {
            Trace.TraceWarning($"Controller received malformed message: {rawMessage}");
            return;
        }

        string type = parts[0];

        switch (type)
        {
            case Protocol.Join:
                HandleJoin(parts[1]);
                break;

            case Protocol.Leave:
                HandleLeave(parts[1]);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Handles JOIN: deserializes the ControllerUser from JSON.
    /// Later, It sends the message to SessionManager to check whether join is successful or not.
    /// </summary>
    /// <param name = "userJson"> It is the JSON payload representing the user.</param>
    private void HandleJoin(string userJson)
    {
        if (!_sessionManager.HasActiveSession())
        {
            Trace.TraceWarning("Join failed: no active session on this host.");
            return;
        }
        try
        {
            ControllerUser? user = JsonSerializer.Deserialize<ControllerUser>(userJson);
            if (user == null)
            {
                Trace.TraceWarning("Join failed: malformed user payload.");
                return;
            }
            user.IsHost = false; // Joined users are clients/participants
            bool joined = _sessionManager.JoinSession(user);
            if (joined)
            {
                Trace.TraceInformation($"{user.Username} joined session from {user.Ip}:{user.Port}");
            }
            else
            {
                Trace.TraceWarning($"Join failed for {user.Username} (could not add to session)");
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Failed to process JOIN payload: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles LEAVE messages.Format: "LEAVE:userId"
    /// </summary> 
    ///<param name = "userKey"> It is the key representing the user leaving the session.</param>
    private void HandleLeave(string userKey)
    {
        if (!_sessionManager.HasActiveSession())
        {
            Trace.TraceWarning($"Leave failed for {userKey}: no active session on this host.");
            return;
        }

        bool left = _sessionManager.LeaveSession(userKey);

        if (left)
        {
            Trace.TraceInformation($"{userKey} left session");
        }
        else
        {
            Trace.TraceWarning($"Leave failed for {userKey}");
        }
    }
}