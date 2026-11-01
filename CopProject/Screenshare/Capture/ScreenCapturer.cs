using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScreenShare;

/// <summary>
/// captures the primary display on windows
/// </summary>
public class ScreenCapturer : IImageSource
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    /// <summary>
    /// captures the current contents of the primary display
    /// </summary>
    /// <returns>a bitmap containing the captured screen</returns>
    /// <exception cref="InvalidOperationException">
    /// the display dimensions are invalid
    /// </exception>
    public Bitmap Capture()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        if(width<=0 || height <= 0)
        {
            throw new InvalidOperationException($"invalid screen dimensions: {width}x{height}");
        }

        var bounds = new Rectangle(
            0,
            0,
            width,
            height);

        var screenshot = new Bitmap(
            width,
            height,
            PixelFormat.Format32bppArgb);

        try
        {
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                return screenshot;
            }
        }
        catch
        {
            screenshot.Dispose();
            throw;
        }
    }
}
