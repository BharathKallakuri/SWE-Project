// Author = Srikar
// Roll Number = 142301013

using System.Net;

namespace Networking.Packets;

/// <summary>
/// Represents a request by a client to leave a multicast group.
/// </summary>
/// <param name="MulticastGroup">The multicast group identifier to leave.</param>
/// <param name="ClientIp">Endpoint of the leaving client.</param>
public record LeaveGroupPacket(int MulticastGroup, IPEndPoint ClientIp) : NetworkMessage;
