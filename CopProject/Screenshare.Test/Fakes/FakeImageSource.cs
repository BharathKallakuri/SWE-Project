using System.Drawing;
using System.Drawing.Imaging;
using ScreenShare;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// a "screen" for sharing tests: every capture is a new bitmap filled with Color, so a test
/// changes the screen by changing Color. records captures and whether it was disposed.
/// </summary>
internal sealed class FakeImageSource : IImageSource, IDisposable
{
    public FakeImageSource(int width = 64, int height = 48)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>the colour the next capture is filled with</summary>
    public Color Color { get; set; } = Color.CornflowerBlue;

    /// <summary>
    /// when true, every capture is new random pixels, which PNG cannot shrink: a busy screen
    /// whose frames are far bigger than one UDP datagram
    /// </summary>
    public bool Busy { get; set; }

    /// <summary>when set, Capture throws it, like a screen that cannot be read</summary>
    public Exception? CaptureException { get; set; }

    /// <summary>how many times Capture was called</summary>
    public int CaptureCount { get; private set; }

    public bool IsDisposed { get; private set; }

    public Bitmap Capture()
    {
        CaptureCount++;
        if (CaptureException != null)
        {
            throw CaptureException;
        }

        var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        if (Busy)
        {
            FillWithNoise(bitmap, seed: CaptureCount);
            return bitmap;
        }

        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color);
        }

        return bitmap;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }

    private static void FillWithNoise(Bitmap bitmap, int seed)
    {
        byte[] pixels = new byte[bitmap.Width * bitmap.Height * 4];
        new Random(seed).NextBytes(pixels);
        for (int i = 3; i < pixels.Length; i += 4)
        {
            pixels[i] = 255;   // opaque
        }

        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(pixels, y * bitmap.Width * 4,
                    IntPtr.Add(data.Scan0, y * data.Stride), bitmap.Width * 4);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
