// Author = Simeon Kandan Sonar
// Roll Number = 112301031

namespace Cloud.Networking;

/// <summary>
/// Connection state exposed by <see cref="IMessenger"/>.
/// Mirrors SignalR's <see cref="HubConnectionState"/> 
/// so that consumers do not need to refer to the SignalR client's HubConnectionState.
/// </summary>
public enum ConnectionState
{
    /// <summary>Not connected and the default value.</summary>
    Disconnected = 0,

    /// <summary>A connection attempt is in progress.</summary>
    Connecting,

    /// <summary>Connected to the server.</summary>
    Connected,

    /// <summary>The connection was lost and the client is trying to reconnect.</summary>
    Reconnecting
}
