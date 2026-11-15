// Author = Srikar
// Roll Number = 142301013

using System.Net;

namespace Networking.Packets;

/// <summary>
/// Represents a message to be broadcasted to all connected clients.
/// </summary>
/// <param name="Message">The payload data to broadcast.</param>
/// <param name="Module">Target module identifier.</param>
/// <param name="Priority">Message priority level.</param>
/// <param name="ClientIp">Endpoint of the sender client.</param>
public record BroadcastPacket(byte[] Message, int Module, int Priority, IPEndPoint ClientIp) : NetworkMessage;
