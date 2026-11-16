// Author = Sumit
// Roll Number = 112301032

using System.Collections.Concurrent;
using System.Diagnostics;

namespace ScreenShare;

/// <summary>
/// runs posted work one item at a time on a dedicated thread. all relay and receiver
/// state is touched only here, so it needs no locks, and Networking's shared receive
/// thread is never blocked by ScreenShare work.
/// </summary>
internal sealed class SerialDispatcher : IDisposable
{
    private static readonly TimeSpan s_joinTimeout = TimeSpan.FromSeconds(2);

    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;
    private int _pending;

    public SerialDispatcher(string name)
    {
        _thread = new Thread(Run) { IsBackground = true, Name = name };
        _thread.Start();
        Trace.TraceInformation("[Screenshare] SerialDispatcher: started thread '{0}'.", _thread.Name);
    }

    /// <summary>
    /// true when nothing is queued or running. work posted from another dispatcher's
    /// work item is counted before that item finishes, so a group of dispatchers that
    /// are all idle at once has truly settled.
    /// </summary>
    public bool IsIdle => Volatile.Read(ref _pending) == 0;

    /// <summary>
    /// queues work. work posted after Dispose is dropped.
    /// </summary>
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Interlocked.Increment(ref _pending);
        try
        {
            _queue.Add(action);
        }
        catch (InvalidOperationException)
        {
            // disposed
            Interlocked.Decrement(ref _pending);
        }
    }

    /// <summary>
    /// stops accepting work, finishes what is already queued, and waits briefly for the thread
    /// </summary>
    public void Dispose()
    {
        if (_queue.IsAddingCompleted)
        {
            return;
        }

        Trace.TraceInformation("[Screenshare] SerialDispatcher.Dispose: finishing queued work on '{0}' and stopping.", _thread.Name);
        _queue.CompleteAdding();

        if (Thread.CurrentThread != _thread)
        {
            _thread.Join(s_joinTimeout);
        }
    }

    private void Run()
    {
        foreach (Action action in _queue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                // one bad message must not stop every stream
                Trace.TraceError("[Screenshare] SerialDispatcher.Run: work item failed on '{0}': {1}", _thread.Name, e);
            }
            finally
            {
                Interlocked.Decrement(ref _pending);
            }
        }
    }
}
