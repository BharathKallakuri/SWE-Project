using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace ScreenShare;

/// <summary>
/// converts ScreenShare messages to and from bytes for Networking.
/// </summary>

public static class ScreenShareCodec
{
    /// <summary>
    /// version of protocol written in message header
    /// </summary>
    public const byte ProtocolVersion = 1;

    public const int MaxIdBytes = 128;

    public const int MaxDisplayNameBytes = 256;

    public const int MaxEndpointBytes = 64;

    public const int MaxFrameDimension = 16384;

    /// <summary>
    /// Image data allowed in one packet
    /// </summary>
    public const int MaxImageBytes = 32 * 1024 * 1024;

    /// <summary>
    /// endpoints allowed in a send-to list
    /// </summary>
    public const int MaxSendToEntries = 1024;

    private const int HeaderLength = 2;

    /// <summary>
    /// to weed off invalid UTF-8
    /// </summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);


    // summary here
    public static byte[] Encode(IScreenShareMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(ProtocolVersion);
        writer.Write((byte)message.Kind);

        // encode by type of message
        switch (message)
        {
            case ScreenDataPacket packet:
                WriteDataPacket(writer, packet);
                break;

            case StreamStarted started:
                WriteString(writer, started.StreamId, MaxIdBytes);
                WriteParticipant(writer, started.Sharer);
                WriteString(writer, started.DisplayName, MaxDisplayNameBytes);
                WriteDimension(writer, started.Width);
                WriteDimension(writer, started.Height);
                break;

            case StreamStopped stopped:
                WriteString(writer, stopped.StreamId, MaxIdBytes);
                break;

            case Subscribe subscribe:
                WriteParticipant(writer, subscribe.Viewer);
                WriteString(writer, subscribe.StreamId, MaxIdBytes);
                WriteDimension(writer, subscribe.MaxHeight);
                break;

            case Unsubscribe unsubscribe:
                WriteParticipant(writer, unsubscribe.Viewer);
                WriteString(writer, unsubscribe.StreamId, MaxIdBytes);
                break;

            case RequestFullFrame request:
                WriteParticipant(writer, request.Viewer);
                WriteString(writer, request.StreamId, MaxIdBytes);
                break;

            case FrameAck ack:
                WriteParticipant(writer, ack.Viewer);
                WriteString(writer, ack.StreamId, MaxIdBytes);
                writer.Write(ack.FrameNumber);
                break;

            case UpdateSendToList update:
                WriteString(writer, update.StreamId, MaxIdBytes);
                if (update.SendTo.Count > MaxSendToEntries)
                {
                    throw new ArgumentException(
                        $"send-to list cannot exceed {MaxSendToEntries} entries", nameof(message));
                }
                writer.Write(update.SendTo.Count);
                foreach (IPEndPoint endpoint in update.SendTo)
                {
                    WriteString(writer, endpoint.ToString(), MaxEndpointBytes);
                }
                break;

            default:
                throw new ArgumentException(
                    $"unsupported message type {message.GetType().Name}", nameof(message));
        }

        writer.Flush();
        return stream.ToArray();
    }


    // summary here
    public static bool Decode(byte[]? data, [NotNullWhen(true)] out IScreenShareMessage? message)
    {
        message = null;

        if (data == null || data.Length < HeaderLength)
        {
            return false;
        }

        try
        {
            var reader = new Reader(data);

            if (reader.ReadByte() != ProtocolVersion)
            {
                return false;
            }

            var kind = (ScreenShareMessageKind)reader.ReadByte();

            IScreenShareMessage? decoded = kind switch
            {
                ScreenShareMessageKind.Data => ReadDataPacket(reader),
                ScreenShareMessageKind.StreamStarted => new StreamStarted(
                    reader.ReadString(MaxIdBytes),
                    ReadParticipant(reader),
                    reader.ReadString(MaxDisplayNameBytes),
                    ReadDimension(reader),
                    ReadDimension(reader)),
                ScreenShareMessageKind.StreamStopped => new StreamStopped(
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.Subscribe => new Subscribe(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes),
                    ReadDimension(reader)),
                ScreenShareMessageKind.Unsubscribe => new Unsubscribe(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.RequestFullFrame => new RequestFullFrame(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes)),
                ScreenShareMessageKind.FrameAck => new FrameAck(
                    ReadParticipant(reader),
                    reader.ReadString(MaxIdBytes),
                    reader.ReadInt64()),
                ScreenShareMessageKind.UpdateSendToList => ReadUpdateSendToList(reader),
                _ => null
            };

            // trailing bytes mean the sender and receiver disagree about the layout
            if (decoded == null || !reader.IsAtEnd)
            {
                return false;
            }

            message = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }


    private static void WriteDataPacket(BinaryWriter writer, ScreenDataPacket packet)
    {
        // pass
    }


    private static void WriteDataPacket(BinaryWriter writer, ScreenDataPacket packet)
    {
        // pass
    }


    private static UpdateSendToList ReadUpdateSendToList(Reader reader)
    {
        // pass
    }


    private static void WriteParticipant(BinaryWriter writer, Participant participant)
    {
        // pass
    }


    private static Participant ReadParticipant(Reader reader)
    {
        // pass
    }


    private static IPEndPoint ReadEndpoint(Reader reader)
    {
        // pass
    }


    private static void WriteDimension(BinaryWriter writer, int value)
    {
        // pass
    }


    private static int ReadDimension(Reader reader)
    {
        // pass
    }


    private static void WriteString(BinaryWriter writer, string value, int maxBytes)
    {
        // pass
    }


    private static void WriteBytes(BinaryWriter writer, byte[] value, int maxBytes)
    {
        // pass
    }


    /// <summary>
    /// 
    /// </summary>
    private sealed class Reader
    {
        // pass
    }
}