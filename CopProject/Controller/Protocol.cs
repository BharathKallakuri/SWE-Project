// K.Bala Krishna
// 112301015

using System.Text.Json;
using Utils;

namespace Controller;

// Defines the message types and formatting rules used for controller communication.
public static class Protocol
{
    public const int ControllerSubscriberId = 1;
    public const string Join = "JOIN";
    public const string Leave = "LEAVE";
    public const string End = "END";

    // Notification headers sent by the host to update connected clients.
    public const string NotifyUserJoined = "USER_JOINED";
    public const string NotifyUserLeft = "USER_LEFT";
    public const string NotifySessionEnded = "SESSION_ENDED";
    public const string NotifySecurityAlert = "SECURITY_ALERT";
    public const string NotifyJoinRejected = "JOIN_REJECTED";
    public const char Separator = '|';

    // Helper methods to serialize requests into the wire format.
    public static string FormatJoinRequest(ControllerUser user)
    {
        return $"{Join}{Separator}{JsonSerializer.Serialize(user)}";
    }

    public static string FormatLeaveRequest(string userKey)
    {
        return $"{Leave}{Separator}{userKey}";
    }

    // A notification sent from host back to participants.
    public static string FormatNotification(string notificationType, string payload)
    {
        return $"{notificationType}{Separator}{payload}";
    }

    // Splits a raw incoming message into its command type, main payload, and any extra segments.
    public static ParsedMessage? Parse(string rawMessage)
    {
        if (string.IsNullOrEmpty(rawMessage))
        {
            return null;
        }

        string[] parts = rawMessage.Split(Separator);
        if (parts.Length < 2)
        {
            return null;
        }

        return new ParsedMessage(
            type: parts[0],
            userId: parts[1],
            extra: parts.Length > 2 ? parts[2..] : Array.Empty<string>()
        );
    }
}

// Represents a parsed Controller protocol message.
public sealed class ParsedMessage
{
    public string Type { get; }
    public string UserId { get; }
    public string Payload => UserId;
    public string[] Extra { get; }

    public ParsedMessage(string type, string userId, string[] extra)
    {
        Type = type;
        UserId = userId;
        Extra = extra;
    }
}
