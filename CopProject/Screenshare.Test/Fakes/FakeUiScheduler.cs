using ScreenShare;

namespace Screenshare.Tests.Fakes;

/// <summary>
/// stands in for the UI thread: posted work waits until the test calls RunPending, and
/// timers tick only when the test calls Fire, so a test controls exactly when the UI runs.
/// </summary>
internal sealed class FakeUiScheduler : IUiScheduler
{
    private readonly Queue<Action> _pending = new();

    /// <summary>every timer made, in order</summary>
    public List<FakeUiTimer> Timers { get; } = new();

    /// <summary>how many actions are waiting to run</summary>
    public int PendingCount => _pending.Count;

    public bool Post(Action action)
    {
        _pending.Enqueue(action);
        return true;
    }

    public IUiTimer CreateTimer(TimeSpan interval, bool isRepeating, Action tick)
    {
        var timer = new FakeUiTimer(interval, isRepeating, tick);
        Timers.Add(timer);
        return timer;
    }

    /// <summary>runs the posted work, including work it posts in turn</summary>
    public void RunPending()
    {
        while (_pending.TryDequeue(out Action? action))
        {
            action();
        }
    }

    /// <summary>the timer with the given interval</summary>
    public FakeUiTimer Timer(TimeSpan interval)
    {
        return Timers.Single(t => t.Interval == interval);
    }
}

internal sealed class FakeUiTimer : IUiTimer
{
    private readonly Action _tick;

    public FakeUiTimer(TimeSpan interval, bool isRepeating, Action tick)
    {
        Interval = interval;
        IsRepeating = isRepeating;
        _tick = tick;
    }

    public TimeSpan Interval { get; }

    public bool IsRepeating { get; }

    public bool IsRunning { get; private set; }

    public bool IsDisposed { get; private set; }

    /// <summary>how many times Start was called</summary>
    public int StartCount { get; private set; }

    public void Start()
    {
        IsRunning = true;
        StartCount++;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Dispose()
    {
        IsRunning = false;
        IsDisposed = true;
    }

    /// <summary>ticks now, as the real timer would when its interval passes</summary>
    public void Fire()
    {
        if (!IsRunning)
        {
            return;
        }

        if (!IsRepeating)
        {
            IsRunning = false;
        }

        _tick();
    }
}
