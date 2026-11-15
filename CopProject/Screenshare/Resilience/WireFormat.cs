// Authot = Devapriya Pradeep
// Roll Number = 112301006

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ScreenShare;

/// <summary>
/// classifies encoded message without decoding it
/// no need to decode fully to know kind of message
/// </summary>
public static class WireFormat
{
    /// <returns>false if the bytes are not a message of this protocol version</returns>
    public static bool TryPeekKind(byte[]? bytes, out ScreenShareMessageKind kind)
    {
        kind = default;

        if (bytes == null || bytes.Length < 2 || bytes[0] != ScreenShareCodec.ProtocolVersion)
        {
            return false;
        }

        kind = (ScreenShareMessageKind)bytes[1];
        return true;
    }

    /// <returns>false unless the message is a data packet or StreamStopped with a readable stream id</returns>
    public static bool TryPeekStreamId(byte[]? bytes, [NotNullWhen(true)] out string? streamId)
    {
        streamId = null;

        if (!TryPeekKind(bytes, out ScreenShareMessageKind kind) ||
            (kind != ScreenShareMessageKind.Data && kind != ScreenShareMessageKind.StreamStopped) ||
            bytes!.Length < 4)
        {
            return false;
        }

        int length = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(2, 2));
        if (length == 0 || length > ScreenShareCodec.MaxIdBytes || bytes.Length < 4 + length)
        {
            return false;
        }

        streamId = Encoding.UTF8.GetString(bytes, 4, length);
        return true;
    }
}
