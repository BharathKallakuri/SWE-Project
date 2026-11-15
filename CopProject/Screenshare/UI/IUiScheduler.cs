// Author = Sumit
// Roll Number = 112301032

namespace ScreenShare;

/// <summary>
/// the UI thread as the view model sees it: run work on it, and make timers that tick on it.
/// the app uses DispatcherQueueScheduler; tests use a fake that runs work when told to.
/// </summary>
internal interface IUiScheduler
{
    /// <summary>queues work to run on the UI thread; false if the UI is shutting down</summary>
    bool Post(Action action);

    /// <summary>makes a stopped timer that calls tick on the UI thread</summary>
    /// <param name="interval">time between ticks</param>
    /// <param name="isRepeating">false to tick once per Start</param>
    /// <param name="tick">what to run on each tick</param>
    IUiTimer CreateTimer(TimeSpan interval, bool isRepeating, Action tick);
}
