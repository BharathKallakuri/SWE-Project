using Microsoft.UI.Dispatching;

namespace ScreenShare;

/// <summary>
/// the app's UI thread, through WinUI's DispatcherQueue.
/// not covered by unit tests: a DispatcherQueue only exists inside a running WinUI app.
/// </summary>
internal sealed class DispatcherQueueScheduler : IUiScheduler
{
    private readonly DispatcherQueue _queue;

    /// <exception cref="ArgumentNullException">the queue is null</exception>
    public DispatcherQueueScheduler(DispatcherQueue queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        _queue = queue;
    }

    public bool Post(Action action)
    {
        return _queue.TryEnqueue(() => action());
    }

    public IUiTimer CreateTimer(TimeSpan interval, bool isRepeating, Action tick)
    {
        return new Timer(_queue.CreateTimer(), interval, isRepeating, tick);
    }

    private sealed class Timer : IUiTimer
    {
        private readonly DispatcherQueueTimer _timer;
        private readonly Action _tick;

        public Timer(DispatcherQueueTimer timer, TimeSpan interval, bool isRepeating, Action tick)
        {
            _timer = timer;
            _tick = tick;
            _timer.Interval = interval;
            _timer.IsRepeating = isRepeating;
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= OnTick;
        }

        private void OnTick(DispatcherQueueTimer sender, object args)
        {
            _tick();
        }
    }
}
