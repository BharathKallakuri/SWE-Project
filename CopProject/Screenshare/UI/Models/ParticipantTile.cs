using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ScreenShare.Models;

public class ParticipantTile : INotifyPropertyChanged
{
    private WriteableBitmap? _bitmap;

    public string Id { get; }
    public string Name { get; }

    // null until the first frame arrives; replaced only when the stream's size changes
    public ImageSource? Frame => _bitmap;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ParticipantTile(string id, string name) { Id = id; Name = name; }

    // Call on the UI thread. Copies the raw pixels straight into the bitmap, no decoding needed.
    public void UpdateFrame(ScreenFrame frame)
    {
        try
        {
            if (_bitmap == null || _bitmap.PixelWidth != frame.Width || _bitmap.PixelHeight != frame.Height)
            {
                _bitmap = new WriteableBitmap(frame.Width, frame.Height);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frame)));
            }

            using (Stream pixels = _bitmap.PixelBuffer.AsStream())
            {
                pixels.Write(frame.Pixels, 0, frame.Pixels.Length);
            }

            _bitmap.Invalidate();
        }
        catch (Exception e)
        {
            // one bad frame should not take down the meeting window; the next frame replaces it
            Trace.WriteLine($"[ScreenShare] could not show a frame for {Id}: {e.Message}");
        }
    }
}
