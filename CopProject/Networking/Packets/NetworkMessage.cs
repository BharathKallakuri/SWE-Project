// Author = Srikar
// Roll Number = 142301013

using System.Net;
using System.Text.Json.Serialization;

namespace Networking.Packets;

/// <summary>
/// Base record for network control and distribution messages.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BroadcastPacket), typeDiscriminator: "broadcast")]
[JsonDerivedType(typeof(MulticastPacket), typeDiscriminator: "multicast")]
[JsonDerivedType(typeof(JoinGroupPacket), typeDiscriminator: "join")]
[JsonDerivedType(typeof(LeaveGroupPacket), typeDiscriminator: "leave")]
public abstract record NetworkMessage;
