// Author = Srikar
// Roll Number = 142301013

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Packets;

/// <summary>
/// Represents a network transmission packet containing payload and routing metadata.
/// </summary>
/// <param name="SerializedData">The serialized payload data.</param>
/// <param name="Destination">The target IP endpoint.</param>
/// <param name="Module">The destination module identifier.</param>
/// <param name="Priority">The packet priority level.</param>
public record Packet(byte[] SerializedData, IPEndPoint Destination, int Module, int Priority);
