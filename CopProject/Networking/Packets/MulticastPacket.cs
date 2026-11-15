// Author = Srikar
// Roll Number = 142301013

using System.Net;

namespace Networking.Packets;

/// <summary>
/// Represents a message to be multicasted to members of a multicast group.
/// </summary>
/// <param name="MulticastGroup">Target multicast group identifier.</param>
/// <param name="Message">Payload data to send.</param>
/// <param name="Module">Target module identifier.</param>
/// <param name="Priority">Message priority level.</param>
/// <param name="ClientIp">Endpoint of the sender client.</param>
public record MulticastPacket(int MulticastGroup, byte[] Message, int Module, int Priority, IPEndPoint ClientIp) : NetworkMessage;
