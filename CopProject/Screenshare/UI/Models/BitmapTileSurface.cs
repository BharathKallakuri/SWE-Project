// Author = Sumit
// Roll Number = 112301032

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
namespace ScreenShare.Models;


/// <summary>
/// a tile surface backed by a WinUI WriteableBitmap. UI thread only.
/// not covered by unit tests: a WriteableBitmap needs the running WinUI app (see the skipped
/// BitMapTileSurfaceTests); the tile logic around it is tested with FakeTileSurface.
/// </summary>
/// 
[ExcludeFromCodeCoverage]
internal sealed class BitmapTileSurface : ITileSurface
{
    private readonly WriteableBitmap _bitmap;

    public BitmapTileSurface(int width, int height)
    {
        _bitmap = new WriteableBitmap(width, height);
    }

    public int PixelWidth => _bitmap.PixelWidth;

    public int PixelHeight => _bitmap.PixelHeight;

    public ImageSource? Source => _bitmap;

    // copies the raw pixels straight into the bitmap, no decoding needed
    public void WritePixels(byte[] pixels)
    {
        using (Stream stream = _bitmap.PixelBuffer.AsStream())
        {
            stream.Write(pixels, 0, pixels.Length);
        }

        _bitmap.Invalidate();
    }
}
