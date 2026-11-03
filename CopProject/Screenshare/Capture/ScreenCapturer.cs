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

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    /// <summary>
    /// captures the current contents of the primary display
    /// </summary>
    /// <returns>a bitmap containing the captured screen</returns>
    /// <exception cref="InvalidOperationException">
    /// the display dimensions are invalid
    /// </exception>
    public Bitmap Capture()
    {

        // a dpi-unaware thread sees scaled-down metrics (e.g. 1536 wide on a 1920 display at 125%)
        // and captures only part of the screen. switch only this thread, so the host's
        // process-wide dpi setting is left alone, and restore it afterwards.
        IntPtr previousContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        try
        {
            return CaptureScreen();
        }
        finally
        {
            if (previousContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousContext);
            }
        }

    }

    private static Bitmap CaptureScreen()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        if (width <= 0 || height <= 0)
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
