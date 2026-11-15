// Author = Aryan
// Roll Number = 112301041
using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenShare;

/// <summary>
/// encodes full frames as JPEG about a quarter of PNG size for a screen, and about 9 times
/// faster to encode. the picture is slightly lossy
/// </summary>
public static class JpegEncoder
{
    private static readonly ImageCodecInfo s_codec =
        ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

    /// <param name="quality">1 (smallest) to 100 (best)</param>
    /// <exception cref="ArgumentNullException">the bitmap is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">the quality is not 1 to 100</exception>
    public static byte[] Encode(Bitmap bitmap, int quality)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 100);

        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);

        using var stream = new MemoryStream();
        bitmap.Save(stream, s_codec, parameters);
        return stream.ToArray();
    }
}
