using System.Drawing;
using System.Drawing.Imaging;

namespace ScreenShare;

public class ImageStitcher : IImageStitcher, IDisposable
{
    private Bitmap? currentFrame;

    public void ApplyFullFrame(Bitmap frame)
    {
        Bitmap newFrame = new Bitmap(frame);
        currentFrame?.Dispose();
        currentFrame = newFrame;
    }

    /// <summary>
    /// Applies a changed tile to the current frame after validating its bounds.
    /// </summary>
    /// <param name="tile">The changed tile received from the sender.</param>
    /// <exception cref="ArgumentNullException">The tile is null.</exception>
    /// <exception cref="InvalidOperationException">No full frame has been received.</exception>
    /// <exception cref="ArgumentException">The tile has invalid dimensions or coordinates.</exception>
    public void ApplyChangedTile(Tile tile)
    {
        if(currentFrame == null)
        {
            throw new InvalidOperationException(
                "cannot apply a changed tile before recieving a full frame"
                );
        }

        if(tile.X<0 || tile.Y<0 || tile.Width<=0 || tile.Height <= 0)
        {
            throw new ArgumentException(
                "tile coordinates and dimensions are invalid.", nameof(tile));
        }

        // using subtraction to avoid integer overflow in X+width or Y+height
        if(tile.X>currentFrame.Width - tile.Width ||
            tile.Y>currentFrame.Height - tile.Height)
        {
            throw new ArgumentException("tile lies outside the current frame", nameof(tile));
        }

        if(tile.Data == null || tile.Data.Length == 0)
        {
            throw new ArgumentException(
                "tile image data cannot be empty", nameof(tile));
        }

        using MemoryStream stream = new(tile.Data); // ImageDiffer stores the changed tile as PNG bytes. so these two lines decodes the bytes back to bitmap for use
        using Bitmap tileBitmap = new(stream);

        // ensure the encoded image dimensions mathch the tile metadata
        if(tileBitmap.Width != tile.Width || tileBitmap.Height != tile.Height)
        {
            throw new ArgumentException(
                "encoded tile dimensions do not match the tile metadata", nameof(tile));
        }

        using Graphics graphics = Graphics.FromImage(currentFrame);

        graphics.DrawImage(
            tileBitmap,
            new Rectangle(tile.X, tile.Y, tile.Width, tile.Height),
            0,
            0,
            tileBitmap.Width,
            tileBitmap.Height,
            GraphicsUnit.Pixel
            );
    }

    public Bitmap? GetCurrentFrame()
    {
        return currentFrame == null ? null : new Bitmap(currentFrame);
    }

    public void Reset()
    {
        currentFrame?.Dispose();
        currentFrame = null;
    }

    public void Dispose()
    {
        Reset();
    }

}
