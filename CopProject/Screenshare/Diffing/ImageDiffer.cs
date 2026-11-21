// Author = Aryan
// Roll Number = 112301041

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenShare;

public class ImageDiffer : IImageDiffer
{
    /// <summary>width and height of a full tile in pixels, edge tiles may be smaller</summary>
    public const int TILE_SIZE = 64;

    /*if more than this percentage of the screen has changed, sending individual tiles becomes less useful, so we send the complete frame.*/
    private const double FULL_FRAME_THRESHOLD = 0.50;

    /*if this fraction of the screen or less has changed (1-2 tiles at 720p, like a blinking caret or a
    hover highlight), the change is held back instead of sent. ScreenSharingService applies it.*/
    public const double MIN_CHANGE_THRESHOLD = 0.01;

    public const int LARGE_DELTA_BYTES = 16 * 1024;

    private readonly int _tileSize;

    /// <param name="tileSize">width and height of a full tile in pixels; smaller tiles suit a
    /// small picture (a grid thumbnail), where one 64-pixel tile is a big share of the screen</param>
    /// <exception cref="ArgumentOutOfRangeException">the tile size is not positive</exception>
    public ImageDiffer(int tileSize = TILE_SIZE)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);
        _tileSize = tileSize;
    }

    /// <summary>gets the width and height of a full tile in pixels</summary>
    public int TileSize => _tileSize;

    private const ulong FNV_OFFSET_BASIS = 14695981039346656037UL;
    private const ulong FNV_PRIME = 1099511628211UL;

    public ImageDiffResult Compare(Bitmap previousFrame, Bitmap currentFrame)
    {
        if (previousFrame.Width != currentFrame.Width ||
            previousFrame.Height != currentFrame.Height)
        {
            Trace.TraceWarning(
            "[Screenshare] ImageDiffer.Compare: Frame dimensions changed. Previous: {0}x{1}, Current: {2}x{3}. Sending full frame.",
            previousFrame.Width,
            previousFrame.Height,
            currentFrame.Width,
            currentFrame.Height);
            return ImageDiffResult.CreateFullFrame(new Bitmap(currentFrame));
        }

        // read each frame pixels once, instead of locking the bitmap twice for every tile
        int stride = currentFrame.Width * 4;
        byte[] previousPixels = ReadPixels(previousFrame);
        byte[] currentPixels = ReadPixels(currentFrame);

        int columns = (currentFrame.Width + _tileSize - 1) / _tileSize;

        int rows = (currentFrame.Height + _tileSize - 1) / _tileSize;

        //List<Tile> changedTiles = new();

        long totalPixels = (long)currentFrame.Width * currentFrame.Height;

        long changedPixels = 0;

        // first find which tiles changed, without encoding anything: when most of the picture
        // changed the tiles are not sent at all, so encoding them first would be wasted
        List<Rectangle> changedAreas = new();



        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int x = column * _tileSize;
                int y = row * _tileSize;

                int width = Math.Min(
                    _tileSize,
                    currentFrame.Width - x
                );

                int height = Math.Min(
                    _tileSize,
                    currentFrame.Height - y
                );

                // compare the bytes directly (vectorised, stops at the first difference)
                if (!TileEquals(previousPixels, currentPixels, stride, x, y, width, height))
                {
                    changedAreas.Add(new Rectangle(x, y, width, height));

                    changedPixels += (long)width * height;
                }
            }
        }

        // Nothing changed.

        if (changedAreas.Count == 0)
        {
            return ImageDiffResult.Unchanged();
        }

        //tooo much of the screen changed.
        //send a full frame instead of many tiles.

        double changeRatio =
            (double)changedPixels / totalPixels;

        if (changeRatio >= FULL_FRAME_THRESHOLD)
        {
            Trace.TraceInformation(
            "[Screenshare] ImageDiffer.Compare: Change ratio: {0:P2}. Threshold: {1:P0}. Sending full frame.",
            changeRatio,
            FULL_FRAME_THRESHOLD);
            return ImageDiffResult.CreateFullFrame(
                new Bitmap(currentFrame)
            );
        }

        // only a small part changed.encode just the changed tiles each with its hash
        List<Tile> changedTiles = new(changedAreas.Count);
        foreach (Rectangle area in changedAreas)
        {
            changedTiles.Add(new Tile(
                area.X,
                area.Y,
                area.Width,
                area.Height,
                CalculateTileHash(currentPixels, stride, area.X, area.Y, area.Width, area.Height),
                ExtractTileData(currentFrame, area.X, area.Y, area.Width, area.Height)));
        }

        long totalPayloadSize = changedTiles.Sum(tile => (long)tile.Data.Length);

        return totalPayloadSize > LARGE_DELTA_BYTES ? ImageDiffResult.Delta(changedTiles, new Bitmap(currentFrame))
        : ImageDiffResult.Delta(changedTiles);

    }


    /// <summary>copies a bitmap's pixels as 32bpp ARGB rows packed with no padding</summary>
    private static byte[] ReadPixels(Bitmap bitmap)
    {
        int rowBytes = bitmap.Width * 4;
        byte[] pixels = new byte[rowBytes * bitmap.Height];

        /// bitmapData represents locked pixel memory including Scan0 and Stride
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), pixels, y * rowBytes, rowBytes);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return pixels;
    }

    private static bool TileEquals(byte[] previous, byte[] current, int stride, int startX, int startY, int width, int height)
    {
        int rowBytes = width * 4;
        for (int y = startY; y < startY + height; y++)
        {
            int offset = y * stride + startX * 4;
            if (!previous.AsSpan(offset, rowBytes).SequenceEqual(current.AsSpan(offset, rowBytes)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>FNV over the tile rows, top to bottom; the same value the old per-tile lock gave</summary>
    private static ulong CalculateTileHash(byte[] pixels, int stride, int startX, int startY, int width, int height)
    {
        ulong hash = FNV_OFFSET_BASIS;
        int rowBytes = width * 4;

        for (int y = startY; y < startY + height; y++)
        {
            ReadOnlySpan<byte> row = pixels.AsSpan(y * stride + startX * 4, rowBytes);
            foreach (byte value in row)
            {
                hash ^= value;
                hash *= FNV_PRIME;
            }
        }

        return hash;
    }

    private byte[] ExtractTileData(
        Bitmap bitmap,
        int startX,
        int startY,
        int width,
        int height)
    {

        Rectangle rectangle = new(
            startX,
            startY,
            width,
            height
            );

        try
        {
            using Bitmap tileBitmap = bitmap.Clone(
                rectangle,
                PixelFormat.Format32bppArgb);

            return PngEncoder.Encode(tileBitmap);
        }
        catch (Exception exception)
        {
            Trace.TraceError(
                "[Screenshare] ImageDiffer.ExtractTileData: Failed to extract tile at ({0}, {1}) of size {2}x{3}: {4}",
                startX,
                startY,
                width,
                height,
                exception);
            throw;
        }
    }
}
