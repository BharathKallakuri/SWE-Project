// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;

namespace Screenshare.Tests;

public class PngEncoderTests
{
    [Fact]
    public void Encode_ValidBitmap_ReturnsPngBytes()
    {
        using var bitmap = new Bitmap(10, 10);
        bitmap.SetPixel(5, 5, Color.Red);

        byte[] encoded = PngEncoder.Encode(bitmap);

        // ensure bytes are non-empty and represent a PNG
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);

        // png files start with this 8byte signature
        byte[] pngSignature =
        {
            137, 80, 78, 71, 13, 10, 26, 10
        };

        Assert.Equal(
            pngSignature,
            encoded.Take(pngSignature.Length).ToArray());

        // decode the output to verify it is a valid image
        using var stream = new MemoryStream(encoded);
        using var decoded = new Bitmap(stream);

        Assert.Equal(10, decoded.Width);
        Assert.Equal(10, decoded.Height);
        Assert.Equal(Color.Red.ToArgb(), decoded.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Encode_NullBitmap_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => PngEncoder.Encode(null!));
    }
}
