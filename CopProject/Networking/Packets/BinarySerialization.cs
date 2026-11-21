// Author = Srikar
// Roll Number = 142301013

using System.Diagnostics;
using System.Net;
using System.Text;

namespace Networking.Packets;

/// <summary>
/// Serializes and deserializes <see cref="Packet"/> instances to and from binary data.
/// </summary>
public class BinarySerialization : ISerializer<Packet>
{
    /// <summary>
    /// Serializes a packet into a byte array.
    /// </summary>
    /// <param name="packet">The packet to serialize.</param>
    /// <returns>The serialized byte array, or an empty array if <paramref name="packet"/> is null.</returns>
    public byte[] Serialize(Packet packet)
    {
        if (packet == null)
        {
            Trace.TraceWarning("[Networking] BinarySerialization.Serialize: Packet is null.");
            return Array.Empty<byte>();
        }

        using var memoryStream = new MemoryStream();
        using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
        {
            if (packet.SerializedData == null)
            {
                writer.Write(-1);
            }
            else
            {
                writer.Write(packet.SerializedData.Length);
                writer.Write(packet.SerializedData);
            }

            if (packet.Destination == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);
                writer.Write(packet.Destination.Address.ToString());
                writer.Write(packet.Destination.Port);
            }

            writer.Write(packet.Module);
            writer.Write(packet.Priority);
            writer.Flush();
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Deserializes a byte array into a <see cref="Packet"/>.
    /// </summary>
    /// <param name="data">The raw binary packet data.</param>
    /// <returns>The deserialized packet, or null if deserialization fails.</returns>
    public Packet? Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            Trace.TraceWarning("[Networking] BinarySerialization.Deserialize: Data is null or empty.");
            return null;
        }

        try
        {
            using var memoryStream = new MemoryStream(data);
            using var reader = new BinaryReader(memoryStream, Encoding.UTF8, leaveOpen: false);

            int dataLength = reader.ReadInt32();
            byte[] serializedData;
            if (dataLength < 0)
            {
                serializedData = Array.Empty<byte>();
            }
            else
            {
                serializedData = reader.ReadBytes(dataLength);
                if (serializedData.Length != dataLength)
                {
                    Trace.TraceWarning("[Networking] BinarySerialization.Deserialize: Incomplete payload data.");
                    return null;
                }
            }

            bool hasDestination = reader.ReadBoolean();
            IPEndPoint destination = new IPEndPoint(IPAddress.Any, 0);
            if (hasDestination)
            {
                string ipString = reader.ReadString();
                int port = reader.ReadInt32();
                if (!IPAddress.TryParse(ipString, out IPAddress? ipAddress))
                {
                    Trace.TraceWarning($"[Networking] BinarySerialization.Deserialize: Invalid IP address '{ipString}'.");
                    return null;
                }

                destination = new IPEndPoint(ipAddress, port);
            }

            int module = reader.ReadInt32();
            int priority = reader.ReadInt32();

            return new Packet(serializedData, destination, module, priority);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[Networking] BinarySerialization.Deserialize error: {ex.Message}");
            return null;
        }
    }
}
