// Author = Sanjaysiva A
// Roll Number = 112301030

using System.Diagnostics;
using System.Drawing;

namespace ScreenShare;

internal sealed class SharedScreenCapture : IDisposable
{
    private readonly IImageSource _source;
    private readonly TimeSpan _maxAge;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();
    private Bitmap? _latest;
    private DateTimeOffset _takenAt;

    /// <param name="source">the screen; disposed with this object if it is disposable</param>
    /// <param name="maxAge">how long a capture may be reused, e.g. one frame of the full stream</param>
    public SharedScreenCapture(IImageSource source, TimeSpan maxAge, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _source = source;
        _maxAge = maxAge;
        _timeProvider = timeProvider;
        Trace.TraceInformation("[Screenshare] SharedScreenCapture.constructorr: Initialized SharedScreenCapture with maxAge {0}.", maxAge);
    }

    internal int ScreenCaptures { get; private set; }

    /// <summary>a copy of the latest capture, taking a new one if it is too old</summary>
    public Bitmap Capture()
    {
        lock (_lock)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_latest == null || now - _takenAt >= _maxAge)
            {
                try
                {
                    Bitmap fresh = _source.Capture();
                    ScreenCaptures++;
                    _latest?.Dispose();
                    _latest = fresh;
                    _takenAt = now;
                    Trace.TraceInformation("[Screenshare] SharedScreenCapture.Capture: Captured fresh frame (total captures: {0}).", ScreenCaptures);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("[Screenshare] SharedScreenCapture.Capture: Failed to capture from image source: {0}", ex);
                    throw;
                }
            }

            return new Bitmap(_latest);
        }
    }

    /// <summary>an image source reading this capture; disposing it leaves the capture alone</summary>
    public IImageSource View()
    {
        return new SharedView(this);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _latest?.Dispose();
            _latest = null;
        }

        (_source as IDisposable)?.Dispose();
        Trace.TraceInformation("[Screenshare] SharedScreenCapture.Dispose: Disposed SharedScreenCapture.");
    }

    private sealed class SharedView(SharedScreenCapture owner) : IImageSource
    {
        public Bitmap Capture()
        {
            return owner.Capture();
        }
    }
}
