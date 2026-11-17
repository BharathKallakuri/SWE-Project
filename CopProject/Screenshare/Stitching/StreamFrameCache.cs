// Author = Aryan
// Roll Number = 112301041

using System.Diagnostics;
using System.Drawing;

namespace ScreenShare;

public class StreamFrameCache : IDisposable
{
    private readonly Dictionary<string, Bitmap> _frames = new();

    private readonly object _cacheLock = new();

    private bool _disposed;

    public void Update(string streamId, Bitmap frame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);
        ArgumentNullException.ThrowIfNull(frame);

        // make a copy so that caller retains ownership of its bitmap.
        Bitmap copy = new Bitmap(frame);
        int width = copy.Width;
        int height = copy.Height;

        lock (_cacheLock)
        {
            if (_disposed)
            {
                copy.Dispose();
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (_frames.TryGetValue(streamId, out Bitmap? oldFrame))
            {
                _frames[streamId] = copy;
                oldFrame.Dispose();
            }
            else
            {
                _frames.Add(streamId, copy);
            }
        }
    }

    public Bitmap? Get(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (_cacheLock)
        {
            if (_disposed)
            {
                Trace.TraceWarning(
                    "[Screenshare] StreamFrameCache.Get: Cache already disposed. Stream: {0}.", streamId);
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (!_frames.TryGetValue(streamId, out Bitmap? frame))
            {
                return null;
            }

            return new Bitmap(frame);
        }
    }

    public void Remove(string streamId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamId);

        lock (_cacheLock)
        {
            if (_disposed)
            {
                Trace.TraceWarning(
                    "[Screenshare] StreamFrameCache.Remove: Cache already disposed. Stream: {0}.", streamId);
                throw new ObjectDisposedException(nameof(StreamFrameCache));
            }

            if (_frames.Remove(streamId, out Bitmap? frame))
            {
                frame.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_cacheLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (Bitmap frame in _frames.Values)
            {
                frame.Dispose();
            }

            _frames.Clear();
        }
    }
}
