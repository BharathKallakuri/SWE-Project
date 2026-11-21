// Author = Sumit
// Roll Number = 112301032

namespace Screenshare.Tests.Fakes;

/// <summary>
/// a clock that only moves when a test calls Advance. timers made with CreateTimer fire
/// on the thread that calls Advance, in due order, so timeouts are tested without waiting.
/// a timer due now (dueTime zero) fires on the next Advance, even Advance(TimeSpan.Zero).
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _lock = new();
    private readonly List<ManualTimer> _timers = new();
    private DateTimeOffset _utcNow;

    public ManualTimeProvider()
        : this(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
    {
    }

    public ManualTimeProvider(DateTimeOffset start)
    {
        _utcNow = start;
    }

    /// <summary>gets how many timers are still running</summary>
    public int ActiveTimerCount
    {
        get { lock (_lock) { return _timers.Count(t => t.DueAt != null); } }
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock) { return _utcNow; }
    }

    // keep timestamps on the same manual clock, so elapsed-time maths agrees with GetUtcNow
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        return GetUtcNow().UtcTicks;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var timer = new ManualTimer(this, callback, state);
        lock (_lock)
        {
            _timers.Add(timer);
            timer.Schedule(_utcNow, dueTime, period);
        }

        return timer;
    }

    /// <summary>moves the clock forward, firing every timer that falls due on the way</summary>
    /// <exception cref="ArgumentOutOfRangeException">delta is negative</exception>
    public void Advance(TimeSpan delta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delta, TimeSpan.Zero);

        DateTimeOffset target;
        lock (_lock) { target = _utcNow + delta; }

        while (true)
        {
            ManualTimer? next;
            lock (_lock)
            {
                next = _timers
                    .Where(t => t.DueAt != null && t.DueAt <= target)
                    .OrderBy(t => t.DueAt)
                    .FirstOrDefault();

                if (next == null)
                {
                    _utcNow = target;
                    return;
                }

                _utcNow = next.DueAt!.Value;
                next.Reschedule(_utcNow);
            }

            // outside the lock, so the callback may read the clock or change timers
            next.Fire();
        }
    }

    private void Remove(ManualTimer timer)
    {
        lock (_lock) { _timers.Remove(timer); }
    }

    private sealed class ManualTimer : ITimer
    {
        private readonly ManualTimeProvider _owner;
        private readonly TimerCallback _callback;
        private readonly object? _state;
        private TimeSpan _period;

        public ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state)
        {
            _owner = owner;
            _callback = callback;
            _state = state;
        }

        /// <summary>when the timer fires next; null when it is stopped</summary>
        public DateTimeOffset? DueAt { get; private set; }

        public void Schedule(DateTimeOffset now, TimeSpan dueTime, TimeSpan period)
        {
            _period = period;
            DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : now + dueTime;
        }

        // a period of zero or infinite means the timer fires once
        public void Reschedule(DateTimeOffset now)
        {
            DueAt = _period > TimeSpan.Zero && _period != Timeout.InfiniteTimeSpan ? now + _period : null;
        }

        public void Fire()
        {
            _callback(_state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            lock (_owner._lock)
            {
                Schedule(_owner._utcNow, dueTime, period);
            }

            return true;
        }

        public void Dispose()
        {
            lock (_owner._lock)
            {
                DueAt = null;
            }

            _owner.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
