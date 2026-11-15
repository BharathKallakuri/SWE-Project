// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;
using System.Drawing;

namespace ScreenShare;

/// <summary>
/// turns the sender's diff results into numbered data packets for a stream:
/// a full frame becomes a PNG, a delta keeps its tiles, and an unchanged frame becomes a pulse
/// </summary>
public sealed class ScreenPacketizer
{
    private int _width;
    private int _height;

    /// <summary>
    /// gets the stream these packets belong to
    /// </summary>
    public string StreamId { get; }

    /// <summary>
    /// gets the frame number the next packet will carry. pulses take a number too,
    /// so the receiver can tell a quiet screen from a lost packet.
    /// </summary>
    public long NextFrameNumber { get; private set; }

    /// <exception cref="ArgumentException">the stream id is empty</exception>
    private readonly int _jpegQuality;
    private readonly int _tileSize;
    private bool _jpegWonLastCheck;
    private int _fullFramesSinceCheck;

    /// <summary>how many full frames go out as JPEG, without trying PNG, after JPEG was smaller</summary>
    internal const int PngRecheckInterval = 15;

    /// <param name="jpegQuality">JPEG quality 1 to 100 for full frames, or 0 for PNG</param>
    /// <param name="tileSize">the tile size the differ used, written into delta packets</param>
    /// <exception cref="ArgumentOutOfRangeException">the quality is not 0 to 100, or the tile size not positive</exception>
    public ScreenPacketizer(string streamId, int jpegQuality = 0, int tileSize = ImageDiffer.TILE_SIZE)
    {
        if (string.IsNullOrWhiteSpace(streamId))
        {
            Trace.TraceError("[Screenshare] ScreenPacketizer.constructro: streamId cannot be null or whitespace.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        if (jpegQuality < 0 || jpegQuality > 100)
        {
            Trace.TraceError("[Screenshare] ScreenPacketizer.constructor: Invalid jpegQuality {0}. Must be between 0 and 100.", jpegQuality);
        }
        ArgumentOutOfRangeException.ThrowIfNegative(jpegQuality);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(jpegQuality, 100);

        if (tileSize <= 0)
        {
            Trace.TraceError("[Screenshare] ScreenPacketizer.constructor: Invalid tileSize {0}. Must be positive.", tileSize);
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        StreamId = streamId;
        _jpegQuality = jpegQuality;
        _tileSize = tileSize;
        Trace.TraceInformation("[Screenshare] ScreenPacketizer.constructorr: Initialized packetizer for stream {0} (jpegQuality: {1}, tileSize: {2}).", streamId, jpegQuality, tileSize);
    }

    /// <summary>
    /// creates the packet for one diff result and advances the frame number
    /// </summary>
    /// <param name="result">the diff result; it stays owned by the caller</param>
    /// <param name="capturedAtUtc">when the frame was captured, in UTC</param>
    /// <exception cref="ArgumentNullException">the result is null</exception>
    /// <exception cref="ArgumentException">the timestamp is not UTC</exception>
    /// <exception cref="InvalidOperationException">a delta or pulse arrives before any full frame</exception>
    public ScreenDataPacket CreatePacket(ImageDiffResult result, DateTime capturedAtUtc)
    {
        if (result == null)
        {
            Trace.TraceError("[Screenshare] ScreenPacketizer.CreatePacket: ImageDiffResult is null for stream {0}.", StreamId);
        }
        ArgumentNullException.ThrowIfNull(result);

        ScreenDataPacket packet;

        // a big change may be smaller sent as one JPEG than as many PNG tiles (a scrolled page)
        byte[]? wholeFrameJpeg = BigDeltaAsJpegIfSmaller(result);

        if (result.IsFullFrame || wholeFrameJpeg != null)
        {
            Bitmap frame = result.IsFullFrame ? result.FullFrame! : result.WholeFrame!;

            packet = new ScreenDataPacket(
                StreamId,
                NextFrameNumber,
                capturedAtUtc,
                frame.Width,
                frame.Height,
                DataPacketType.FullFrame,
                fullFrameData: wholeFrameJpeg ?? EncodeFullFrame(frame));

            // deltas and pulses carry no image, so it needs to remember the size from the last full frame.
            // the differ sends a full frame whenever the size changes.
            _width = frame.Width;
            _height = frame.Height;
        }
        else
        {
            if (_width == 0)
            {
                Trace.TraceWarning("[Screenshare] ScreenPacketizer.CreatePacket: Delta or pulse arrived before any full frame for stream {0}.", StreamId);
                throw new InvalidOperationException(
                    "a full frame must be packetized before deltas or pulses");
            }

            packet = result.IsUnchanged
                ? new ScreenDataPacket(
                    StreamId, NextFrameNumber, capturedAtUtc, _width, _height,
                    DataPacketType.Pulse)
                : new ScreenDataPacket(
                    StreamId, NextFrameNumber, capturedAtUtc, _width, _height,
                    DataPacketType.ChangedTiles,
                    changedTiles: result.ChangedTiles,
                    tileSize: _tileSize);
        }

        Trace.TraceInformation("[Screenshare] ScreenPacketizer.CreatePacket: Created {0} packet for stream {1}, frame {2}.", packet.Type, StreamId, packet.FrameNumber);
        NextFrameNumber++;
        return packet;
    }

    /// <summary>
    /// PNG, or with JPEG on the smaller of the two
    /// </summary>
    private byte[] EncodeFullFrame(Bitmap frame)
    {
        try
        {
            if (_jpegQuality == 0)
            {
                byte[] pngOnly = PngEncoder.Encode(frame);
                Trace.TraceInformation("[Screenshare] ScreenPacketizer.EncodeFullFrame: Encoded full frame as PNG ({0} bytes) for stream {1}.", pngOnly.Length, StreamId);
                return pngOnly;
            }

            byte[] jpeg = JpegEncoder.Encode(frame, _jpegQuality);

            if (_jpegWonLastCheck && _fullFramesSinceCheck < PngRecheckInterval)
            {
                _fullFramesSinceCheck++;
                Trace.TraceInformation("[Screenshare] ScreenPacketizer.EncodeFullFrame: Reused JPEG encoding ({0} bytes) for stream {1}.", jpeg.Length, StreamId);
                return jpeg;
            }

            byte[] png = PngEncoder.Encode(frame);
            _jpegWonLastCheck = jpeg.Length < png.Length;
            _fullFramesSinceCheck = 0;
            byte[] chosen = _jpegWonLastCheck ? jpeg : png;
            Trace.TraceInformation("[Screenshare] ScreenPacketizer.EncodeFullFrame: Encoded full frame for stream {0} using {1} (JPEG: {2} bytes, PNG: {3} bytes).", StreamId, _jpegWonLastCheck ? "JPEG" : "PNG", jpeg.Length, png.Length);
            return chosen;
        }
        catch (Exception ex)
        {
            Trace.TraceError("[Screenshare] ScreenPacketizer.EncodeFullFrame: Failed to encode full frame for stream {0}: {1}", StreamId, ex);
            throw;
        }
    }

    private byte[]? BigDeltaAsJpegIfSmaller(ImageDiffResult result)
    {
        if (_jpegQuality == 0 || result.IsFullFrame || result.WholeFrame == null || _width == 0)
        {
            return null;
        }

        try
        {
            byte[] jpeg = JpegEncoder.Encode(result.WholeFrame, _jpegQuality);
            long tileBytes = result.ChangedTiles.Sum(tile => (long)tile.Data.Length);
            bool jpegSmaller = jpeg.Length < tileBytes;
            if (jpegSmaller)
            {
                Trace.TraceInformation("[Screenshare] ScreenPacketizer.BigDeltaAsJpegIfSmaller: Sending whole frame JPEG ({0} bytes) instead of {1} tiles ({2} bytes) for stream {3}.", jpeg.Length, result.ChangedTiles.Count, tileBytes, StreamId);
                return jpeg;
            }

            return null;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("[Screenshare] ScreenPacketizer.BigDeltaAsJpegIfSmaller: Failed to check whole frame JPEG size for stream {0}: {1}", StreamId, ex);
            return null;
        }
    }
}
