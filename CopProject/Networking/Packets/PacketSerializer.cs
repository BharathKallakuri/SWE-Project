// Author = Srikar
// Roll Number = 142301013

using System.Buffers.Binary;
using System.Diagnostics;

namespace Networking.Packets;

/// <summary>
/// Encodes and decodes length-prefixed packet frames for stream transmission.
/// </summary>
public class PacketSerializer
{
    private BinarySerialization _packetSerializer = new BinarySerialization();

    /// <summary>
    /// Encodes a packet into a length-prefixed byte array.
    /// </summary>
    /// <param name="packet">The packet to encode.</param>
    /// <returns>A byte array prefixed with a 4-byte big-endian length header.</returns>
    public byte[] PacketToBytes(Packet packet)
    {
        byte[] serializedData = _packetSerializer.Serialize(packet);
        byte[] finalData = new byte[4 + serializedData.Length];
        BinaryPrimitives.WriteInt32BigEndian(finalData, serializedData.Length);
        Buffer.BlockCopy(serializedData, 0, finalData, 4, serializedData.Length);
        return finalData;
    }

    /// <summary>
    /// Decodes a length-prefixed byte array into a <see cref="Packet"/>.
    /// </summary>
    /// <param name="data">The raw framed byte array.</param>
    /// <returns>The deserialized packet, or null if decoding fails.</returns>
    public Packet? BytesToPacket(byte[] data)
    {
        try
        {
            if (data == null || data.Length < 4)
            {
                Trace.TraceWarning("[Networking] PacketSerializer.BytesToPacket: Unable to process data, data is null or fewer than 4 bytes.");
                return null;
            }
            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
            if (data.Length < 4 + payloadLength)
            {
                Trace.TraceWarning("[Networking] PacketSerializer.BytesToPacket: Payload length does not match the length in header.");
            }
            byte[] payload = new byte[payloadLength];
            Buffer.BlockCopy(data, 4, payload, 0, payloadLength);
            return _packetSerializer.Deserialize(payload);
        }
        catch (Exception e)
        {
            Trace.TraceError($"[Networking] PacketSerializer.BytesToPacket: Deserialization error: {e.Message}");
            return null;
        }
        throw new NotImplementedException();
    }
}
