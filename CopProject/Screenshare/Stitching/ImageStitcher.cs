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

    public void ApplyChangedTile(Tile tile)
    {
        if(currentFrame == null)
        {
            throw new InvalidOperationException(
                "cannot apply a changed tile before recieving a full frame"
                );
        }

        using MemoryStream stream = new(tile.Data); // ImageDiffer stores the changed tile as PNG bytes. so these two lines decodes the bytes back to bitmap for use
        using Bitmap tileBitmap = new(stream);

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
