// Author = Srikar
// Roll Number = 142301013

using System.Net;

namespace Networking.Packets;

/// <summary>
/// Represents a request by a client to join a multicast group.
/// </summary>
/// <param name="MulticastGroup">The multicast group identifier to join.</param>
/// <param name="ClientIp">Endpoint of the joining client.</param>
public record JoinGroupPacket(int MulticastGroup, IPEndPoint ClientIp) : NetworkMessage;
